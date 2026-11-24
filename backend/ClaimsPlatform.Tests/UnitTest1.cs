using ClaimsPlatform.Api.Domain;

namespace ClaimsPlatform.Tests;

public class ClaimDomainTests
{
    [Fact]
    public void Submit_ValidClaim_InitializesSubmittedAndCreatesAuditEntry()
    {
        var claim = Claim.Submit("POL-SG-1001", "Alice Tan", ApacMarket.SG, ClaimType.Motor, "Bumper damage", 3500m);

        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        Assert.StartsWith("CLM-SG-", claim.ClaimNumber);
        Assert.Single(claim.Timeline);
    }

    [Fact]
    public void FullLifecycle_InfoRequestAndSettlement_TransitionsCorrectly()
    {
        var claim = Claim.Submit("POL-MY-2002", "Bala Subramaniam", ApacMarket.MY, ClaimType.Property, "Roof leak", 12000m);

        claim.AssignOfficer("officer.sarah");
        Assert.Equal(ClaimStatus.UnderReview, claim.Status);

        claim.RequestAdditionalInfo("officer.sarah", "Attach contractor quote");
        Assert.Equal(ClaimStatus.InfoRequired, claim.Status);

        claim.ProvideAdditionalInfo("Bala Subramaniam", "Quote #Q-99 attached");
        Assert.Equal(ClaimStatus.UnderReview, claim.Status);

        claim.Settle("officer.sarah", 10500m, "Approved per quote");
        Assert.Equal(ClaimStatus.Settled, claim.Status);
        Assert.Equal(10500m, claim.SettledAmount);
        Assert.Equal(5, claim.Timeline.Count);
    }

    [Fact]
    public void Settle_AlreadyRejectedClaim_ThrowsInvalidOperationException()
    {
        var claim = Claim.Submit("POL-JP-3003", "Kenji Sato", ApacMarket.JP, ClaimType.Motor, "Scratch", 2000m);
        claim.Reject("officer.david", "Policy lapsed");

        Assert.Throws<InvalidOperationException>(() => claim.Settle("officer.david", 1500m, "Late override"));
    }

    [Fact]
    public void ProvideAdditionalInfo_WhenNotRequested_ThrowsInvalidOperationException()
    {
        var claim = Claim.Submit("POL-AU-4004", "Liam Smith", ApacMarket.AU, ClaimType.Property, "Fence storm damage", 4000m);

        Assert.Throws<InvalidOperationException>(() => claim.ProvideAdditionalInfo("Liam Smith", "Unsolicited note"));
    }
}