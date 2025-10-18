using Microsoft.JSInterop;
using PromptsValut.Models;

namespace PromptsValut.Services;

public class BackgroundRefreshService : IBackgroundRefreshService
{
    private readonly IPromptService _promptService;
    private readonly ILocalStorageService _localStorage;
    private readonly IJSRuntime _jsRuntime;
    private Timer? _refreshTimer;
    private bool _isRunning = false;
    private DateTime _lastRefresh = DateTime.MinValue;
    private int _refreshIntervalMinutes = 60;
    private bool _isEnabled = true;
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);

    public bool IsRunning => _isRunning;
    public DateTime LastRefresh => _lastRefresh;
    
    public int RefreshIntervalMinutes 
    { 
        get => _refreshIntervalMinutes;
        set 
        {
            _refreshIntervalMinutes = Math.Max(1, value); // Minimum 1 minute
            if (_isRunning)
            {
                RestartTimer();
            }
        }
    }
    
    public bool IsEnabled 
    { 
        get => _isEnabled;
        set 
        {
            _isEnabled = value;
            if (!value && _isRunning)
            {
                StopAsync().Wait();
            }
            else if (value && !_isRunning)
            {
                StartAsync().Wait();
            }
        }
    }

    public BackgroundRefreshService(IPromptService promptService, ILocalStorageService localStorage, IJSRuntime jsRuntime)
    {
        _promptService = promptService;
        _localStorage = localStorage;
        _jsRuntime = jsRuntime;
    }

    public async Task StartAsync()
    {
        if (_isRunning || !_isEnabled)
            return;

        try
        {
            // Load settings from storage
            await LoadSettingsAsync();
            
            _isRunning = true;
            _refreshTimer = new Timer(OnTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromMinutes(_refreshIntervalMinutes));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BackgroundRefreshService: Error starting service: {ex.Message}");
            _isRunning = false;
        }
    }

    public async Task StopAsync()
    {
        if (!_isRunning)
            return;

        try
        {
            _refreshTimer?.Dispose();
            _refreshTimer = null;
            _isRunning = false;
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BackgroundRefreshService: Error stopping service: {ex.Message}");
        }
    }

    public async Task RefreshDataAsync()
    {
        if (!_isEnabled)
            return;

        await _refreshSemaphore.WaitAsync();
        try
        {
            // Check if we need to refresh based on cache metadata
            var shouldRefresh = await ShouldRefreshAsync();
            if (!shouldRefresh)
            {
                return;
            }

            // Perform the refresh
            await _promptService.RefreshExternalDataAsync();
            
            // Update cache metadata
            await UpdateCacheMetadataAsync();
            
            _lastRefresh = DateTime.UtcNow;
            
            // Show a subtle notification to the user
            await _jsRuntime.InvokeVoidAsync("showToast", "Data updated in background", "success");
        }
        catch (Exception ex)
        {
            // Don't show error toast for background failures to avoid annoying users
        }
        finally
        {
            _refreshSemaphore.Release();
        }
    }

    private async void OnTimerElapsed(object? state)
    {
        try
        {
            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            // Timer callback error - ignore silently
        }
    }

    private async Task<bool> ShouldRefreshAsync()
    {
        try
        {
            var state = _promptService.State;
            if (state?.CacheMetadata == null)
                return true;

            var cacheMetadata = state.CacheMetadata;
            
            // Always refresh if never refreshed before
            if (cacheMetadata.LastBackgroundRefresh == DateTime.MinValue)
                return true;

            // Check if refresh interval has passed
            var timeSinceLastRefresh = DateTime.UtcNow - cacheMetadata.LastBackgroundRefresh;
            var refreshInterval = TimeSpan.FromMinutes(cacheMetadata.RefreshIntervalMinutes);
            
            return timeSinceLastRefresh >= refreshInterval;
        }
        catch (Exception ex)
        {
            return true; // Default to refresh on error
        }
    }

    private async Task UpdateCacheMetadataAsync()
    {
        try
        {
            var state = _promptService.State;
            if (state?.CacheMetadata != null)
            {
                state.CacheMetadata.LastBackgroundRefresh = DateTime.UtcNow;
                state.CacheMetadata.LastUpdated = DateTime.UtcNow;
                state.CacheMetadata.IsStale = false;
                state.CacheMetadata.DataVersion = Guid.NewGuid().ToString("N")[..8]; // Short version string
                
                // Save the updated state
                await _localStorage.SetItemAsync("promptvault-state", state);
            }
        }
        catch (Exception ex)
        {
            // Error updating cache metadata - ignore silently
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var state = _promptService.State;
            if (state?.CacheMetadata != null)
            {
                _refreshIntervalMinutes = state.CacheMetadata.RefreshIntervalMinutes;
                _isEnabled = state.CacheMetadata.BackgroundRefreshEnabled;
                _lastRefresh = state.CacheMetadata.LastBackgroundRefresh;
            }
        }
        catch (Exception ex)
        {
            // Error loading settings - ignore silently
        }
    }

    private void RestartTimer()
    {
        _refreshTimer?.Dispose();
        if (_isRunning && _isEnabled)
        {
            _refreshTimer = new Timer(OnTimerElapsed, null, TimeSpan.FromMinutes(_refreshIntervalMinutes), TimeSpan.FromMinutes(_refreshIntervalMinutes));
        }
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _refreshSemaphore?.Dispose();
    }
}
