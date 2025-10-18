using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using PromptsValut.Models;

namespace PromptsValut.Services
{
    public interface ISeoService
    {
        Task UpdatePageTitleAsync(string title);
        Task UpdateMetaDescriptionAsync(string description);
        Task UpdateCanonicalUrlAsync(string url);
        Task AddJsonLdAsync(object structuredData);
        Task TrackPageViewAsync(string pageName, Dictionary<string, object>? parameters = null);
        Task GenerateDynamicFaqAsync(List<Prompt> prompts);
        Task GenerateDynamicJsonLdAsync(List<Prompt> prompts, List<Category> categories);
        Task GenerateDynamicSitemapAsync(List<Prompt> prompts, List<Category> categories);
        Task UpdateSeoForPromptAsync(Prompt prompt);
        Task UpdateSeoForCategoryAsync(Category category);
    }

    public class SeoService : ISeoService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navigationManager;

        public SeoService(IJSRuntime jsRuntime, NavigationManager navigationManager)
        {
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
        }

        public async Task UpdatePageTitleAsync(string title)
        {
            await _jsRuntime.InvokeVoidAsync("updatePageTitle", title);
        }

        public async Task UpdateMetaDescriptionAsync(string description)
        {
            await _jsRuntime.InvokeVoidAsync("updateMetaDescription", description);
        }

        public async Task UpdateCanonicalUrlAsync(string url)
        {
            await _jsRuntime.InvokeVoidAsync("updateCanonicalUrl", url);
        }

