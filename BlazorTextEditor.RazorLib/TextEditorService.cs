﻿using System.Collections.Immutable;
using BlazorALaCarte.DialogNotification.Dialog;
using BlazorALaCarte.Shared.Facts;
using BlazorALaCarte.Shared.Storage;
using BlazorALaCarte.Shared.Store;
using BlazorALaCarte.Shared.Theme;
using BlazorTextEditor.RazorLib.Analysis.CSharp.Decoration;
using BlazorTextEditor.RazorLib.Analysis.CSharp.SyntaxActors;
using BlazorTextEditor.RazorLib.Analysis.Css.Decoration;
using BlazorTextEditor.RazorLib.Analysis.Css.SyntaxActors;
using BlazorTextEditor.RazorLib.Analysis.FSharp.Decoration;
using BlazorTextEditor.RazorLib.Analysis.FSharp.SyntaxActors;
using BlazorTextEditor.RazorLib.Analysis.Html.Decoration;
using BlazorTextEditor.RazorLib.Analysis.Html.SyntaxActors;
using BlazorTextEditor.RazorLib.Analysis.JavaScript.Decoration;
using BlazorTextEditor.RazorLib.Analysis.JavaScript.SyntaxActors;
using BlazorTextEditor.RazorLib.Analysis.Json.Decoration;
using BlazorTextEditor.RazorLib.Analysis.Json.SyntaxActors;
using BlazorTextEditor.RazorLib.Analysis.Razor.SyntaxActors;
using BlazorTextEditor.RazorLib.Analysis.TypeScript.Decoration;
using BlazorTextEditor.RazorLib.Analysis.TypeScript.SyntaxActors;
using BlazorTextEditor.RazorLib.Group;
using BlazorTextEditor.RazorLib.HelperComponents;
using BlazorTextEditor.RazorLib.Keymap;
using BlazorTextEditor.RazorLib.Measurement;
using BlazorTextEditor.RazorLib.Model;
using BlazorTextEditor.RazorLib.Row;
using BlazorTextEditor.RazorLib.Store.StorageCase;
using BlazorTextEditor.RazorLib.Store.TextEditorCase.Group;
using BlazorTextEditor.RazorLib.Store.TextEditorCase.Model;
using BlazorTextEditor.RazorLib.Store.TextEditorCase.ViewModel;
using BlazorTextEditor.RazorLib.TextEditor;
using BlazorTextEditor.RazorLib.ViewModel;
using Fluxor;
using Microsoft.JSInterop;

namespace BlazorTextEditor.RazorLib;

public class TextEditorService : ITextEditorService
{
    private readonly IState<TextEditorModelsCollection> _textEditorModelsCollectionWrap;
    private readonly IState<ThemeState> _themeStateWrap;
    private readonly IState<TextEditorViewModelsCollection> _textEditorViewModelsCollectionWrap;
    private readonly IState<TextEditorGroupsCollection> _textEditorGroupsCollectionWrap;
    private readonly IDispatcher _dispatcher;
    private readonly IStorageProvider _storageProvider;
    
    // TODO: Perhaps do not reference IJSRuntime but instead wrap it in a 'IUiProvider' or something like that. The 'IUiProvider' would then expose methods that allow the TextEditorViewModel to adjust the scrollbars. 
    private readonly IJSRuntime _jsRuntime;

    public TextEditorService(
        IState<TextEditorModelsCollection> textEditorModelsCollectionWrap,
        IState<ThemeState> themeStateWrap,
        IState<TextEditorViewModelsCollection> textEditorViewModelsCollectionWrap,
        IState<TextEditorGroupsCollection> textEditorGroupsCollectionWrap,
        IDispatcher dispatcher,
        IStorageProvider storageProvider,
        IJSRuntime jsRuntime)
    {
        _textEditorModelsCollectionWrap = textEditorModelsCollectionWrap;
        _themeStateWrap = themeStateWrap;
        _textEditorViewModelsCollectionWrap = textEditorViewModelsCollectionWrap;
        _textEditorGroupsCollectionWrap = textEditorGroupsCollectionWrap;
        _dispatcher = dispatcher;
        _storageProvider = storageProvider;
        _jsRuntime = jsRuntime;

        _textEditorModelsCollectionWrap.StateChanged += TextEditorModelsCollectionWrapOnModelsCollectionWrapChanged;
    }

