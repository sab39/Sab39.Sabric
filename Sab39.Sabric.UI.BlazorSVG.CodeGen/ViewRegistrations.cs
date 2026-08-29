using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace Sab39.Sabric.UI.BlazorSVG.CodeGen;

/// <summary>
/// Everything one sweep of a compilation found: the registrations to write, and anything worth
/// saying about what it didn't write.
/// </summary>
/// <remarks>
/// The arrays compare by reference rather than by content, so this model is a poor cache key. That
/// costs nothing here because the sweep hangs off the compilation itself, which changes on every
/// keystroke regardless - see <see cref="ViewRegistrationGenerator"/> for why it has to.
/// </remarks>
internal sealed record ViewRegistrations(
    ImmutableArray<ViewRegistration> Registrations,
    ImmutableArray<Diagnostic> Diagnostics);