        public async Task AddJsonLdAsync(object structuredData)
        {
            var json = JsonSerializer.Serialize(structuredData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            await _jsRuntime.InvokeVoidAsync("addJsonLd", json);
        }

        public async Task TrackPageViewAsync(string pageName, Dictionary<string, object>? parameters = null)
        {
            var trackingData = new
            {
                page_name = pageName,
                page_location = _navigationManager.Uri,
                custom_parameters = parameters ?? new Dictionary<string, object>()
            };

            await _jsRuntime.InvokeVoidAsync("trackPageView", trackingData);
        }

        public async Task GenerateDynamicFaqAsync(List<Prompt> prompts)
        {
            var faqItems = new List<object>();

            // Generate FAQs based on actual prompts and categories
            var categories = prompts.GroupBy(p => p.Category).ToList();
            
            // General FAQs
            faqItems.AddRange([
                new
                {
                    type = "Question",
                    name = "What is PromptVault?",
                    acceptedAnswer = new
                    {
                        type = "Answer",
                        text = $"PromptVault is a comprehensive AI prompt library featuring {prompts.Count}+ curated prompts for ChatGPT, Claude, and other AI models. Discover, create, and share powerful prompts to enhance your AI interactions."
                    }
                },
                new
                {
                    type = "Question", 
                    name = "How many prompts are available?",
                    acceptedAnswer = new
                    {
                        type = "Answer",
                        text = $"We currently have {prompts.Count} carefully curated prompts across {categories.Count} different categories, with new prompts added regularly."
                    }
                },
                new
                {
                    type = "Question",
                    name = "What categories of prompts are available?",
                    acceptedAnswer = new
                    {
                        type = "Answer",
                        text = $"Our prompts are organized into {categories.Count} categories including: {string.Join(", ", categories.Take(5).Select(c => c.Key))}, and more."
                    }
                }
            ]);

            // Category-specific FAQs
            foreach (var category in categories.Take(3))
            {
                var categoryPrompts = category.ToList();
                var topPrompt = categoryPrompts.OrderByDescending(p => p.AverageRating).FirstOrDefault();
                
                faqItems.Add(new
                {
                    type = "Question",
                    name = $"What are the best {category.Key} prompts?",
                    acceptedAnswer = new
                    {
                        type = "Answer",
                        text = $"Our {category.Key} category contains {categoryPrompts.Count} high-quality prompts. Our top-rated prompt is '{topPrompt?.Title}' with a {topPrompt?.AverageRating:F1} star rating."
                    }
                });
            }

            // Usage FAQs based on actual data
            var mostUsedPrompt = prompts.OrderByDescending(p => p.UsageCount).FirstOrDefault();
            if (mostUsedPrompt != null)
            {
                faqItems.Add(new
                {
                    type = "Question",
                    name = "What's the most popular prompt?",
                    acceptedAnswer = new
                    {
                        type = "Answer",
                        text = $"Our most popular prompt is '{mostUsedPrompt.Title}' with {mostUsedPrompt.UsageCount} uses. It's a {mostUsedPrompt.Category} prompt with a {mostUsedPrompt.AverageRating:F1} star rating."
                    }
                });
            }

            var faqSchema = new
            {
                context = "https://schema.org",
                type = "FAQPage",
                mainEntity = faqItems
            };

            await AddJsonLdAsync(faqSchema);
        }

        public async Task GenerateDynamicJsonLdAsync(List<Prompt> prompts, List<Category> categories)
        {
            var baseUrl = "https://codefrydev.in/PromptsValut/";
            
            // Website schema
            var websiteSchema = new
            {
                context = "https://schema.org",
                type = "WebSite",
                name = "PromptVault",
                alternateName = "AI Prompt Library",
                url = baseUrl,
                description = $"The ultimate AI prompt library featuring {prompts.Count}+ curated prompts for ChatGPT, Claude, and other AI models.",
                publisher = new
                {
                    type = "Organization",
                    name = "PromptVault",
                    url = baseUrl,
                    logo = new
                    {
                        type = "ImageObject",
                        url = $"{baseUrl}icon-192.png",
                        width = 192,
                        height = 192
                    }
                },
                potentialAction = new
                {
                    type = "SearchAction",
                    target = new
                    {
                        type = "EntryPoint",
                        urlTemplate = $"{baseUrl}?search={{search_term_string}}"
                    },
                    queryInput = "required name=search_term_string"
                },
                mainEntity = new
                {
                    type = "ItemList",
                    name = "AI Prompts Collection",
                    description = $"Curated collection of {prompts.Count} AI prompts across {categories.Count} categories",
                    numberOfItems = prompts.Count,
                    itemListElement = categories.Take(10).Select((category, index) => new
                    {
                        type = "ListItem",
                        position = index + 1,
                        name = $"{category.Name} Prompts",
                        description = $"Optimized prompts for {category.Name} tasks",
                        url = $"{baseUrl}?category={category.Id}"
                    }).ToArray()
                }
            };

            // Software Application schema
            var softwareSchema = new
            {
                context = "https://schema.org",
                type = "SoftwareApplication",
                name = "PromptVault",
                applicationCategory = "WebApplication",
                operatingSystem = "Web Browser",
                description = $"AI prompt library with {prompts.Count}+ prompts for ChatGPT, Claude, and other AI models",
                url = baseUrl,
                author = new
                {
                    type = "Organization",
                    name = "PromptVault"
                },
                offers = new
                {
                    type = "Offer",
                    price = "0",
                    priceCurrency = "USD"
                },
                aggregateRating = new
                {
                    type = "AggregateRating",
                    ratingValue = prompts.Any() ? prompts.Average(p => p.AverageRating).ToString("F1") : "4.5",
                    ratingCount = prompts.Count,
                    bestRating = "5",
                    worstRating = "1"
                }
            };

            await AddJsonLdAsync(websiteSchema);
            await AddJsonLdAsync(softwareSchema);
        }

        public async Task GenerateDynamicSitemapAsync(List<Prompt> prompts, List<Category> categories)
        {
            var baseUrl = "https://codefrydev.in/PromptsValut/";
            var currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            
            var sitemapUrls = new List<object>
            {
                new { loc = baseUrl, lastmod = currentDate, changefreq = "daily", priority = "1.0" },
                new { loc = $"{baseUrl}generator", lastmod = currentDate, changefreq = "weekly", priority = "0.8" }
            };

            // Add category pages
            foreach (var category in categories)
            {
                sitemapUrls.Add(new
                {
                    loc = $"{baseUrl}?category={category.Id}",
                    lastmod = currentDate,
                    changefreq = "weekly",
                    priority = "0.7"
                });
            }

            // Add popular prompt pages (if you have individual prompt pages)
            var popularPrompts = prompts.OrderByDescending(p => p.UsageCount).Take(20);
            foreach (var prompt in popularPrompts)
            {
                sitemapUrls.Add(new
                {
                    loc = $"{baseUrl}?prompt={prompt.Id}",
                    lastmod = prompt.UpdatedAt.ToString("yyyy-MM-dd"),
                    changefreq = "monthly",
                    priority = "0.6"
                });
            }

            // Add search pages for popular terms
            var popularTerms = new[] { "ai", "writing", "coding", "marketing", "business", "creative" };
            foreach (var term in popularTerms)
            {
                sitemapUrls.Add(new
                {
                    loc = $"{baseUrl}?search={term}",
                    lastmod = currentDate,
                    changefreq = "monthly",
                    priority = "0.5"
                });
            }

            var sitemapData = new
            {
                urls = sitemapUrls
            };

            await _jsRuntime.InvokeVoidAsync("generateDynamicSitemap", sitemapData);
        }

        public async Task UpdateSeoForPromptAsync(Prompt prompt)
        {
            var baseUrl = "https://codefrydev.in/PromptsValut/";
            
            // Update page title
            await UpdatePageTitleAsync($"{prompt.Title} - {prompt.Category} Prompt | PromptVault");
            
            // Update meta description
            var description = !string.IsNullOrEmpty(prompt.Description) 
                ? prompt.Description 
                : $"Use this {prompt.Category} prompt for AI models. Rated {prompt.AverageRating:F1}/5 stars with {prompt.UsageCount} uses.";
            
            await UpdateMetaDescriptionAsync(description);
            
            // Update canonical URL
            await UpdateCanonicalUrlAsync($"{baseUrl}?prompt={prompt.Id}");
            
            // Add prompt-specific JSON-LD
            var promptSchema = new
            {
                context = "https://schema.org",
                type = "CreativeWork",
                name = prompt.Title,
                description = description,
                author = new
                {
                    type = "Person",
                    name = prompt.Author ?? "PromptVault Community"
                },
                dateCreated = prompt.CreatedAt.ToString("yyyy-MM-dd"),
                dateModified = prompt.UpdatedAt.ToString("yyyy-MM-dd"),
                genre = prompt.Category,
                keywords = string.Join(", ", prompt.Tags ?? []),
                aggregateRating = new
                {
                    type = "AggregateRating",
                    ratingValue = prompt.AverageRating.ToString("F1"),
                    bestRating = "5",
                    worstRating = "1"
                },
                usageCount = prompt.UsageCount,
                url = $"{baseUrl}?prompt={prompt.Id}"
            };

            await AddJsonLdAsync(promptSchema);
        }

        public async Task UpdateSeoForCategoryAsync(Category category)
        {
            var baseUrl = "https://codefrydev.in/PromptsValut/";
            
            // Update page title
            await UpdatePageTitleAsync($"{category.Name} AI Prompts | PromptVault");
            
            // Update meta description
            var description = $"Discover {category.Name} AI prompts for ChatGPT, Claude, and other AI models. Curated collection of high-quality prompts.";
            
            await UpdateMetaDescriptionAsync(description);
            
            // Update canonical URL
            await UpdateCanonicalUrlAsync($"{baseUrl}?category={category.Id}");
            
            // Add category-specific JSON-LD
            var categorySchema = new
            {
                context = "https://schema.org",
                type = "CollectionPage",
                name = $"{category.Name} AI Prompts",
                description = description,
                url = $"{baseUrl}?category={category.Id}",
                mainEntity = new
                {
                    type = "ItemList",
                    name = $"{category.Name} Prompts Collection",
                    description = $"Curated collection of {category.Name} prompts for AI models"
                }
            };

            await AddJsonLdAsync(categorySchema);
        }
    }
}
