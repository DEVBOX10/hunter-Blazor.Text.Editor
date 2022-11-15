﻿using System.Collections.Immutable;
using BlazorTextEditor.RazorLib.Character;
using BlazorTextEditor.RazorLib.Cursor;
using BlazorTextEditor.RazorLib.Decoration;
using BlazorTextEditor.RazorLib.Editing;
using BlazorTextEditor.RazorLib.Keyboard;
using BlazorTextEditor.RazorLib.Lexing;
using BlazorTextEditor.RazorLib.Row;
using BlazorTextEditor.RazorLib.Store.TextEditorCase;
using BlazorTextEditor.RazorLib.Store.TextEditorCase.Actions;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorTextEditor.RazorLib.TextEditor;

public partial class TextEditorBase
{
    public (int positionIndex, RowEndingKind rowEndingKind) GetStartOfRowTuple(int rowIndex)
    {
        return rowIndex > 0
            ? _rowEndingPositions[rowIndex - 1]
            : (0, RowEndingKind.StartOfFile);
    }

    /// <summary>
    ///     Returns the Length of a row however it does not include the line ending characters by default.
    ///     To include line ending characters the parameter <see cref="includeLineEndingCharacters" /> must be true.
    /// </summary>
    public int GetLengthOfRow(
        int rowIndex,
        bool includeLineEndingCharacters = false)
    {
        if (!_rowEndingPositions.Any())
            return 0;

        var startOfRowTupleInclusive = GetStartOfRowTuple(rowIndex);

        var endOfRowTupleExclusive = _rowEndingPositions[rowIndex];

        var lengthOfRowWithLineEndings = endOfRowTupleExclusive.positionIndex
                                         - startOfRowTupleInclusive.positionIndex;

        if (includeLineEndingCharacters)
            return lengthOfRowWithLineEndings;

        return lengthOfRowWithLineEndings - endOfRowTupleExclusive.rowEndingKind.AsCharacters().Length;
    }

    /// <param name="startingRowIndex">The starting index of the rows to return</param>
    /// <param name="count">count of 0 returns 0 rows. count of 1 returns the startingRowIndex.</param>
    public List<List<RichCharacter>> GetRows(int startingRowIndex, int count)
    {
        var rowCountAvailable = _rowEndingPositions.Count - startingRowIndex;

        var rowCountToReturn = count < rowCountAvailable
            ? count
            : rowCountAvailable;

        var endingRowIndexExclusive = startingRowIndex + rowCountToReturn;

        var rows = new List<List<RichCharacter>>();

        for (var i = startingRowIndex;
             i < endingRowIndexExclusive;
             i++)
        {
            // Previous row's line ending position is this row's start.
            var startOfRowInclusive = GetStartOfRowTuple(i)
                .positionIndex;

            var endOfRowExclusive = _rowEndingPositions[i].positionIndex;

            var row = _content
                .Skip(startOfRowInclusive)
                .Take(endOfRowExclusive - startOfRowInclusive)
                .ToList();

            rows.Add(row);
        }

        return rows;
    }

    public int GetTabsCountOnSameRowBeforeCursor(int rowIndex, int columnIndex)
    {
        var startOfRowPositionIndex = GetStartOfRowTuple(rowIndex)
            .positionIndex;

        var tabs = _tabKeyPositions
            .SkipWhile(positionIndex => positionIndex < startOfRowPositionIndex)
            .TakeWhile(positionIndex => positionIndex < startOfRowPositionIndex + columnIndex);

        return tabs.Count();
    }

    public TextEditorBase PerformEditTextEditorAction(KeyboardEventTextEditorBaseAction keyboardEventTextEditorBaseAction)
    {
        if (KeyboardKeyFacts.IsMetaKey(keyboardEventTextEditorBaseAction.KeyboardEventArgs))
        {
            if (KeyboardKeyFacts.MetaKeys.BACKSPACE == keyboardEventTextEditorBaseAction.KeyboardEventArgs.Key ||
                KeyboardKeyFacts.MetaKeys.DELETE == keyboardEventTextEditorBaseAction.KeyboardEventArgs.Key)
                PerformDeletions(keyboardEventTextEditorBaseAction);
        }
        else
            PerformInsertions(keyboardEventTextEditorBaseAction);

        return new TextEditorBase(this);
    }
    
