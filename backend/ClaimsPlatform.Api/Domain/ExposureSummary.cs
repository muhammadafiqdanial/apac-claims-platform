namespace ClaimsPlatform.Api.Domain;

public class ExposureSummary
{
    public ApacMarket Market { get; set; }
    public ClaimType Type { get; set; }
    public int OpenClaimsCount { get; set; }
    public decimal TotalOutstandingLiability { get; set; }
    public decimal TotalSettledAmount { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}