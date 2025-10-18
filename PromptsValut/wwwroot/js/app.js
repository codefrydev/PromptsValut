// Theme management with error handling
window.setTheme = (theme) => {
    try {
        const root = document.documentElement;
        if (theme === 'dark') {
            root.classList.add('dark');
        } else {
            root.classList.remove('dark');
        }
        localStorage.setItem('theme', theme);
    } catch (error) {
        console.error('Error setting theme:', error);
        // Fallback: just apply the theme without saving
        const root = document.documentElement;
        if (theme === 'dark') {
            root.classList.add('dark');
        } else {
            root.classList.remove('dark');
        }
    }
};

window.getTheme = () => {
    try {
        return localStorage.getItem('theme') || 'light';
    } catch (error) {
        console.error('Error getting theme:', error);
        return 'light';
    }
};

window.toggleTheme = () => {
    const currentTheme = window.getTheme();
    const newTheme = currentTheme === 'light' ? 'dark' : 'light';
    window.setTheme(newTheme);
    return newTheme;
};

// Initialize theme on page load
document.addEventListener('DOMContentLoaded', () => {
    const savedTheme = window.getTheme();
    window.setTheme(savedTheme);
});

// Toast notification system
window.showToast = (message, type = 'info') => {
    // Simple toast implementation - can be enhanced with a proper toast library
    const toast = document.createElement('div');
    toast.className = `fixed top-4 right-4 p-4 rounded-md shadow-lg z-50 ${
        type === 'success' ? 'bg-green-500 text-white' :
        type === 'error' ? 'bg-red-500 text-white' :
        type === 'warning' ? 'bg-yellow-500 text-white' :
        'bg-blue-500 text-white'
    }`;
    toast.textContent = message;
    
    document.body.appendChild(toast);
    
    setTimeout(() => {
        toast.remove();
    }, 3000);
};

// Copy to clipboard
window.copyToClipboard = async (text) => {
    try {
        await navigator.clipboard.writeText(text);
        window.showToast('Copied to clipboard!', 'success');
    } catch (err) {
        // Fallback for older browsers
        const textArea = document.createElement('textarea');
        textArea.value = text;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        document.body.removeChild(textArea);
        window.showToast('Copied to clipboard!', 'success');
    }
};

