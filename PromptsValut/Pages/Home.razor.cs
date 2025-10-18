using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PromptsValut.Constants;
using PromptsValut.Models;
using PromptsValut.Services;
using PromptsValut.Components.Modals;

namespace PromptsValut.Pages;

public partial class Home : ComponentBase
{
    [Inject] private IPromptService PromptService { get; set; } = default!;
    [Inject] private ISeoService SeoService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private PromptDetailModal? promptDetailModal;
    private HelpModal? helpModal;
    private FavoritesModal? favoritesModal;
    private CreateModal? createModal;
    
    private bool showPromptDetail = false;
    private bool showCreateModal = false;

    protected override async Task OnInitializedAsync()
    {
        await PromptService.InitializeAsync();
        PromptService.StateChanged += StateHasChanged;
        
        // Generate dynamic SEO content
        await GenerateDynamicSeoContent();
    }

    private async Task GenerateDynamicSeoContent()
    {
        try
        {
            // Generate dynamic FAQ based on actual prompts
            await SeoService.GenerateDynamicFaqAsync(PromptService.Prompts);
            
            // Generate dynamic JSON-LD based on actual data
            await SeoService.GenerateDynamicJsonLdAsync(PromptService.Prompts, PromptService.Categories);
            
            // Track page view with dynamic data
            await SeoService.TrackPageViewAsync("Home", new Dictionary<string, object>
            {
                ["total_prompts"] = PromptService.Prompts.Count,
                ["total_categories"] = PromptService.Categories.Count,
                ["favorites_count"] = PromptService.State.Favorites.Count
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating dynamic SEO content: {ex.Message}");
        }
    }

    // SVG icons are inline - no initialization needed

    private async Task OnPromptClick(string promptId)
    {
        await PromptService.AddToHistoryAsync(promptId);
        var prompt = await PromptService.GetPromptByIdAsync(promptId);
        if (prompt != null && promptDetailModal != null)
        {
            await promptDetailModal.ShowPrompt(prompt);
            showPromptDetail = true;
            StateHasChanged();
        }
    }

    private async Task OnPromptClick(Prompt prompt)
    {
        await PromptService.AddToHistoryAsync(prompt.Id);
        if (promptDetailModal != null)
        {
            await promptDetailModal.ShowPrompt(prompt);
            showPromptDetail = true;
            StateHasChanged();
        }
    }

    private async Task OnToggleFavorite(string promptId)
    {
        await PromptService.ToggleFavoriteAsync(promptId);
    }

    private async Task OnSetRating((string promptId, UserRating rating) args)
    {
        await PromptService.SetRatingAsync(args.promptId, args.rating);
    }

    private async Task ShowFavorites()
    {
        await PromptService.SetShowFavoritesOnlyAsync(false); // Reset filter first
        await PromptService.ShowFavoritesModalAsync();
    }

    private void ShowCreateModal()
    {
        showCreateModal = true;
        StateHasChanged();
    }

    private void ClosePromptDetail()
    {
        showPromptDetail = false;
        StateHasChanged();
    }

    private async Task CloseHelpModal()
    {
        await PromptService.HideHelpModalAsync();
    }

    private async Task CloseFavoritesModal()
    {
        await PromptService.HideFavoritesModalAsync();
    }

    private void CloseCreateModal()
    {
        showCreateModal = false;
        StateHasChanged();
    }
}
