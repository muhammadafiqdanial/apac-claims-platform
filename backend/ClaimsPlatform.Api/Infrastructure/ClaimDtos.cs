using ClaimsPlatform.Api.Domain;

namespace ClaimsPlatform.Api.Features.Claims;

public record SubmitClaimRequest(string PolicyNumber, string ClaimantName, ApacMarket Market, ClaimType Type, string Description, decimal EstimatedLiability);
public record AssignClaimRequest(string OfficerId);
public record RequestInfoRequest(string OfficerId, string Question);
public record ProvideInfoRequest(string ClaimantName, string Response);
public record SettleClaimRequest(string OfficerId, decimal SettledAmount, string Notes);
public record RejectClaimRequest(string OfficerId, string Reason);