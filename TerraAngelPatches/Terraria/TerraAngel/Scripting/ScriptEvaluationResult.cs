using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TerraAngel.Scripting;

public record ScriptEvaluationResult(object? Result, ImmutableArray<Diagnostic> Diagnostics, Exception? Exception);
