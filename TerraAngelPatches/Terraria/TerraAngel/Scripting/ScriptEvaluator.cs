using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;

namespace TerraAngel.Scripting;

public class ScriptEvaluator : IDisposable
{
    private ScriptState<object>? _scriptState;
    private AdhocWorkspace? _workspace;
    private DocumentId? _documentId;
    private ProjectId? _projectId;
    private readonly object _workspaceLock = new object();
    private readonly ScriptOptions _scriptOptions;
    private readonly ImmutableArray<MetadataReference> _metadataReferences;
    private bool _disposed;

    public ScriptEvaluator(IEnumerable<Assembly> assemblyImports, IEnumerable<string> usings)
    {
        _scriptOptions = ScriptOptions.Default
            .WithReferences(assemblyImports)
            .WithImports(usings);

        _metadataReferences = _scriptOptions.MetadataReferences.ToImmutableArray();
        _scriptState = CSharpScript.RunAsync<object>("", _scriptOptions).Result;
    }

    public ScriptEvaluationResult Eval(string source)
    {
        try
        {
            Script<object> continued = _scriptState!.Script.ContinueWith<object>(source);
            ImmutableArray<Diagnostic> diagnostics = continued.Compile();

            if (diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
            {
                return new ScriptEvaluationResult(null, diagnostics, null);
            }

            _scriptState = continued.RunFromAsync(_scriptState, catchException: _ => true).Result;

            if (_scriptState.Exception is not null)
            {
                return new ScriptEvaluationResult(null, diagnostics, _scriptState.Exception);
            }

            return new ScriptEvaluationResult(_scriptState.ReturnValue, diagnostics, null);
        }
        catch (Exception ex)
        {
            return new ScriptEvaluationResult(null, ImmutableArray<Diagnostic>.Empty, ex);
        }
    }

    private void UpdateWorkspace(string source)
    {
        lock (_workspaceLock)
        {
            if (_workspace == null)
            {
                _workspace = new AdhocWorkspace();
                _projectId = ProjectId.CreateNewId();

                ProjectInfo projectInfo = ProjectInfo.Create(
                    _projectId,
                    VersionStamp.Create(),
                    "ScriptProject",
                    "ScriptProject",
                    LanguageNames.CSharp,
                    isSubmission: true,
                    parseOptions: new CSharpParseOptions(kind: SourceCodeKind.Script),
                    compilationOptions: new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        usings: _scriptOptions.Imports),
                    metadataReferences: _metadataReferences);

                _workspace.AddProject(projectInfo);
                _documentId = DocumentId.CreateNewId(_projectId);
            }

            SourceText sourceText = SourceText.From(source);

            if (_workspace.CurrentSolution.ContainsDocument(_documentId!))
            {
                _workspace.TryApplyChanges(_workspace.CurrentSolution.WithDocumentText(_documentId, sourceText));
            }
            else
            {
                DocumentInfo documentInfo = DocumentInfo.Create(
                    _documentId!,
                    "REPL",
                    sourceCodeKind: SourceCodeKind.Script,
                    loader: TextLoader.From(TextAndVersion.Create(sourceText, VersionStamp.Create())));

                _workspace.AddDocument(documentInfo);
            }
        }
    }

    public Document GetDocument()
    {
        lock (_workspaceLock)
        {
            if (_workspace == null || _documentId == null)
            {
                UpdateWorkspace("");
            }

            return _workspace!.CurrentSolution.GetDocument(_documentId!)!;
        }
    }

    public async Task<ImmutableArray<CompletionItem>> GetCompletionsAsync(
        string source,
        int caretPosition,
        CompletionTrigger completionTrigger,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(source))
        {
            return ImmutableArray<CompletionItem>.Empty;
        }

        UpdateWorkspace(source);

        Document document = GetDocument();
        CompletionService? completionService = CompletionService.GetService(document);

        if (completionService is null)
        {
            return ImmutableArray<CompletionItem>.Empty;
        }

        if (!ShouldTriggerCompletion(completionTrigger))
        {
            return ImmutableArray<CompletionItem>.Empty;
        }

        CompletionList? completions = await completionService.GetCompletionsAsync(
            document,
            caretPosition,
            trigger: default,
            cancellationToken: cancellationToken);

        if (completions == null || completions.Span.Length == 0)
        {
            return completions?.ItemsList.ToImmutableArray() ?? ImmutableArray<CompletionItem>.Empty;
        }

