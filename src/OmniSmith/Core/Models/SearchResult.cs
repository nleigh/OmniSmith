namespace OmniSmith.Core.Models;

public class SearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // e.g., "CustomForge", "Musescore"
    public string? Rating { get; set; }
    public string? ThumbnailUrl { get; set; }
}
