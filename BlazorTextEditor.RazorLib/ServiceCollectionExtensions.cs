using BlazorTextEditor.RazorLib.Autocomplete;
using BlazorTextEditor.RazorLib.Clipboard;
using BlazorTextEditor.RazorLib.Store.StorageCase;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BlazorTextEditor.RazorLib;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlazorTextEditor(
        this IServiceCollection services,
        Action<TextEditorServiceOptions>? configure = null)
    {
        return services
            .AddTextEditorClassLibServices(
                serviceProvider =>
                    new JavaScriptInteropClipboardProvider(
                        serviceProvider.GetRequiredService<IJSRuntime>()),
                serviceProvider =>
                    new LocalStorageProvider(
                        serviceProvider.GetRequiredService<IJSRuntime>()),
                serviceProvider =>
                    new AutocompleteService(serviceProvider.GetRequiredService<IAutocompleteIndexer>()),
                serviceProvider =>
                    new AutocompleteIndexer(serviceProvider.GetRequiredService<ITextEditorService>()),
                configure);
    }

    private static IServiceCollection AddTextEditorClassLibServices(
        this IServiceCollection services,
        Func<IServiceProvider, IClipboardProvider> clipboardProviderDefaultFactory,
        Func<IServiceProvider, IStorageProvider> storageProviderDefaultFactory,
        Func<IServiceProvider, IAutocompleteService> autocompleteServiceDefaultFactory,
        Func<IServiceProvider, IAutocompleteIndexer> autocompleteIndexerDefaultFactory,
        Action<TextEditorServiceOptions>? configure = null)
    {
        var textEditorOptions = new TextEditorServiceOptions();
        configure?.Invoke(textEditorOptions);

        var clipboardProviderFactory = textEditorOptions.ClipboardProviderFactory
                                       ?? clipboardProviderDefaultFactory;
        
        var storageProviderFactory = textEditorOptions.StorageProviderFactory
                                       ?? storageProviderDefaultFactory;
        
        var autocompleteServiceFactory = textEditorOptions.AutocompleteServiceFactory
                                                ?? autocompleteServiceDefaultFactory;
        
        var autocompleteIndexerFactory = textEditorOptions.AutocompleteIndexerFactory
                                                ?? autocompleteIndexerDefaultFactory;

        services
            .AddSingleton<ITextEditorServiceOptions, ImmutableTextEditorServiceOptions>(
                _ => new ImmutableTextEditorServiceOptions(textEditorOptions))
            .AddScoped(serviceProvider => clipboardProviderFactory.Invoke(serviceProvider))
            .AddScoped(serviceProvider => storageProviderFactory.Invoke(serviceProvider))
            .AddScoped(serviceProvider => autocompleteServiceFactory.Invoke(serviceProvider))
            .AddScoped(serviceProvider => autocompleteIndexerFactory.Invoke(serviceProvider))
            .AddScoped<IThemeService, ThemeService>()
            .AddScoped<ITextEditorService, TextEditorService>();

        if (textEditorOptions.InitializeFluxor)
        {
            services
                .AddFluxor(options => options
                    .ScanAssemblies(typeof(ServiceCollectionExtensions).Assembly));
        }

        return services;
    }
}