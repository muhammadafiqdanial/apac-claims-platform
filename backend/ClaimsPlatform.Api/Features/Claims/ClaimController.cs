using ClaimsPlatform.Api.Domain;
using ClaimsPlatform.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClaimsPlatform.Api.Features.Claims;

[ApiController]
[Route("api/[controller]")]
public class ClaimsController : ControllerBase
{
    private readonly ClaimsDbContext _db;
    private readonly IClaimEventPublisher _eventBus;

    public ClaimsController(ClaimsDbContext db, IClaimEventPublisher eventBus)
    {
        _db = db;
        _eventBus = eventBus;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ApacMarket? market, [FromQuery] ClaimStatus? status)
    {
        var query = _db.Claims.Include(c => c.Timeline).AsQueryable();
        if (market.HasValue) query = query.Where(c => c.Market == market.Value);
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);

        var claims = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        return Ok(claims);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var claim = await _db.Claims.Include(c => c.Timeline).FirstOrDefaultAsync(c => c.Id == id);
        return claim is null ? NotFound() : Ok(claim);
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitClaimRequest req)
    {
        try
        {
            var claim = Claim.Submit(req.PolicyNumber, req.ClaimantName, req.Market, req.Type, req.Description, req.EstimatedLiability);
            _db.Claims.Add(claim);
            await _db.SaveChangesAsync();

            await PublishTransition(claim, "NONE");
            return CreatedAtAction(nameof(GetById), new { id = claim.Id }, claim);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/assign")]
    public Task<IActionResult> Assign(Guid id, [FromBody] AssignClaimRequest req)
        => ExecuteTransition(id, c => c.AssignOfficer(req.OfficerId));

    [HttpPost("{id:guid}/request-info")]
    public Task<IActionResult> RequestInfo(Guid id, [FromBody] RequestInfoRequest req)
        => ExecuteTransition(id, c => c.RequestAdditionalInfo(req.OfficerId, req.Question));

    [HttpPost("{id:guid}/provide-info")]
    public Task<IActionResult> ProvideInfo(Guid id, [FromBody] ProvideInfoRequest req)
        => ExecuteTransition(id, c => c.ProvideAdditionalInfo(req.ClaimantName, req.Response));

    [HttpPost("{id:guid}/settle")]
    public Task<IActionResult> Settle(Guid id, [FromBody] SettleClaimRequest req)
        => ExecuteTransition(id, c => c.Settle(req.OfficerId, req.SettledAmount, req.Notes));

    [HttpPost("{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id, [FromBody] RejectClaimRequest req)
        => ExecuteTransition(id, c => c.Reject(req.OfficerId, req.Reason));

    private async Task<IActionResult> ExecuteTransition(Guid id, Action<Claim> action)
    {
        var claim = await _db.Claims.Include(c => c.Timeline).FirstOrDefaultAsync(c => c.Id == id);
        if (claim is null) return NotFound();

        var prevStatus = claim.Status.ToString();
        try
        {
            action(claim);
            await _db.SaveChangesAsync();
            await PublishTransition(claim, prevStatus);
            return Ok(claim);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Task PublishTransition(Claim claim, string previousStatus)
        => _eventBus.PublishAsync(new ClaimDomainEvent(
            claim.Id, claim.ClaimNumber, claim.Market.ToString(), claim.Type.ToString(),
            previousStatus, claim.Status.ToString(), claim.EstimatedLiability, claim.SettledAmount, DateTime.UtcNow)).AsTask();
}