    public TextEditorModelsCollection TextEditorModelsCollection => _textEditorModelsCollectionWrap.Value;
    public ThemeRecord? GlobalThemeValue => TextEditorModelsCollection.GlobalTextEditorOptions.Theme;
    public string GlobalThemeCssClassString => TextEditorModelsCollection.GlobalTextEditorOptions.Theme?.CssClassString ?? string.Empty;
    public string GlobalFontSizeInPixelsStyling => $"font-size: {TextEditorModelsCollection.GlobalTextEditorOptions.FontSizeInPixels!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}px;";
    public bool GlobalShowNewlines => TextEditorModelsCollection.GlobalTextEditorOptions.ShowNewlines!.Value;
    public bool GlobalShowWhitespace => TextEditorModelsCollection.GlobalTextEditorOptions.ShowWhitespace!.Value;
    public int GlobalFontSizeInPixelsValue => TextEditorModelsCollection.GlobalTextEditorOptions.FontSizeInPixels!.Value;
    public double GlobalCursorWidthInPixelsValue => TextEditorModelsCollection.GlobalTextEditorOptions.CursorWidthInPixels!.Value;
    public KeymapDefinition GlobalKeymapDefinition => TextEditorModelsCollection.GlobalTextEditorOptions.KeymapDefinition!;
    public int? GlobalHeightInPixelsValue => TextEditorModelsCollection.GlobalTextEditorOptions.HeightInPixels;

    public event Action? TextEditorModelsCollectionChanged;
    
    public void RegisterCustomTextEditor(
        TextEditorModel textEditorModel)
    {
        _dispatcher.Dispatch(
            new RegisterTextEditorModelAction(textEditorModel));
    }

    public void RegisterCSharpTextEditor(
        TextEditorModelKey textEditorModelKey,
        string resourceUri,
        DateTime resourceLastWriteTime,
        string fileExtension,
        string initialContent,
        ITextEditorKeymap? textEditorKeymapOverride = null)
    {
        var textEditorModel = new TextEditorModel(
            resourceUri,
            resourceLastWriteTime,
            fileExtension,
            initialContent,
            new TextEditorCSharpLexer(),
            new TextEditorCSharpDecorationMapper(),
            null,
            textEditorModelKey);

        _ = Task.Run(async () =>
        {
            await textEditorModel.ApplySyntaxHighlightingAsync();
        });
        
        _dispatcher.Dispatch(
            new RegisterTextEditorModelAction(textEditorModel));
    }
    
    public void RegisterHtmlTextEditor(
        TextEditorModelKey textEditorModelKey, 
        string resourceUri,
        DateTime resourceLastWriteTime,
        string fileExtension,
        string initialContent,
        ITextEditorKeymap? textEditorKeymapOverride = null)
    {
        var textEditorModel = new TextEditorModel(
            resourceUri,
            resourceLastWriteTime,
            fileExtension,
            initialContent,
            new TextEditorHtmlLexer(),
            new TextEditorHtmlDecorationMapper(),
            null,
            textEditorModelKey);
        
        _ = Task.Run(async () =>
        {
            await textEditorModel.ApplySyntaxHighlightingAsync();
        });
        
        _dispatcher.Dispatch(
            new RegisterTextEditorModelAction(textEditorModel));
    }

