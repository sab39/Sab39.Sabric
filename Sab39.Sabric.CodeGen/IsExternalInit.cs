namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill. Records and <c>init</c> accessors need this type to exist and netstandard2.0 - which
/// a Roslyn component has to target - doesn't ship it.
/// </summary>
internal static class IsExternalInit;
