using Microsoft.CodeAnalysis;

namespace Sab39.Sabric.CodeGen;

/// <summary>
/// What the generator worked out about one candidate type: either source to emit, or the reason
/// there isn't any.
/// </summary>
/// <remarks>
/// A record rather than a tuple because this crosses the incremental pipeline, where the model's
/// equality is what decides whether downstream work is re-run.
/// </remarks>
internal sealed record AcceptTarget(string? HintName, string? Source, Diagnostic? Diagnostic)
{
    public static AcceptTarget Emitting(string hintName, string source) => new(hintName, source, null);

    public static AcceptTarget Reporting(Diagnostic diagnostic) => new(null, null, diagnostic);
}
