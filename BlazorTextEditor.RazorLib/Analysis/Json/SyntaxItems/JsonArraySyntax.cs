﻿using System.Collections.Immutable;
using BlazorTextEditor.RazorLib.Lexing;

namespace BlazorTextEditor.RazorLib.Analysis.Json.SyntaxItems;

public class JsonArraySyntax : IJsonSyntax
{
    public JsonArraySyntax(
        TextEditorTextSpan textEditorTextSpan,
        ImmutableArray<JsonObjectSyntax> childJsonObjectSyntaxes)
    {
        TextEditorTextSpan = textEditorTextSpan;
        ChildJsonObjectSyntaxes = childJsonObjectSyntaxes;
    }
    
    public TextEditorTextSpan TextEditorTextSpan { get; }
    public ImmutableArray<JsonObjectSyntax> ChildJsonObjectSyntaxes { get; }
    public ImmutableArray<IJsonSyntax> ChildJsonSyntaxes => new IJsonSyntax[]
    {

    }.Union(ChildJsonObjectSyntaxes)
        .ToImmutableArray();
    
    public JsonSyntaxKind JsonSyntaxKind => JsonSyntaxKind.Array;
}