// Download text file
window.downloadTextFile = (fileName, content) => {
    try {
        const blob = new Blob([content], { type: 'text/plain' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
        window.showToast('File downloaded successfully!', 'success');
    } catch (error) {
        console.error('Error downloading file:', error);
        window.showToast('Error downloading file', 'error');
    }
};



// Close modal when clicking outside
document.addEventListener('click', (e) => {
    if (e.target.classList.contains('modal')) {
        e.target.style.display = 'none';
        document.body.style.overflow = 'auto';
    }
});

// Keyboard shortcuts
document.addEventListener('keydown', (e) => {
    // Escape key to close modals
    if (e.key === 'Escape') {
        const visibleModal = document.querySelector('.modal[style*="display: flex"]');
        if (visibleModal) {
            visibleModal.style.display = 'none';
            document.body.style.overflow = 'auto';
        }
    }
    
    // Ctrl/Cmd + K for search
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        const searchInput = document.querySelector('input[type="text"]');
        if (searchInput) {
            searchInput.focus();
        }
    }
});


// Screen size detection for responsive layout
window.getWindowWidth = () => {
    return window.innerWidth;
};

// Resize listener management for ResponsiveLayout component
window.addResizeListener = (dotNetObjectRef) => {
    const handleResize = () => {
        try {
            dotNetObjectRef.invokeMethodAsync('OnResize');
        } catch (error) {
            console.error('Error calling OnResize:', error);
        }
    };
    
    window.addEventListener('resize', handleResize);
    
    // Store the handler and reference for cleanup
    if (!window._resizeHandlers) {
        window._resizeHandlers = new Map();
    }
    window._resizeHandlers.set(dotNetObjectRef, handleResize);
};

window.removeResizeListener = (dotNetObjectRef) => {
    if (window._resizeHandlers && window._resizeHandlers.has(dotNetObjectRef)) {
        const handler = window._resizeHandlers.get(dotNetObjectRef);
        window.removeEventListener('resize', handler);
        window._resizeHandlers.delete(dotNetObjectRef);
    }
};

// SEO Management Functions
window.updatePageTitle = (title) => {
    document.title = title;
    
    // Update Open Graph title
    const ogTitle = document.querySelector('meta[property="og:title"]');
    if (ogTitle) {
        ogTitle.setAttribute('content', title);
    }
    
    // Update Twitter title
    const twitterTitle = document.querySelector('meta[property="twitter:title"]');
    if (twitterTitle) {
        twitterTitle.setAttribute('content', title);
    }
};

window.updateMetaDescription = (description) => {
    // Update meta description
    let metaDesc = document.querySelector('meta[name="description"]');
    if (!metaDesc) {
        metaDesc = document.createElement('meta');
        metaDesc.setAttribute('name', 'description');
        document.head.appendChild(metaDesc);
    }
    metaDesc.setAttribute('content', description);
    
    // Update Open Graph description
    const ogDesc = document.querySelector('meta[property="og:description"]');
    if (ogDesc) {
        ogDesc.setAttribute('content', description);
    }
    
    // Update Twitter description
    const twitterDesc = document.querySelector('meta[property="twitter:description"]');
    if (twitterDesc) {
        twitterDesc.setAttribute('content', description);
    }
};

window.updateCanonicalUrl = (url) => {
    let canonical = document.querySelector('link[rel="canonical"]');
    if (!canonical) {
        canonical = document.createElement('link');
        canonical.setAttribute('rel', 'canonical');
        document.head.appendChild(canonical);
    }
    canonical.setAttribute('href', url);
    
    // Update Open Graph URL
    const ogUrl = document.querySelector('meta[property="og:url"]');
    if (ogUrl) {
        ogUrl.setAttribute('content', url);
    }
    
    // Update Twitter URL
    const twitterUrl = document.querySelector('meta[property="twitter:url"]');
    if (twitterUrl) {
        twitterUrl.setAttribute('content', url);
    }
};

window.addJsonLd = (jsonData) => {
    // Remove existing JSON-LD scripts with the same type
    const existingScripts = document.querySelectorAll('script[type="application/ld+json"]');
    existingScripts.forEach(script => {
        if (script.dataset.dynamic === 'true') {
            script.remove();
        }
    });
    
    // Add new JSON-LD script
    const script = document.createElement('script');
    script.type = 'application/ld+json';
    script.dataset.dynamic = 'true';
    script.textContent = jsonData;
    document.head.appendChild(script);
};

window.trackPageView = (trackingData) => {
    // Google Analytics tracking
    if (typeof gtag !== 'undefined') {
        gtag('config', 'G-VM01Q3R43D', {
            page_title: trackingData.page_name,
            page_location: trackingData.page_location,
            custom_map: trackingData.custom_parameters
        });
        
        gtag('event', 'page_view', {
            page_title: trackingData.page_name,
            page_location: trackingData.page_location,
            ...trackingData.custom_parameters
        });
    }
};

// Performance optimization: Preload critical resources
window.preloadCriticalResources = () => {
    const criticalResources = [
        '/css/app.css',
        '/favicon-32x32.png',
        '/icon-192.png'
    ];
    
    criticalResources.forEach(resource => {
        const link = document.createElement('link');
        link.rel = 'preload';
        link.href = resource;
        link.as = resource.endsWith('.css') ? 'style' : 'image';
        document.head.appendChild(link);
    });
};

// Dynamic Sitemap Generation
window.generateDynamicSitemap = (sitemapData) => {
    const sitemapXml = `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"
        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        xsi:schemaLocation="http://www.sitemaps.org/schemas/sitemap/0.9
        http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd">
${sitemapData.urls.map(url => `    <url>
        <loc>${url.loc}</loc>
        <lastmod>${url.lastmod}</lastmod>
        <changefreq>${url.changefreq}</changefreq>
        <priority>${url.priority}</priority>
    </url>`).join('\n')}
</urlset>`;

    // Create and download the sitemap
    const blob = new Blob([sitemapXml], { type: 'application/xml' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'sitemap.xml';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
};

// Initialize performance optimizations
document.addEventListener('DOMContentLoaded', () => {
    window.preloadCriticalResources();
});

