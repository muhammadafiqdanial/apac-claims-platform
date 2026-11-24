namespace ClaimsPlatform.Api.Domain;

public enum ClaimStatus { Submitted, UnderReview, InfoRequired, Settled, Rejected }
public enum ClaimType { Motor, Property }
public enum ApacMarket { SG, MY, HK, TH, AU, JP }

public class Claim
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string ClaimNumber { get; private set; } = string.Empty;
    public string PolicyNumber { get; private set; } = string.Empty;
    public string ClaimantName { get; private set; } = string.Empty;
    public ApacMarket Market { get; private set; }
    public ClaimType Type { get; private set; }
    public ClaimStatus Status { get; private set; } = ClaimStatus.Submitted;
    public string Description { get; private set; } = string.Empty;
    public decimal EstimatedLiability { get; private set; }
    public decimal? SettledAmount { get; private set; }
    public string? AssignedOfficerId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private readonly List<ClaimAuditEntry> _timeline = new();
    public IReadOnlyCollection<ClaimAuditEntry> Timeline => _timeline.AsReadOnly();

    private Claim() { }

    public static Claim Submit(string policyNumber, string claimantName, ApacMarket market, ClaimType type, string description, decimal estimatedLiability)
    {
        if (estimatedLiability <= 0)
            throw new InvalidOperationException("Estimated liability must be greater than zero.");

        var claim = new Claim
        {
            ClaimNumber = $"CLM-{market}-{Random.Shared.Next(100000, 999999)}",
            PolicyNumber = policyNumber,
            ClaimantName = claimantName,
            Market = market,
            Type = type,
            Description = description,
            EstimatedLiability = estimatedLiability,
            Status = ClaimStatus.Submitted
        };

        claim.AddAudit("CLAIMANT", "Claim submitted", ClaimStatus.Submitted);
        return claim;
    }

    public void AssignOfficer(string officerId)
    {
        EnsureOpen();
        AssignedOfficerId = officerId;
        if (Status == ClaimStatus.Submitted) Status = ClaimStatus.UnderReview;
        UpdatedAt = DateTime.UtcNow;
        AddAudit(officerId, $"Assigned to officer {officerId}", Status);
    }

    public void RequestAdditionalInfo(string officerId, string question)
    {
        EnsureOpen();
        Status = ClaimStatus.InfoRequired;
        UpdatedAt = DateTime.UtcNow;
        AddAudit(officerId, $"Requested info: {question}", Status);
    }

    public void ProvideAdditionalInfo(string claimantName, string response)
    {
        if (Status != ClaimStatus.InfoRequired)
            throw new InvalidOperationException("Additional info is only accepted when status is InfoRequired.");

        Status = ClaimStatus.UnderReview;
        UpdatedAt = DateTime.UtcNow;
        AddAudit(claimantName, $"Claimant provided info: {response}", Status);
    }

    public void Settle(string officerId, decimal settledAmount, string notes)
    {
        EnsureOpen();
        if (settledAmount < 0) throw new InvalidOperationException("Settled amount cannot be negative.");

        Status = ClaimStatus.Settled;
        SettledAmount = settledAmount;
        UpdatedAt = DateTime.UtcNow;
        AddAudit(officerId, $"Settled for {settledAmount:C}. Notes: {notes}", Status);
    }

    public void Reject(string officerId, string reason)
    {
        EnsureOpen();
        Status = ClaimStatus.Rejected;
        SettledAmount = 0;
        UpdatedAt = DateTime.UtcNow;
        AddAudit(officerId, $"Rejected. Reason: {reason}", Status);
    }

    private void EnsureOpen()
    {
        if (Status is ClaimStatus.Settled or ClaimStatus.Rejected)
            throw new InvalidOperationException($"Claim is already closed ({Status}).");
    }

    private void AddAudit(string actor, string note, ClaimStatus statusAtEvent)
    {
        _timeline.Add(new ClaimAuditEntry
        {
            ClaimId = Id,
            Actor = actor,
            Note = note,
            Status = statusAtEvent,
            Timestamp = DateTime.UtcNow
        });
    }
}

public class ClaimAuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClaimId { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public ClaimStatus Status { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}