using System.Text.Json.Serialization;
using ClaimsPlatform.Api.Domain;
using ClaimsPlatform.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ClaimsDbContext>(opt => opt.UseSqlite("Data Source=claims.db"));
builder.Services.AddSingleton<InMemoryKafkaChannelBus>();
builder.Services.AddSingleton<IClaimEventPublisher>(sp => sp.GetRequiredService<InMemoryKafkaChannelBus>());
builder.Services.AddHostedService<ExposureConsumerWorker>();

builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
    db.Database.EnsureCreated();

    if (!db.Claims.Any())
    {
        var c1 = Claim.Submit("POL-SG-8821", "Wei Ming Tan", ApacMarket.SG, ClaimType.Motor, "Rear-end collision on PIE expressway", 14500m);
        c1.AssignOfficer("officer.sarah");

        var c2 = Claim.Submit("POL-MY-3310", "Afiq Rahman", ApacMarket.MY, ClaimType.Property, "Flash flood water damage to ground floor retail unit", 48000m);
        c2.AssignOfficer("officer.sarah");
        c2.RequestAdditionalInfo("officer.sarah", "Please provide municipal flood report and repair quotation.");

        var c3 = Claim.Submit("POL-JP-9902", "Kenji Sato", ApacMarket.JP, ClaimType.Motor, "Side mirror and door panel scrape in Shibuya", 6200m);
        var c4 = Claim.Submit("POL-AU-4412", "Chloe Vance", ApacMarket.AU, ClaimType.Property, "Hailstorm roof damage", 32000m);
        c4.AssignOfficer("officer.david");
        c4.Settle("officer.david", 29500m, "Verified against assessor report.");

        db.Claims.AddRange(c1, c2, c3, c4);
        await db.SaveChangesAsync();

        var publisher = scope.ServiceProvider.GetRequiredService<IClaimEventPublisher>();
        foreach (var c in new[] { c1, c2, c3, c4 })
        {
            await publisher.PublishAsync(new ClaimDomainEvent(
                c.Id, c.ClaimNumber, c.Market.ToString(), c.Type.ToString(),
                "NONE", c.Status.ToString(), c.EstimatedLiability, c.SettledAmount, DateTime.UtcNow));
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();
app.Run();