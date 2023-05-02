﻿using BlazorTextEditor.RazorLib.Keymap;
using BlazorTextEditor.RazorLib.Store.Model;
using BlazorTextEditor.RazorLib.Store.Options;
using Fluxor;
using Fluxor.Blazor.Web.Components;
using Microsoft.AspNetCore.Components;

namespace BlazorTextEditor.RazorLib.Options;

public partial class InputTextEditorKeymap : FluxorComponent
{
    [Inject]
    private IState<TextEditorModelsCollection> TextEditorModelsCollectionWrap { get; set; } = null!;
    [Inject]
    private IState<TextEditorOptionsState> TextEditorOptionsState { get; set; } = null!;
    [Inject]
    private ITextEditorService TextEditorService { get; set; } = null!;

    [Parameter]
    public string TopLevelDivElementCssClassString { get; set; } = string.Empty;
    [Parameter]
    public string InputElementCssClassString { get; set; } = string.Empty;
    [Parameter]
    public string LabelElementCssClassString { get; set; } = string.Empty;
    
    private void SelectedKeymapChanged(ChangeEventArgs changeEventArgs)
    {
        var allKeymapDefinitions = KeymapFacts.AllKeymapDefinitions;
        
        var chosenKeymapGuidString = changeEventArgs.Value?.ToString() ?? string.Empty;

        if (Guid.TryParse(chosenKeymapGuidString, 
                out var chosenKeymapKeyGuid))
        {
            var chosenKeymapKey = new KeymapKey(chosenKeymapKeyGuid);

            var foundKeymap = allKeymapDefinitions
                .FirstOrDefault(x => x.KeymapKey == chosenKeymapKey);
            
            if (foundKeymap is not null)
                TextEditorService.Options.OptionsSetKeymap(foundKeymap);
        }
        else
        {
            TextEditorService.Options.OptionsSetKeymap(KeymapFacts.DefaultKeymapDefinition);
        }
    }
}