    public void RegisterCssTextEditor(
        TextEditorModelKey textEditorModelKey, 
        string resourceUri,
        DateTime resourceLastWriteTime,
        string fileExtension,
        string initialContent,
        ITextEditorKeymap? textEditorKeymapOverride = null)
    {
        var textEditorModel = new TextEditorModel(
            resourceUri,
            resourceLastWriteTime,
            fileExtension,
            initialContent,
            new TextEditorCssLexer(),
            new TextEditorCssDecorationMapper(),
            null,
            textEditorModelKey);
        
        _ = Task.Run(async () =>
        {
            await textEditorModel.ApplySyntaxHighlightingAsync();
        });
        
        _dispatcher.Dispatch(
            new RegisterTextEditorModelAction(textEditorModel));
    }
    
    public void RegisterJsonTextEditor(
        TextEditorModelKey textEditorModelKey,
        string resourceUri,
        DateTime resourceLastWriteTime,
        string fileExtension,
        string initialContent,
        ITextEditorKeymap? textEditorKeymapOverride = null)
    {
        var textEditorModel = new TextEditorModel(
            resourceUri,
            resourceLastWriteTime,
            fileExtension,
            initialContent,
            new TextEditorJsonLexer(),
            new TextEditorJsonDecorationMapper(),
            null,
            textEditorModelKey);
        
        _ = Task.Run(async () =>
        {
            await textEditorModel.ApplySyntaxHighlightingAsync();
        });
        
        _dispatcher.Dispatch(
            new RegisterTextEditorModelAction(textEditorModel));
    }

    public void RegisterFSharpTextEditor(
        TextEditorModelKey textEditorModelKey,
        string resourceUri,
        DateTime resourceLastWriteTime,
        string fileExtension,
        string initialContent,
        ITextEditorKeymap? textEditorKeymapOverride = null)
    {
        var textEditorModel = new TextEditorModel(
            resourceUri,
            resourceLastWriteTime,
            fileExtension,
            initialContent,
            new TextEditorFSharpLexer(),
            new TextEditorFSharpDecorationMapper(),
            null,
            textEditorModelKey);
        
        _ = Task.Run(async () =>
        {
            await textEditorModel.ApplySyntaxHighlightingAsync();
        });
        
        _dispatcher.Dispatch(
            new RegisterTextEditorModelAction(textEditorModel));
    }

    public void RegisterRazorTextEditor(
        TextEditorModelKey textEditorModelKey,
        string resourceUri,
        DateTime resourceLastWriteTime,
        string fileExtension,
        string initialContent,
        ITextEditorKeymap? textEditorKeymapOverride = null)
    {
        var textEditorModel = new TextEditorModel(
            resourceUri,
            resourceLastWriteTime,
            fileExtension,
            initialContent,
            new TextEditorRazorLexer(),
            new TextEditorHtmlDecorationMapper(),
            null,
            textEditorModelKey);
        
        _ = Task.Run(async () =>
        {
            await textEditorModel.ApplySyntaxHighlightingAsync();
        });
        
        _dispatcher.Dispatch(
            new RegisterTextEditorModelAction(textEditorModel));
    }

    public void RegisterJavaScriptTextEditor(
        TextEditorModelKey textEditorModelKey,
        string resourceUri,
        DateTime resourceLastWriteTime,
        string fileExtension,
        string initialContent,
        ITextEditorKeymap? textEditorKeymapOverride = null)
    {
        var textEditorModel = new TextEditorModel(
            resourceUri,
            resourceLastWriteTime,
            fileExtension,
            initialContent,
            new TextEditorJavaScriptLexer(),
            new TextEditorJavaScriptDecorationMapper(),
            null,
            textEditorModelKey);
        
        _ = Task.Run(async () =>
        {
            await textEditorModel.ApplySyntaxHighlightingAsync();
        });
        
        _dispatcher.Dispatch(
            new RegisterTextEditorModelAction(textEditorModel));
    }
    
