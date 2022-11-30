﻿using System.Collections.Immutable;

namespace BlazorTextEditor.RazorLib.Analysis.Html.SyntaxItems;

public class TagTextSyntax : TagSyntax, IHtmlSyntax
{
    public TagTextSyntax(
        ImmutableArray<AttributeSyntax> attributeSyntaxes,
        ImmutableArray<IHtmlSyntax> childHtmlSyntaxes,
        string value,
        bool hasSpecialHtmlCharacter = false)
        : base(
            null,
            null,
            attributeSyntaxes,
            childHtmlSyntaxes,
            TagKind.Text,
            hasSpecialHtmlCharacter)
    {
        Value = value;
    }

    public string Value { get; }

    public override HtmlSyntaxKind HtmlSyntaxKind => HtmlSyntaxKind.TagText;
}