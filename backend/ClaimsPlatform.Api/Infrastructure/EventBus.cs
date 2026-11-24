using System.Threading.Channels;
using ClaimsPlatform.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClaimsPlatform.Api.Infrastructure;

public record ClaimDomainEvent(
    Guid ClaimId,
    string ClaimNumber,
    string Market,
    string Type,
    string PreviousStatus,
    string NewStatus,
    decimal EstimatedLiability,
    decimal? SettledAmount,
    DateTime OccurredAt
);

public interface IClaimEventPublisher
{
    ValueTask PublishAsync(ClaimDomainEvent domainEvent, CancellationToken ct = default);
}

public class InMemoryKafkaChannelBus : IClaimEventPublisher
{
    private readonly Channel<ClaimDomainEvent> _channel = Channel.CreateUnbounded<ClaimDomainEvent>();

    public ValueTask PublishAsync(ClaimDomainEvent domainEvent, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(domainEvent, ct);

    public IAsyncEnumerable<ClaimDomainEvent> ReadAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}

public class ExposureConsumerWorker : BackgroundService
{
    private readonly InMemoryKafkaChannelBus _bus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExposureConsumerWorker> _logger;

    public ExposureConsumerWorker(InMemoryKafkaChannelBus bus, IServiceScopeFactory scopeFactory, ILogger<ExposureConsumerWorker> logger)
    {
        _bus = bus;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in _bus.ReadAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();

            var market = Enum.Parse<ApacMarket>(evt.Market);
            var type = Enum.Parse<ClaimType>(evt.Type);

            var claimsInSlice = await db.Claims.Where(c => c.Market == market && c.Type == type).ToListAsync(stoppingToken);
            var openClaims = claimsInSlice.Where(c => c.Status is ClaimStatus.Submitted or ClaimStatus.UnderReview or ClaimStatus.InfoRequired).ToList();
            var settledClaims = claimsInSlice.Where(c => c.Status == ClaimStatus.Settled).ToList();

            var summary = await db.ExposureSummaries.FindAsync(new object[] { market, type }, stoppingToken);
            if (summary is null)
            {
                summary = new ExposureSummary { Market = market, Type = type };
                db.ExposureSummaries.Add(summary);
            }

            summary.OpenClaimsCount = openClaims.Count;
            summary.TotalOutstandingLiability = openClaims.Sum(c => c.EstimatedLiability);
            summary.TotalSettledAmount = settledClaims.Sum(c => c.SettledAmount ?? 0);
            summary.LastUpdated = DateTime.UtcNow;

            await db.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("[EVENT CONSUMED] {ClaimNumber} -> {Status} | {Market}-{Type} Exposure: {Liability:C}",
                evt.ClaimNumber, evt.NewStatus, evt.Market, evt.Type, summary.TotalOutstandingLiability);
        }
    }
}