    public void RegisterTypeScriptTextEditor(
        TextEditorModelKey textEditorModelKey,
        string resourceUri,
        DateTime resourceLastWriteTime,
        string fileExtension,
        string initialContent,
        ITextEditorKeymap? textEditorKeymapOverride = null)
    {
        var textEditorModel = new TextEditorModel(
            resourceUri,
            resourceLastWriteTime,
            fileExtension,
            initialContent,
            new TextEditorTypeScriptLexer(),
            new TextEditorTypeScriptDecorationMapper(),
            null,
            textEditorModelKey);
        
        _ = Task.Run(async () =>
        {
            await textEditorModel.ApplySyntaxHighlightingAsync();
        });
        
        _dispatcher.Dispatch(
            new RegisterTextEditorModelAction(textEditorModel));
    }
    
    public void RegisterPlainTextEditor(
        TextEditorModelKey textEditorModelKey,
        string resourceUri,
        DateTime resourceLastWriteTime,
        string fileExtension,
        string initialContent,
        ITextEditorKeymap? textEditorKeymapOverride = null)
    {
        var textEditorModel = new TextEditorModel(
            resourceUri,
            resourceLastWriteTime,
            fileExtension,
            initialContent,
            null,
            null,
            null,
            textEditorModelKey);
        
        _dispatcher.Dispatch(
            new RegisterTextEditorModelAction(textEditorModel));
    }

    public string? GetAllText(TextEditorModelKey textEditorModelKey)
    {
        return TextEditorModelsCollection.TextEditorList
            .FirstOrDefault(x => x.ModelKey == textEditorModelKey)
            ?.GetAllText();
    }
    
    public string? GetAllText(TextEditorViewModelKey textEditorViewModelKey)
    {
        var textEditorModel = GetTextEditorModelFromViewModelKey(textEditorViewModelKey);

        return textEditorModel is null 
            ? null 
            : GetAllText(textEditorModel.ModelKey);
    }

    public void InsertText(InsertTextTextEditorModelAction insertTextTextEditorModelAction)
    {
        _dispatcher.Dispatch(insertTextTextEditorModelAction);
    }

    public void HandleKeyboardEvent(KeyboardEventTextEditorModelAction keyboardEventTextEditorModelAction)
    {
        _dispatcher.Dispatch(keyboardEventTextEditorModelAction);
    }
    
    public void DeleteTextByMotion(DeleteTextByMotionTextEditorModelAction deleteTextByMotionTextEditorModelAction)
    {
        _dispatcher.Dispatch(deleteTextByMotionTextEditorModelAction);
    }
    
    public void DeleteTextByRange(DeleteTextByRangeTextEditorModelAction deleteTextByRangeTextEditorModelAction)
    {
        _dispatcher.Dispatch(deleteTextByRangeTextEditorModelAction);
    }
    
    public void RedoEdit(TextEditorModelKey textEditorModelKey)
    {
        var redoEditAction = new RedoEditAction(textEditorModelKey);
        
        _dispatcher.Dispatch(redoEditAction);
    }
    
    public void UndoEdit(TextEditorModelKey textEditorModelKey)
    {
        var undoEditAction = new UndoEditAction(textEditorModelKey);
        
        _dispatcher.Dispatch(undoEditAction);
    }

    public void DisposeTextEditor(TextEditorModelKey textEditorModelKey)
    {
        _dispatcher.Dispatch(
            new DisposeTextEditorModelAction(textEditorModelKey));
    }

    public void SetFontSize(int fontSizeInPixels)
    {
        _dispatcher.Dispatch(
            new TextEditorSetFontSizeAction(fontSizeInPixels));
        
        WriteGlobalTextEditorOptionsToLocalStorage();
    }
    
    public void SetCursorWidth(double cursorWidthInPixels)
    {
        _dispatcher.Dispatch(
            new TextEditorSetCursorWidthAction(cursorWidthInPixels));
        
        WriteGlobalTextEditorOptionsToLocalStorage();
    }
    
