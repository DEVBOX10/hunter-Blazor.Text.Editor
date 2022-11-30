﻿using System.Collections;
using BlazorTextEditor.RazorLib.Lexing;

namespace BlazorTextEditor.RazorLib.Analysis;

public class TextEditorDiagnosticBag : IEnumerable<TextEditorDiagnostic>
{
    private readonly List<TextEditorDiagnostic> _textEditorDiagnostics = new();

    public IEnumerator<TextEditorDiagnostic> GetEnumerator()
    {
        return _textEditorDiagnostics.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Report(DiagnosticLevel diagnosticLevel,
        string message,
        TextEditorTextSpan textEditorTextSpan)
    {
        _textEditorDiagnostics.Add(
            new TextEditorDiagnostic(
                diagnosticLevel,
                message,
                textEditorTextSpan));
    }

    public void ReportEndOfFileUnexpected(TextEditorTextSpan textEditorTextSpan)
    {
        Report(
            DiagnosticLevel.Error,
            "'End of file' was unexpected.",
            textEditorTextSpan);
    }
    
    public void ReportUnexpectedToken(
        TextEditorTextSpan textEditorTextSpan,
        string unexpectedToken)
    {
        Report(
            DiagnosticLevel.Error,
            $"Unexpected token: '{unexpectedToken}'",
            textEditorTextSpan);
    }
}