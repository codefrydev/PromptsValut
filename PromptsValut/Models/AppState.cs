namespace PromptsValut.Models;

public class AppState
{
    public List<Prompt> Prompts { get; set; } = [];
    public List<Category> Categories { get; set; } = [];
    public List<string> Favorites { get; set; } = [];
    public Dictionary<string, UserRating> UserRatings { get; set; } = new();
    public List<string> History { get; set; } = [];
    public string SelectedCategory { get; set; } = "all";
    public string SearchQuery { get; set; } = string.Empty;
    public string Theme { get; set; } = "light";
    public bool IsLoading { get; set; } = false;
    public string SortBy { get; set; } = "newest";
    public bool ShowFavoritesOnly { get; set; } = false;
    public CacheMetadata CacheMetadata { get; set; } = new();
}

public class CacheMetadata
{
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime LastBackgroundRefresh { get; set; } = DateTime.MinValue;
    public string DataVersion { get; set; } = string.Empty;
    public bool IsStale { get; set; } = false;
    public int RefreshIntervalMinutes { get; set; } = 60; // Default 1 hour
    public bool BackgroundRefreshEnabled { get; set; } = true;
}