    public void SetHeight(int? heightInPixels)
    {
        _dispatcher.Dispatch(
            new TextEditorSetHeightAction(heightInPixels));
        
        WriteGlobalTextEditorOptionsToLocalStorage();
    }

    public void SetTheme(ThemeRecord theme)
    {
        _dispatcher.Dispatch(
            new TextEditorSetThemeAction(theme));
        
        WriteGlobalTextEditorOptionsToLocalStorage();
    }
    
    public void SetKeymap(KeymapDefinition foundKeymap)
    {
        _dispatcher.Dispatch(
            new TextEditorSetKeymapAction(foundKeymap));
        
        WriteGlobalTextEditorOptionsToLocalStorage();
    }

    public void SetShowWhitespace(bool showWhitespace)
    {
        _dispatcher.Dispatch(
            new TextEditorSetShowWhitespaceAction(showWhitespace));
        
        WriteGlobalTextEditorOptionsToLocalStorage();
    }

    public void SetShowNewlines(bool showNewlines)
    {
        _dispatcher.Dispatch(
            new TextEditorSetShowNewlinesAction(showNewlines));

        WriteGlobalTextEditorOptionsToLocalStorage();
    }

    public void SetUsingRowEndingKind(TextEditorModelKey textEditorModelKey, RowEndingKind rowEndingKind)
    {
        _dispatcher.Dispatch(
            new TextEditorSetUsingRowEndingKindAction(textEditorModelKey, rowEndingKind));
    }
    
    public void SetResourceData(
        TextEditorModelKey textEditorModelKey,
        string resourceUri,
        DateTime resourceLastWriteTime)
    {
        _dispatcher.Dispatch(
            new TextEditorSetResourceDataAction(
                textEditorModelKey,
                resourceUri,
                resourceLastWriteTime));
    }

    public void ShowSettingsDialog(bool isResizable = false)
    {
        var settingsDialog = new DialogRecord(
            DialogKey.NewDialogKey(), 
            "Text Editor Settings",
            typeof(TextEditorSettings),
            null)
        {
            IsResizable = isResizable
        };
        
        _dispatcher.Dispatch(
            new DialogsState.RegisterDialogRecordAction(
                settingsDialog));
    }
    
    private void TextEditorModelsCollectionWrapOnModelsCollectionWrapChanged(object? sender, EventArgs e)
    {
        TextEditorModelsCollectionChanged?.Invoke();
    }

    public void RegisterGroup(TextEditorGroupKey textEditorGroupKey)
    {
        var textEditorGroup = new TextEditorGroup(
            textEditorGroupKey,
            TextEditorViewModelKey.Empty,
            ImmutableList<TextEditorViewModelKey>.Empty);
        
        _dispatcher.Dispatch(new RegisterTextEditorGroupAction(textEditorGroup));
    }

    public void AddViewModelToGroup(
        TextEditorGroupKey textEditorGroupKey,
        TextEditorViewModelKey textEditorViewModelKey)
    {
        _dispatcher.Dispatch(new TextEditorGroupsCollection.AddViewModelToGroupAction(
            textEditorGroupKey,
            textEditorViewModelKey));
    }
    
    public void RemoveViewModelFromGroup(
        TextEditorGroupKey textEditorGroupKey,
        TextEditorViewModelKey textEditorViewModelKey)
    {
        _dispatcher.Dispatch(new TextEditorGroupsCollection.RemoveViewModelFromGroupAction(
            textEditorGroupKey,
            textEditorViewModelKey));
    }
    
    public void SetActiveViewModelOfGroup(
        TextEditorGroupKey textEditorGroupKey,
        TextEditorViewModelKey textEditorViewModelKey)
    {
        _dispatcher.Dispatch(new TextEditorGroupsCollection.SetActiveViewModelOfGroupAction(
            textEditorGroupKey,
            textEditorViewModelKey));
    }

