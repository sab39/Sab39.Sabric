using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Sab39.Sabric.UI.BlazorSVG.CodeGen;

/// <summary>
/// Writes an <c>AddGeneratedViews()</c> extension holding one closed <c>AddGameObjectView</c> call
/// per view.
/// </summary>
/// <remarks>
/// No annotation is involved: a view already says what it renders through its
/// <c>GameObjectViewBase&lt;T&gt;</c> type argument, so the pairing comes for free from inheriting
/// the right base class.
///
/// The sweep hangs off the whole compilation rather than off syntax, because a view need not be in
/// source. Razor components in particular exist only as another source generator's output, which
/// this generator cannot see - so a view written as a <c>.razor</c> file is visible here only from
/// a project that references the assembly it ended up in.
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class ViewRegistrationGenerator : IIncrementalGenerator
{
    private const string ViewBaseName = "Sab39.Sabric.UI.BlazorSVG.GameObjectViewBase`1";
    private const string SabricViewAssembly = "Sab39.Sabric.UI.BlazorSVG";
    private const string ServiceCollectionName = "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";

    private static readonly DiagnosticDescriptor ambiguousView = new(
        id: "SBR0101",
        title: "More than one view for a game object",
        messageFormat: "Game object '{0}' has more than one view ({1}); only '{2}' is registered",
        category: "Sab39.Sabric.UI.BlazorSVG.CodeGen",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var rootNamespace = context.AnalyzerConfigOptionsProvider.Select(static (options, _)
            => options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var value) ? value : null);

        var found = context.CompilationProvider.Select(static (compilation, cancellationToken)
            => Collect(compilation, cancellationToken));

        context.RegisterSourceOutput(found.Combine(rootNamespace), Emit);
    }

    private static ViewRegistrations Collect(Compilation compilation, CancellationToken cancellationToken)
    {
        if (compilation.GetTypeByMetadataName(ViewBaseName) is not { } viewBase) return new([], []);

        var views = Assemblies(compilation)
            .SelectMany(assembly => Types(assembly.GlobalNamespace, cancellationToken))
            .Where(type => IsRegisterable(compilation, type))
            .Select(type => (Object: RenderedObject(type, viewBase), View: type))
            .Where(pair => pair.Object is not null && IsRegisterable(compilation, pair.Object!));

        var registrations = ImmutableArray.CreateBuilder<ViewRegistration>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        var byObject = views
            .GroupBy(pair => Name(pair.Object!))
            .OrderBy(group => group.Key, StringComparer.Ordinal);

        foreach (var group in byObject)
        {
            // Ordered so that which view wins is a property of the code rather than of the order
            // the assemblies happened to be walked in.
            var candidates = group.Select(pair => pair.View).OrderBy(Name, StringComparer.Ordinal).ToList();
            var chosen = candidates[0];

            if (candidates.Count > 1)
            {
                diagnostics.Add(Diagnostic.Create(
                    ambiguousView,
                    chosen.Locations.FirstOrDefault(),
                    group.Key,
                    string.Join(", ", candidates.Select(Name)),
                    Name(chosen)));
            }

            registrations.Add(new(group.Key, Name(chosen)));
        }

        return new(registrations.ToImmutable(), diagnostics.ToImmutable());
    }

    private static void Emit(SourceProductionContext context, (ViewRegistrations Found, string? RootNamespace) input)
    {
        foreach (var diagnostic in input.Found.Diagnostics) context.ReportDiagnostic(diagnostic);

        context.AddSource(
            "GeneratedViewRegistrations.g.cs",
            SourceText.From(Render(input.Found.Registrations, input.RootNamespace), Encoding.UTF8));
    }

    /// <summary>
    /// This compilation plus every referenced assembly that could hold a view - which means the ones
    /// that reference the layer the view base lives in, and that layer itself.
    /// </summary>
    private static IEnumerable<IAssemblySymbol> Assemblies(Compilation compilation)
        => compilation.SourceModule.ReferencedAssemblySymbols
            .Where(assembly => assembly.Name == SabricViewAssembly || ReferencesViewAssembly(assembly))
            .Prepend(compilation.Assembly);

    private static bool ReferencesViewAssembly(IAssemblySymbol assembly)
        => assembly.Modules.Any(module
            => module.ReferencedAssemblies.Any(identity => identity.Name == SabricViewAssembly));

    private static IEnumerable<INamedTypeSymbol> Types(INamespaceOrTypeSymbol container, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var member in container.GetMembers().OfType<INamespaceOrTypeSymbol>())
        {
            if (member is INamedTypeSymbol type) yield return type;

            foreach (var nested in Types(member, cancellationToken)) yield return nested;
        }
    }

    /// <summary>
    /// Whether a type can appear as a type argument at the registration site: closed, constructible,
    /// and reachable from the assembly the registrations are being written into.
    /// </summary>
    private static bool IsRegisterable(Compilation compilation, INamedTypeSymbol type)
        => type is { TypeKind: TypeKind.Class, IsAbstract: false, IsStatic: false, IsGenericType: false }
            && compilation.IsSymbolAccessibleWithin(type, compilation.Assembly);

    private static INamedTypeSymbol? RenderedObject(INamedTypeSymbol type, INamedTypeSymbol viewBase)
    {
        if (type.BaseType is not { } baseType) return null;
        if (!SymbolEqualityComparer.Default.Equals(baseType.OriginalDefinition, viewBase)) return RenderedObject(baseType, viewBase);

        return baseType.TypeArguments[0] as INamedTypeSymbol;
    }

    private static string Name(ISymbol symbol) => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static string Render(ImmutableArray<ViewRegistration> registrations, string? rootNamespace)
    {
        StringBuilder builder = new();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();

        // AddGameObjectView is an extension member, so it has to be in scope rather than qualified.
        builder.AppendLine($"using {SabricViewAssembly};");
        builder.AppendLine();

        var depth = 0;

        if (!string.IsNullOrEmpty(rootNamespace))
        {
            builder.AppendLine($"namespace {rootNamespace}");
            builder.AppendLine("{");
            depth++;
        }

        builder.AppendLine($"{Indent(depth)}/// <summary>");
        builder.AppendLine($"{Indent(depth)}/// Registers the view for every game object this compilation can see.");
        builder.AppendLine($"{Indent(depth)}/// </summary>");
        builder.AppendLine($"{Indent(depth)}public static class GeneratedViewRegistrations");
        builder.AppendLine($"{Indent(depth)}{{");
        builder.AppendLine($"{Indent(depth + 1)}public static {ServiceCollectionName} AddGeneratedViews(this {ServiceCollectionName} services)");

        if (registrations.IsEmpty)
        {
            builder.AppendLine($"{Indent(depth + 2)}=> services;");
        }
        else
        {
            var calls = registrations.Select(registration
                => $"{Indent(depth + 3)}.AddGameObjectView<{registration.ObjectType}, {registration.ViewType}>()");

            builder.AppendLine($"{Indent(depth + 2)}=> services");
            builder.AppendLine($"{string.Join(Environment.NewLine, calls)};");
        }

        builder.AppendLine($"{Indent(depth)}}}");

        while (depth > 0)
        {
            depth--;
            builder.AppendLine($"{Indent(depth)}}}");
        }

        return builder.ToString();
    }

    private static string Indent(int depth) => new(' ', depth * 4);
}
