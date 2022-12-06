﻿using BlazorTextEditor.Demo.ClassLib.TestDataFolder;
using BlazorTextEditor.Demo.ClassLib.TextEditor;
using BlazorTextEditor.RazorLib;
using Microsoft.AspNetCore.Components;

namespace BlazorTextEditor.Demo.RazorLib.Pages;

public partial class Index : ComponentBase
{
    [Inject]
    private ITextEditorService TextEditorService { get; set; } = null!;

    protected override void OnInitialized()
    {
        TextEditorService.RegisterSvelteTextEditor(
            TextEditorFacts.Svelte.SvelteTextEditorKey,
            TestData.Svelte.EXAMPLE_TEXT);
        
        TextEditorService.RegisterRazorTextEditor(
            TextEditorFacts.Razor.RazorTextEditorKey,
            TestData.Razor.EXAMPLE_TEXT_20_LINES);
        
        base.OnInitialized();
    }
}