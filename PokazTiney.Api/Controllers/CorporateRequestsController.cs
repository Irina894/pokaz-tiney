using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokazTiney.Api.Data;
using PokazTiney.Api.Models;
using PokazTiney.Api.Services;

namespace PokazTiney.Api.Controllers;

public record CreateCorporateRequest(
    int? MovieId, int? UserId, string LocationType, string? Address, int Guests, string EventType,
    string Name, string Phone, string Email, string DesiredDate, string? Comment);

public record UpdateRequestStatus(string Status);

[ApiController]
[Route("api/[controller]")]
public class CorporateRequestsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CorporateRequestsController(AppDbContext context)
    {
        _context = context;
    }

    private static decimal Estimate(string type, int guests)
    {
        var basePrice = type == "cinema" ? 4500m : type == "restaurant" ? 6000m : 7500m;
        return basePrice + Math.Max(0, guests - 20) * 50m;
    }

    private static object Shape(CorporateRequest r, string? movieTitle) => new
    {
        id = r.Id,
        number = $"ЗП-{r.Id:D4}",
        contactName = r.ContactName,
        contactPhone = r.ContactPhone,
        contactEmail = r.ContactEmail,
        location = r.Location,
        locationType = r.LocationType,
        eventType = r.EventType,
        desiredDate = r.DesiredDate,
        guestsCount = r.GuestsCount,
        comment = r.Comment,
        status = r.Status.ToString(),
        movieId = r.MovieId,
        movieTitle,
        estimatedPrice = r.EstimatedPrice,
        createdAt = DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc)
    };

    // POST: api/corporaterequests
    [HttpPost]
    public async Task<IActionResult> Create(CreateCorporateRequest dto, CancellationToken ct)
    {
        var name = dto.Name?.Trim() ?? "";
        var phone = dto.Phone?.Trim() ?? "";
        var email = dto.Email?.Trim().ToLowerInvariant() ?? "";
        var type = (dto.LocationType ?? "").Trim().ToLowerInvariant();

        if (name.Length < 2) return BadRequest("Введіть ім'я.");
        if (phone.Length < 10) return BadRequest("Введіть коректний номер телефону.");
        if (!email.Contains('@') || email.Length < 5) return BadRequest("Введіть коректний email.");
        if (dto.Guests < 1 || dto.Guests > 500) return BadRequest("Кількість гостей має бути від 1 до 500.");
        if (type != "cinema" && type != "restaurant" && type != "custom") return BadRequest("Оберіть локацію.");
        if (type == "custom" && string.IsNullOrWhiteSpace(dto.Address)) return BadRequest("Вкажіть адресу проведення.");
        if (!DateTime.TryParse(dto.DesiredDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var when) || when <= DateTime.Now)
            return BadRequest("Оберіть дату й час у майбутньому.");

        string? movieTitle = null;
        if (dto.MovieId.HasValue)
        {
            movieTitle = await _context.Movies.Where(m => m.Id == dto.MovieId.Value).Select(m => m.Title).FirstOrDefaultAsync(ct);
            if (movieTitle is null) return BadRequest("Фільм не знайдено.");
        }

        var label = type == "cinema" ? "Кінозал" : type == "restaurant" ? "Ресторан" : dto.Address!.Trim();

        var request = new CorporateRequest
        {
            ContactName = name,
            ContactPhone = phone,
            ContactEmail = email,
            Location = label,
            LocationType = type,
            EventType = dto.EventType?.Trim() ?? "",
            MovieId = dto.MovieId,
            UserId = HttpContext.User.GetUserId(),
            DesiredDate = when,
            GuestsCount = dto.Guests,
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
            EstimatedPrice = Estimate(type, dto.Guests)
        };

        _context.CorporateRequests.Add(request);
        await _context.SaveChangesAsync(ct);
        return StatusCode(201, Shape(request, movieTitle));
    }

    // GET: api/corporaterequests/5  (власник заявки або персонал)
    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var r = await _context.CorporateRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return NotFound();
        if (!HttpContext.User.IsStaff() && r.UserId != HttpContext.User.GetUserId()) return Forbid();
        var title = r.MovieId.HasValue
            ? await _context.Movies.Where(m => m.Id == r.MovieId.Value).Select(m => m.Title).FirstOrDefaultAsync(ct)
            : null;
        return Ok(Shape(r, title));
    }

    // GET: api/corporaterequests  (заявки поточного користувача; персонал може вказати ?userId=)
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? userId, CancellationToken ct)
    {
        var me = HttpContext.User;
        var target = me.IsStaff() && userId.HasValue ? userId : me.GetUserId();
        if (target is null) return Unauthorized();
        var uid = target.Value;

        var list = await _context.CorporateRequests.AsNoTracking()
            .Where(r => r.UserId == uid).OrderByDescending(r => r.Id).ToListAsync(ct);
        var ids = list.Where(r => r.MovieId.HasValue).Select(r => r.MovieId!.Value).Distinct().ToList();
        var titles = await _context.Movies.Where(m => ids.Contains(m.Id)).ToDictionaryAsync(m => m.Id, m => m.Title, ct);
        return Ok(list.Select(r => Shape(r, r.MovieId.HasValue && titles.TryGetValue(r.MovieId.Value, out var t) ? t : null)));
    }

    // GET: api/corporaterequests/all?status=New  (менеджер або адміністратор)
    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("all")]
    public async Task<IActionResult> ListAll([FromQuery] string? status, CancellationToken ct)
    {
        var query = _context.CorporateRequests.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<RequestStatus>(status, true, out var filter))
                return BadRequest("Невідомий статус.");
            query = query.Where(r => r.Status == filter);
        }

        var list = await query.OrderByDescending(r => r.Id).ToListAsync(ct);
        var ids = list.Where(r => r.MovieId.HasValue).Select(r => r.MovieId!.Value).Distinct().ToList();
        var titles = await _context.Movies.Where(m => ids.Contains(m.Id)).ToDictionaryAsync(m => m.Id, m => m.Title, ct);
        return Ok(list.Select(r => Shape(r, r.MovieId.HasValue && titles.TryGetValue(r.MovieId.Value, out var t) ? t : null)));
    }

    // POST: api/corporaterequests/5/status  (менеджер або адміністратор)
    // Дозволені переходи: New -> UnderReview/Confirmed/Rejected, UnderReview -> Confirmed/Rejected
    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, UpdateRequestStatus dto, CancellationToken ct)
    {
        if (!Enum.TryParse<RequestStatus>(dto.Status, true, out var next))
            return BadRequest("Невідомий статус. Доступні: UnderReview, Confirmed, Rejected.");

        var r = await _context.CorporateRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return NotFound();

        var allowed = r.Status switch
        {
            RequestStatus.New => next is RequestStatus.UnderReview or RequestStatus.Confirmed or RequestStatus.Rejected,
            RequestStatus.UnderReview => next is RequestStatus.Confirmed or RequestStatus.Rejected,
            _ => false
        };
        if (!allowed) return Conflict($"Перехід зі статусу {r.Status} у {next} неможливий.");

        r.Status = next;
        await _context.SaveChangesAsync(ct);

        var title = r.MovieId.HasValue
            ? await _context.Movies.Where(m => m.Id == r.MovieId.Value).Select(m => m.Title).FirstOrDefaultAsync(ct)
            : null;
        return Ok(Shape(r, title));
    }
}