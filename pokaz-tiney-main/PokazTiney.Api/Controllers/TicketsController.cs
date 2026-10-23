using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokazTiney.Api.Data;
using PokazTiney.Api.Models;

namespace PokazTiney.Api.Controllers;

public record CreateTicketRequest(int SessionId, int UserId, string SeatNumber);

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TicketsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/tickets
    // GET: api/tickets?userId=1
    [HttpGet]
    public async Task<IActionResult> GetTickets([FromQuery] int? userId, CancellationToken cancellationToken)
    {
        var query = _context.Tickets.AsNoTracking();

        if (userId.HasValue)
            query = query.Where(t => t.UserId == userId.Value);

        var tickets = await query
            .OrderByDescending(t => t.Id)
            .Select(t => new
            {
                t.Id,
                t.SessionId,
                t.UserId,
                t.SeatNumber,
                Status = t.Status.ToString(),
                t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(tickets);
    }

    // POST: api/tickets
    [HttpPost]
    public async Task<IActionResult> CreateTicket(CreateTicketRequest dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.SeatNumber))
            return BadRequest("Не вказано місце.");

        if (!await _context.Sessions.AnyAsync(s => s.Id == dto.SessionId, cancellationToken))
            return BadRequest($"Сеанс {dto.SessionId} не існує.");

        if (!await _context.Users.AnyAsync(u => u.Id == dto.UserId, cancellationToken))
            return BadRequest($"Користувач {dto.UserId} не існує.");

        var seatTaken = await _context.Tickets.AnyAsync(t =>
            t.SessionId == dto.SessionId &&
            t.SeatNumber == dto.SeatNumber &&
            t.Status != TicketStatus.Cancelled, cancellationToken);

        if (seatTaken)
            return Conflict("Це місце вже зайняте.");

        var ticket = new Ticket
        {
            SessionId = dto.SessionId,
            UserId = dto.UserId,
            SeatNumber = dto.SeatNumber,
            Status = TicketStatus.Pending
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync(cancellationToken);

        return StatusCode(201, new
        {
            ticket.Id,
            ticket.SessionId,
            ticket.UserId,
            ticket.SeatNumber,
            Status = ticket.Status.ToString(),
            ticket.CreatedAt
        });
    }

    // DELETE: api/tickets/5  (скасування: статус стає Cancelled, місце звільняється)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> CancelTicket(int id, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (ticket is null)
            return NotFound();

        ticket.Status = TicketStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}