    public TextEditorBase PerformEditTextEditorAction(InsertTextTextEditorBaseAction insertTextTextEditorBaseAction)
    {
        foreach (var character in insertTextTextEditorBaseAction.Content)
        {
            // TODO: This needs to be rewritten everything should be inserted at the same time not a foreach loop insertion for each character
            //
            // Need innerCursorSnapshots because need
            // after every loop of the foreach that the
            // cursor snapshots are updated
            var innerCursorSnapshots = TextEditorCursorSnapshot
                .TakeSnapshots(
                    insertTextTextEditorBaseAction.CursorSnapshots
                            .Select(x => x.UserCursor)
                            .ToArray())
                .ToImmutableArray();

            var code = character switch
            {
                '\r' => KeyboardKeyFacts.WhitespaceCodes.CARRIAGE_RETURN_CODE,
                '\n' => KeyboardKeyFacts.WhitespaceCodes.ENTER_CODE,
                '\t' => KeyboardKeyFacts.WhitespaceCodes.TAB_CODE,
                ' ' => KeyboardKeyFacts.WhitespaceCodes.SPACE_CODE,
                _ => character.ToString(),
            };

            var keyboardEventTextEditorBaseAction =
                    new KeyboardEventTextEditorBaseAction(
                        insertTextTextEditorBaseAction.TextEditorKey,
                        innerCursorSnapshots,
                        new KeyboardEventArgs
                        {
                            Code = code,
                            Key = character.ToString(),
                        },
                        CancellationToken.None);

            PerformEditTextEditorAction(keyboardEventTextEditorBaseAction);
        }
        
        return new TextEditorBase(this);
    }
    
    public TextEditorBase PerformEditTextEditorAction(DeleteTextByMotionTextEditorBaseAction deleteTextByMotionTextEditorBaseAction)
    {
        var keyboardEventArgs = deleteTextByMotionTextEditorBaseAction.MotionKind switch
        {
            MotionKind.Backspace => new KeyboardEventArgs
            {
                Key = KeyboardKeyFacts.MetaKeys.BACKSPACE
            },
            MotionKind.Delete => new KeyboardEventArgs
            {
                Key = KeyboardKeyFacts.MetaKeys.DELETE
            },
            _ => throw new ApplicationException(
                $"The {nameof(MotionKind)}:" +
                $" {deleteTextByMotionTextEditorBaseAction.MotionKind}" +
                " was not recognized.")
        };
        
        var keyboardEventTextEditorBaseAction =
            new KeyboardEventTextEditorBaseAction(
                deleteTextByMotionTextEditorBaseAction.TextEditorKey,
                deleteTextByMotionTextEditorBaseAction.CursorSnapshots,
                keyboardEventArgs,
                CancellationToken.None);

        PerformEditTextEditorAction(keyboardEventTextEditorBaseAction);

        return new TextEditorBase(this);
    }
    
    public TextEditorBase PerformEditTextEditorAction(DeleteTextByRangeTextEditorBaseAction deleteTextByRangeTextEditorBaseAction)
    {
        // TODO: This needs to be rewritten everything should be deleted at the same time not a foreach loop insertion for each character
        for (var i = 0; i < deleteTextByRangeTextEditorBaseAction.Count; i++)
        {
            // Need innerCursorSnapshots because need
            // after every loop of the foreach that the
            // cursor snapshots are updated
            var innerCursorSnapshots = TextEditorCursorSnapshot
                .TakeSnapshots(
                    deleteTextByRangeTextEditorBaseAction.CursorSnapshots
                        .Select(x => x.UserCursor)
                        .ToArray())
                .ToImmutableArray();
            
            var keyboardEventTextEditorBaseAction =
                new KeyboardEventTextEditorBaseAction(
                    deleteTextByRangeTextEditorBaseAction.TextEditorKey,
                    innerCursorSnapshots,
                    new KeyboardEventArgs
                    {
                        Code = KeyboardKeyFacts.MetaKeys.DELETE,
                        Key = KeyboardKeyFacts.MetaKeys.DELETE,
                    },
                    CancellationToken.None);

            PerformEditTextEditorAction(keyboardEventTextEditorBaseAction);
        }

        return new TextEditorBase(this);
    }

    /// <summary>
    ///     If applying syntax highlighting it may be preferred to use
    ///     <see cref="ApplySyntaxHighlightingAsync" />. It is effectively
    ///     just invoking the lexer and then <see cref="ApplyDecorationRange" />
    /// </summary>
    public void ApplyDecorationRange(IEnumerable<TextEditorTextSpan> textEditorTextSpans)
    {
        foreach (var textEditorTextSpan in textEditorTextSpans)
        {
            for (var i = textEditorTextSpan.StartingIndexInclusive; i < textEditorTextSpan.EndingIndexExclusive; i++)
                _content[i].DecorationByte = textEditorTextSpan.DecorationByte;
        }
    }

    public async Task ApplySyntaxHighlightingAsync(bool clearSyntaxHighlightingBeforeApplication = true)
    {
        // TODO: this did not work out it caused flashing colors to occur find other way to clear old syntax highlighting
        //
        // if (clearSyntaxHighlightingBeforeApplication)
        // {
        //     ApplyDecorationRange(new []
        //     {
        //         new TextEditorTextSpan(
        //             0,
        //             _content.Count,
        //             // 0 is decoration none
        //             0)
        //     });
        // }

        var textEditorTextSpans = await Lexer.Lex(GetAllText());

        ApplyDecorationRange(textEditorTextSpans);
    }

    public string GetAllText()
    {
        return new string(_content
            .Select(rc => rc.Value)
            .ToArray());
    }

    public int GetCursorPositionIndex(TextEditorCursor textEditorCursor)
    {
        return GetPositionIndex(
            textEditorCursor.IndexCoordinates.rowIndex,
            textEditorCursor.IndexCoordinates.columnIndex);
    }

