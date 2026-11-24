using ClaimsPlatform.Api.Domain;
using ClaimsPlatform.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClaimsPlatform.Api.Features.Dashboard;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ClaimsDbContext _db;

    public DashboardController(ClaimsDbContext db) => _db = db;

    [HttpGet("exposure")]
    public async Task<IActionResult> GetExposure()
    {
        var summaries = await _db.ExposureSummaries.OrderBy(e => e.Market).ToListAsync();
        var officerWorkload = await _db.Claims
            .Where(c => c.AssignedOfficerId != null)
            .GroupBy(c => c.AssignedOfficerId!)
            .Select(g => new
            {
                OfficerId = g.Key,
                ActiveClaims = g.Count(c => c.Status != ClaimStatus.Settled && c.Status != ClaimStatus.Rejected),
                SettledCount = g.Count(c => c.Status == ClaimStatus.Settled),
                RejectedCount = g.Count(c => c.Status == ClaimStatus.Rejected)
            })
            .ToListAsync();

        return Ok(new
        {
            TotalOutstandingLiability = summaries.Sum(s => s.TotalOutstandingLiability),
            TotalOpenClaims = summaries.Sum(s => s.OpenClaimsCount),
            TotalSettledAmount = summaries.Sum(s => s.TotalSettledAmount),
            ByMarketAndType = summaries,
            OfficerWorkload = officerWorkload
        });
    }
}