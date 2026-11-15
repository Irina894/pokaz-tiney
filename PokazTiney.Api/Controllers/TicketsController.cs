using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokazTiney.Api.Data;
using PokazTiney.Api.Services;

namespace PokazTiney.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TicketsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/tickets?sessionId=3
    [HttpGet]
    public async Task<IActionResult> GetTickets([FromQuery] int? sessionId, [FromQuery] int? userId, CancellationToken cancellationToken)
    {
        var me = HttpContext.User;
        var staff = me.IsStaff();

        // фільтр за користувачем: лише для власника або персоналу
        if (userId.HasValue && !staff)
        {
            if (me.GetUserId() is null) return Unauthorized();
            if (me.GetUserId() != userId) return Forbid();
        }

        await TicketHolds.ExpireAsync(_context, cancellationToken);

        var query = _context.Tickets.AsNoTracking();
        if (sessionId.HasValue) query = query.Where(t => t.SessionId == sessionId.Value);
        if (userId.HasValue) query = query.Where(t => t.UserId == userId.Value);

        var list = await query
            .OrderByDescending(t => t.Id)
            .Select(t => new
            {
                t.Id,
                t.SessionId,
                UserId = staff ? t.UserId : (int?)null,
                t.SeatNumber,
                Status = t.Status.ToString(),
                OrderNumber = staff ? t.OrderNumber : null,
                t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(list);
    }
}