    public int GetPositionIndex(int rowIndex, int columnIndex)
    {
        var startOfRowPositionIndex =
            GetStartOfRowTuple(rowIndex)
                .positionIndex;

        return startOfRowPositionIndex + columnIndex;
    }

    public string GetTextRange(int startingPositionIndex, int count)
    {
        return new string(_content
            .Skip(startingPositionIndex)
            .Take(count)
            .Select(rc => rc.Value)
            .ToArray());
    }

    public (int rowIndex, int rowStartPositionIndex, (int positionIndex, RowEndingKind rowEndingKind) rowEndingTuple)
        FindRowIndexRowStartRowEndingTupleFromPositionIndex(int positionIndex)
    {
        for (var i = _rowEndingPositions.Count - 1; i >= 0; i--)
        {
            var rowEndingTuple = _rowEndingPositions[i];

            if (positionIndex >= rowEndingTuple.positionIndex)
            {
                return (i + 1, rowEndingTuple.positionIndex,
                    i == _rowEndingPositions.Count - 1
                        ? rowEndingTuple
                        : _rowEndingPositions[i + 1]);
            }
        }

        return (0, 0, _rowEndingPositions[0]);
    }
    
    /// <summary>
    /// <see cref="moveBackwards"/> is to mean earlier in the document
    /// (lower column index or lower row index depending on position) 
    /// </summary>
    /// <returns>Will return -1 if no valid result was found.</returns>
    public int GetColumnIndexOfCharacterWithDifferingKind(
        int rowIndex,
        int columnIndex,
        bool moveBackwards)
    {
        var iterateBy = moveBackwards
            ? -1
            : 1;

        var startOfRowPositionIndex = GetStartOfRowTuple(
                rowIndex)
            .positionIndex;

        var lastPositionIndexOnRow = _rowEndingPositions[rowIndex].positionIndex - 1;

        var positionIndex = GetPositionIndex(rowIndex, columnIndex);

        if (moveBackwards)
        {
            if (positionIndex <= startOfRowPositionIndex)
                return -1;

            positionIndex -= 1;
        }

        if (positionIndex < 0 ||
            positionIndex >= _content.Count)
        {
            return -1;
        }

        var startingCharacterKind = _content[positionIndex].GetCharacterKind();

        while (true)
        {
            if (positionIndex >= _content.Count ||
                positionIndex > lastPositionIndexOnRow ||
                positionIndex < startOfRowPositionIndex)
                return -1;

            var currentCharacterKind = _content[positionIndex].GetCharacterKind();

            if (currentCharacterKind != startingCharacterKind)
                break;

            positionIndex += iterateBy;
        }

        if (moveBackwards) positionIndex += 1;

        return positionIndex - startOfRowPositionIndex;
    }

    public void SetDecorationMapper(IDecorationMapper? decorationMapper)
    {
        DecorationMapper = decorationMapper ?? new DecorationMapperDefault();

        // TODO: Invoke an event to reapply the CSS classes?
    }

    public void SetLexerMapper(ILexer? lexer)
    {
        Lexer = lexer ?? new LexerDefault();

        // TODO: Invoke an event to reapply the CSS classes?
    }

    public TextEditorBase SetUsingRowEndingKind(RowEndingKind rowEndingKind)
    {
        UsingRowEndingKind = rowEndingKind;
        return new TextEditorBase(this);
    }

    public ImmutableArray<RichCharacter> GetAllRichCharacters()
    {
        return _content.ToImmutableArray();
    }

    public void ClearEditBlocks()
    {
        EditBlockIndex = 0;
        _editBlocksPersisted.Clear();
    }
    
    /// <summary>
    /// The "if (EditBlockIndex == _editBlocksPersisted.Count)"
    /// <br/><br/>
    /// Is done because the active EditBlock is not yet persisted.
    /// <br/><br/>
    /// The active EditBlock is instead being 'created' as the user
    /// continues to make edits of the same <see cref="TextEditKind"/>
    /// <br/><br/>
    /// For complete clarity, this comment refers to one possibly expecting
    /// to see "if (EditBlockIndex == _editBlocksPersisted.Count - 1)"
    /// </summary>
    public void UndoEdit()
    {
        if (EditBlockIndex > 0)
        {
            if (EditBlockIndex == _editBlocksPersisted.Count)
            {
                // If the edit block is pending then persist it
                // before reverting back to the previous persisted edit block.
                
                EnsureUndoPoint(TextEditKind.ForcePersistEditBlock);
                EditBlockIndex--;
            }
            
            EditBlockIndex--;

            var restoreEditBlock = _editBlocksPersisted[EditBlockIndex];
            
            SetContent(restoreEditBlock.ContentSnapshot);
        }
    }
    
    public void RedoEdit()
    {
        if (EditBlockIndex < _editBlocksPersisted.Count - 1)
        {
            EditBlockIndex++;

            var restoreEditBlock = _editBlocksPersisted[EditBlockIndex];
            
            SetContent(restoreEditBlock.ContentSnapshot);
        }
    }
}