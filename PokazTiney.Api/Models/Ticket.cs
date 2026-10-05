namespace PokazTiney.Api.Models;

public enum TicketStatus
{
    Pending,
    Paid,
    Cancelled
}

public class Ticket
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string SeatNumber { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}