    public void RegisterViewModel(
        TextEditorViewModelKey textEditorViewModelKey,
        TextEditorModelKey textEditorModelKey)
    {
        _dispatcher.Dispatch(new TextEditorViewModelsCollection.RegisterAction(
            textEditorViewModelKey,
            textEditorModelKey, 
            this));
    }

    public ImmutableArray<TextEditorViewModel> GetViewModelsForModel(TextEditorModelKey textEditorModelKey)
    {
        return _textEditorViewModelsCollectionWrap.Value.ViewModelsList
            .Where(x => x.TextEditorModelKey == textEditorModelKey)
            .ToImmutableArray();
    }

    public TextEditorModel? GetTextEditorModelFromViewModelKey(TextEditorViewModelKey textEditorViewModelKey)
    {
        var textEditorViewModelsCollection = _textEditorViewModelsCollectionWrap.Value;
        
        var viewModel = textEditorViewModelsCollection.ViewModelsList
            .FirstOrDefault(x => 
                x.TextEditorViewModelKey == textEditorViewModelKey);
        
        if (viewModel is null)
            return null;
        
        return GetTextEditorModelOrDefault(viewModel.TextEditorModelKey);
    }

    public void SetViewModelWith(
        TextEditorViewModelKey textEditorViewModelKey,
        Func<TextEditorViewModel, TextEditorViewModel> withFunc)
    {
        _dispatcher.Dispatch(new TextEditorViewModelsCollection.SetViewModelWithAction(
            textEditorViewModelKey,
            withFunc));
    }

    public async Task SetGutterScrollTopAsync(string gutterElementId, double scrollTop)
    {
        await _jsRuntime.InvokeVoidAsync(
            "blazorTextEditor.setGutterScrollTop",
            gutterElementId,
            scrollTop);
        
        // Blazor WebAssembly as of this comment is single threaded and
        // the UI freezes without this await Task.Yield
        await Task.Yield();
    }

    public async Task MutateScrollHorizontalPositionByPixelsAsync(
        string bodyElementId,
        string gutterElementId,
        double pixels)
    {
        await _jsRuntime.InvokeVoidAsync(
            "blazorTextEditor.mutateScrollHorizontalPositionByPixels",
            bodyElementId,
            gutterElementId,
            pixels);
        
        // Blazor WebAssembly as of this comment is single threaded and
        // the UI freezes without this await Task.Yield
        await Task.Yield();
    }
    
    public async Task MutateScrollVerticalPositionByPixelsAsync(
        string bodyElementId,
        string gutterElementId,
        double pixels)
    {
        await _jsRuntime.InvokeVoidAsync(
            "blazorTextEditor.mutateScrollVerticalPositionByPixels",
            bodyElementId,
            gutterElementId,
            pixels);
        
        // Blazor WebAssembly as of this comment is single threaded and
        // the UI freezes without this await Task.Yield
        await Task.Yield();
    }

    /// <summary>
    /// If a parameter is null the JavaScript will not modify that value
    /// </summary>
    public async Task SetScrollPositionAsync(
        string bodyElementId,
        string gutterElementId,
        double? scrollLeft,
        double? scrollTop)
    {
        await _jsRuntime.InvokeVoidAsync(
            "blazorTextEditor.setScrollPosition",
            bodyElementId,
            gutterElementId,
            scrollLeft,
            scrollTop);
        
        // Blazor WebAssembly as of this comment is single threaded and
        // the UI freezes without this await Task.Yield
        await Task.Yield();
    }

    public async Task<ElementMeasurementsInPixels> GetElementMeasurementsInPixelsById(string elementId)
    {
        return await _jsRuntime.InvokeAsync<ElementMeasurementsInPixels>(
            "blazorTextEditor.getElementMeasurementsInPixelsById",
            elementId);
    }
    