        string filterText = source.Substring(completions.Span.Start, completions.Span.Length);
        ImmutableArray<CompletionItem> filteredItems = completionService.FilterItems(
            document,
            completions.ItemsList.ToImmutableArray(),
            filterText);

        return filteredItems.Sort((x, y) => CompareCompletionItems(filterText, x, y));
    }

    private bool ShouldTriggerCompletion(CompletionTrigger completionTrigger)
    {
        if (completionTrigger.Kind == CompletionTriggerKind.Invoke)
        {
            return true;
        }

        if (completionTrigger.Kind == CompletionTriggerKind.Insertion || completionTrigger.Kind == CompletionTriggerKind.Deletion)
        {
            if (char.IsLetterOrDigit(completionTrigger.Character) || completionTrigger.Character == '.')
            {
                return true;
            }
        }

        return false;
    }

    private int CompareCompletionItems(string filterText, CompletionItem x, CompletionItem y)
    {
        if (x.SortText.Equals(filterText, StringComparison.Ordinal))
        {
            return -1;
        }

        if (y.SortText.Equals(filterText, StringComparison.Ordinal))
        {
            return 1;
        }

        bool xStartsWith = x.SortText.StartsWith(filterText, StringComparison.OrdinalIgnoreCase);
        bool yStartsWith = y.SortText.StartsWith(filterText, StringComparison.OrdinalIgnoreCase);

        if (xStartsWith && !yStartsWith) return -1;
        if (!xStartsWith && yStartsWith) return 1;

        return StringComparer.OrdinalIgnoreCase.Compare(x.SortText, y.SortText);
    }

    public async Task<(string, int)> ApplyCompletionAsync(
        string source,
        CompletionItem item,
        int caretPosition,
        char? commitCharacter)
    {
        UpdateWorkspace(source);

        Document document = GetDocument();
        CompletionService? completionService = CompletionService.GetService(document);

        if (completionService is null)
        {
            return (source, caretPosition);
        }

        CompletionChange change = await completionService.GetChangeAsync(document, item, commitCharacter);

        string insertedText = change.TextChange.NewText ?? "";
        string newSource = source.Remove(change.TextChange.Span.Start, change.TextChange.Span.Length)
            .Insert(change.TextChange.Span.Start, insertedText);

        int newCaretPosition;

        if (change.NewPosition.HasValue)
        {
            newCaretPosition = change.NewPosition.Value;
        }
        else
        {
            newCaretPosition = caretPosition + insertedText.Length - change.TextChange.Span.Length;
        }

        return (newSource, newCaretPosition);
    }

    public async Task<ImmutableArray<string>> GetSymbolArgumentsAsync(string source, int caretPosition)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return ImmutableArray<string>.Empty;
        }

        UpdateWorkspace(source);

        Document document = GetDocument();
        SyntaxNode? rootNode = await document.GetSyntaxRootAsync();

        if (rootNode is null)
        {
            return ImmutableArray<string>.Empty;
        }

        SemanticModel? semanticModel = await document.GetSemanticModelAsync();

        if (semanticModel is null)
        {
            return ImmutableArray<string>.Empty;
        }

        SyntaxNode? FindParentArgumentList(SyntaxNode? node)
        {
            SyntaxNode? workingNode = node;
            while (workingNode is not null)
            {
                if (workingNode is ArgumentListSyntax)
                {
                    return workingNode;
                }
                workingNode = workingNode.Parent;
            }

            return null;
        }

        SyntaxToken tokenAtCursor = rootNode.FindToken(Math.Max(caretPosition - 1, 0));
        SyntaxNode? argumentList = FindParentArgumentList(tokenAtCursor.Parent);

        if (argumentList is null || argumentList.Parent is null)
        {
            return ImmutableArray<string>.Empty;
        }

        SymbolInfo info = semanticModel.GetSymbolInfo(argumentList.Parent);
        List<string> candidates = new List<string>(info.CandidateSymbols.Length);

        if (info.Symbol is not null)
        {
            candidates.Add(info.Symbol.ToMinimalDisplayString(semanticModel, caretPosition));
            return candidates.ToImmutableArray();
        }

        for (int i = 0; i < info.CandidateSymbols.Length; i++)
        {
            ISymbol symbol = info.CandidateSymbols[i];
            candidates.Add(symbol.ToMinimalDisplayString(semanticModel, caretPosition));
        }

        return candidates.ToImmutableArray();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _workspace?.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}