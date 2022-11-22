﻿using BlazorTextEditor.RazorLib.JavaScriptObjects;
using BlazorTextEditor.RazorLib.TextEditor;
using Microsoft.AspNetCore.Components;

namespace BlazorTextEditor.RazorLib.TextEditorDisplayInternals;

public partial class ScrollbarSection : ComponentBase
{
    [Parameter, EditorRequired]
    public TextEditorBase TextEditor { get; set; } = null!;
    [Parameter, EditorRequired]
    public CharacterWidthAndRowHeight CharacterWidthAndRowHeight { get; set; } = null!;
    
    private const double SCROLLBAR_SIZE_IN_PIXELS = 30;
    
    private string GetScrollbarHorizontalStyleCss()
    {
        var gutterWidthInPixels = GetGutterWidthInPixels();
        
        var left = $"left: {gutterWidthInPixels}px;";

        var width = $"width: calc(100% - {gutterWidthInPixels}px);";

        return $"{left} {width}";
    }
    
    private string GetScrollbarVerticalStyleCss()
    {
        var gutterWidthInPixels = GetGutterWidthInPixels();

        var left = $"left: calc(100% - {SCROLLBAR_SIZE_IN_PIXELS}px);";

        return $"{left}";
    }
    
    private string GetScrollbarConnectorStyleCss()
    {
        var gutterWidthInPixels = GetGutterWidthInPixels();

        var left = $"left: calc(100% - {gutterWidthInPixels}px - {SCROLLBAR_SIZE_IN_PIXELS}px);";

        return $"{left}";
    }
    
    private double GetGutterWidthInPixels()
    {
        var mostDigitsInARowLineNumber = TextEditor.RowCount
            .ToString()
            .Length;

        var gutterWidthInPixels = mostDigitsInARowLineNumber *
                                  CharacterWidthAndRowHeight.CharacterWidthInPixels;

        gutterWidthInPixels += TextEditorBase.GUTTER_PADDING_LEFT_IN_PIXELS +
                               TextEditorBase.GUTTER_PADDING_RIGHT_IN_PIXELS;

        return gutterWidthInPixels;
    }
}