    public TextEditorModel? GetTextEditorModelOrDefaultByResourceUri(string resourceUri)
    {
        return TextEditorModelsCollection.TextEditorList
            .FirstOrDefault(x => x.ResourceUri == resourceUri);
    }
    
    public void ReloadTextEditorModel(
        TextEditorModelKey textEditorModelKey,
        string content)
    {
        _dispatcher.Dispatch(
            new ReloadTextEditorModelAction(
                textEditorModelKey,
                content));
    }
    
    public TextEditorModel? GetTextEditorModelOrDefault(TextEditorModelKey textEditorModelKey)
    {
        return TextEditorModelsCollection.TextEditorList
            .FirstOrDefault(x => x.ModelKey == textEditorModelKey);
    }
    
    public TextEditorViewModel? GetTextEditorViewModelOrDefault(TextEditorViewModelKey textEditorViewModelKey)
    {
        return _textEditorViewModelsCollectionWrap.Value.ViewModelsList
            .FirstOrDefault(x => 
                x.TextEditorViewModelKey == textEditorViewModelKey);
    }
    
    public TextEditorGroup? GetTextEditorGroupOrDefault(TextEditorGroupKey textEditorGroupKey)
    {
        return _textEditorGroupsCollectionWrap.Value.GroupsList
            .FirstOrDefault(x => 
                x.TextEditorGroupKey == textEditorGroupKey);
    }
    
    public async Task FocusPrimaryCursorAsync(string primaryCursorContentId)
    {
        await _jsRuntime.InvokeVoidAsync(
            "blazorTextEditor.focusHtmlElementById",
            primaryCursorContentId);
    }
    
    public async Task SetTextEditorOptionsFromLocalStorageAsync()
    {
        var optionsJsonString = (await _storageProvider
            .GetValue(ITextEditorService.LOCAL_STORAGE_GLOBAL_TEXT_EDITOR_OPTIONS_KEY))
                as string;

        if (string.IsNullOrWhiteSpace(optionsJsonString))
            return;
        
        var options = System.Text.Json.JsonSerializer
            .Deserialize<TextEditorOptions>(optionsJsonString);

        if (options is null)
            return;
        
        if (options.Theme is not null)
        {
            var matchedTheme = _themeStateWrap.Value.ThemeRecordsList
                .FirstOrDefault(x =>
                    x.ThemeKey == options.Theme.ThemeKey);

            SetTheme(matchedTheme ?? ThemeFacts.VisualStudioDarkThemeClone);
        }
        
        if (options.KeymapDefinition is not null)
        {
            var matchedKeymapDefinition = KeymapFacts.AllKeymapDefinitions
                .FirstOrDefault(x =>
                    x.KeymapKey == options.KeymapDefinition.KeymapKey);
            
            SetKeymap(matchedKeymapDefinition ?? KeymapFacts.DefaultKeymapDefinition);
        }
        
        if (options.FontSizeInPixels is not null)
            SetFontSize(options.FontSizeInPixels.Value);
        
        if (options.CursorWidthInPixels is not null)
            SetCursorWidth(options.CursorWidthInPixels.Value);
        
        if (options.HeightInPixels is not null)
            SetHeight(options.HeightInPixels.Value);
        
        if (options.ShowNewlines is not null)
            SetShowNewlines(options.ShowNewlines.Value);
        
        if (options.ShowWhitespace is not null)
            SetShowWhitespace(options.ShowWhitespace.Value);
    }
    
    public void WriteGlobalTextEditorOptionsToLocalStorage()
    {
        _dispatcher.Dispatch(
            new StorageEffects.WriteGlobalTextEditorOptionsToLocalStorageAction(
                TextEditorModelsCollection.GlobalTextEditorOptions));
    }
    
    public void Dispose()
    {
        _textEditorModelsCollectionWrap.StateChanged -= TextEditorModelsCollectionWrapOnModelsCollectionWrapChanged;
    }
}