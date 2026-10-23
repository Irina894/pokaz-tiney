using static System.Collections.Specialized.BitVector32;

namespace PokazTiney.Api.Models;

public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? TrailerUrl { get; set; }
    public double? Rating { get; set; }
    public DateTime? ReleaseDate { get; set; }

    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}