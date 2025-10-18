using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PromptsValut.Constants;
using PromptsValut.Services;
using PromptsValut.Components.UI;

namespace PromptsValut.Layout;

public partial class ResponsiveLayout : LayoutComponentBase, IDisposable
{
    [Inject] private IPromptService PromptService { get; set; } = default!;
    [Inject] private IBackgroundRefreshService BackgroundRefreshService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private Header? header;
    private CategoryFilter? categoryFilter;
    private bool isMobile = false;
    private bool isTablet = false;
    private bool isInitialized = false;
    private bool sidebarOpen = false;

    protected override async Task OnInitializedAsync()
    {
        PromptService.StateChanged += StateHasChanged;
        await CheckScreenSize();
        
        // Initialize the app and start background refresh service
        await PromptService.InitializeAsync();
        await BackgroundRefreshService.StartAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSRuntime.InvokeVoidAsync("addResizeListener", DotNetObjectReference.Create(this));
            await CheckScreenSize();
        }
    }

    [JSInvokable]
    public async Task OnResize()
    {
        await CheckScreenSize();
    }

    private async Task CheckScreenSize()
    {
        try
        {
            var width = await JSRuntime.InvokeAsync<int>("getWindowWidth");
            var wasMobile = isMobile;
            var wasTablet = isTablet;
            
            // Define breakpoints: mobile < 768px, tablet 768px-1024px, desktop > 1024px
            isMobile = width < 768;
            isTablet = width >= 768 && width < 1024;
            
            if (wasMobile != isMobile || wasTablet != isTablet || !isInitialized)
            {
                isInitialized = true;
                StateHasChanged();
            }
        }
        catch
        {
            // Fallback to desktop if JS interop fails
            isMobile = false;
            isTablet = false;
            if (!isInitialized)
            {
                isInitialized = true;
                StateHasChanged();
            }
        }
    }
    private async Task NavigateToGenerator()
    {
        Navigation.NavigateTo("generator");
        await Task.CompletedTask;
    }

    private async Task NavigateToHome()
    {
        Navigation.NavigateTo("");
        await Task.CompletedTask;
    }
    private void ToggleSidebar()
    {
        sidebarOpen = !sidebarOpen;
        StateHasChanged();
    }

    private async Task ToggleTheme()
    {
        await PromptService.ToggleThemeAsync();
    }

    private async Task ShowHelp()
    {
        await PromptService.ShowHelpModalAsync();
    }

    private async Task OnSearchChanged(string searchQuery)
    {
        await PromptService.SetSearchQueryAsync(searchQuery);
    }

    private async Task OnCategoryChanged(string category)
    {
        await PromptService.SetSelectedCategoryAsync(category);
        // Close sidebar after category selection on mobile and tablet
        if (isMobile || isTablet)
        {
            sidebarOpen = false;
            StateHasChanged();
        }
    }

    private async Task ShowFavorites()
    {
        if (isMobile || isTablet)
        {
            // Mobile/Tablet: Filter to show only favorites
            await PromptService.SetShowFavoritesOnlyAsync(true);
            sidebarOpen = false;
            StateHasChanged();
        }
        else
        {
            // Desktop: Show favorites modal
            await PromptService.ShowFavoritesModalAsync();
        }
    }

    private async Task ShowAllPrompts()
    {
        await PromptService.SetShowFavoritesOnlyAsync(false);
        await PromptService.SetSelectedCategoryAsync("all");
    }

    public void Dispose()
    {
        PromptService.StateChanged -= StateHasChanged;
        
        // Stop background refresh service
        try
        {
            BackgroundRefreshService.StopAsync().Wait();
        }
        catch
        {
            // Ignore errors during cleanup
        }
        
        // Clean up resize listener to prevent memory leaks
        try
        {
            _ = JSRuntime.InvokeVoidAsync("removeResizeListener", DotNetObjectReference.Create(this));
        }
        catch
        {
            // Ignore errors during cleanup
        }
    }
}
