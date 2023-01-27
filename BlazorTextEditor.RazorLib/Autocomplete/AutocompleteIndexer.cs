﻿using System.Collections.Concurrent;
using System.Collections.Immutable;
using BlazorTextEditor.RazorLib.Model;

namespace BlazorTextEditor.RazorLib.Autocomplete;

public class AutocompleteIndexer : IAutocompleteIndexer
{
    private readonly ITextEditorService _textEditorService;
    private readonly ConcurrentBag<string> _indexedStrings = new();
    
    public AutocompleteIndexer(ITextEditorService textEditorService)
    {
        _textEditorService = textEditorService;
        
        _textEditorService.ModelsCollectionChanged += TextEditorServiceTextEditorModelsCollectionChanged;
    }

    public ImmutableArray<string> IndexedStrings => _indexedStrings
        .ToImmutableArray();
    
    public Task IndexTextEditorAsync(TextEditorModel textEditorModel)
    {
        return Task.CompletedTask;
    }
    
    public Task IndexWordAsync(string word)
    {
        if (!_indexedStrings.Contains(word))
        {
            _indexedStrings.Add(word);
        }
        
        return Task.CompletedTask;
    }

    private void TextEditorServiceTextEditorModelsCollectionChanged()
    {
        // TODO: When should the indexer re-index or incrementally do so
    }
    
    public void Dispose()
    {
        _textEditorService.ModelsCollectionChanged -= TextEditorServiceTextEditorModelsCollectionChanged;
    }
}