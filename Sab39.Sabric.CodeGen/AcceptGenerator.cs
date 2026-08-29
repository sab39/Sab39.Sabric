using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sab39.Sabric.CodeGen;

/// <summary>
/// Writes the <c>Accept</c> override into every concrete game object.
/// </summary>
/// <remarks>
/// The body is always <c>visitor.Visit(this)</c> - inside a class body <c>this</c> is statically
/// the concrete type - so the override carries no information beyond the type it sits in, which is
/// exactly what makes it worth generating. No annotation is involved: deriving from
/// <c>GameObjectBase</c> is the whole trigger.
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class AcceptGenerator : IIncrementalGenerator
{
    private const string GameObjectBaseName = "Sab39.Sabric.Engine.GameObjectBase";
    private const string VisitorName = "global::Sab39.Sabric.Engine.IGameObjectVisitor";

    private static readonly DiagnosticDescriptor mustBePartial = new(
        id: "SBR0001",
        title: "Game object must be partial",
        messageFormat: "Game object '{0}' must be declared partial so that its Accept override can be generated",
        category: "Sab39.Sabric.CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor acceptIsGenerated = new(
        id: "SBR0002",
        title: "Accept override is generated",
        messageFormat: "Game object '{0}' declares its own Accept override, so none is generated for it",
        category: "Sab39.Sabric.CodeGen",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The override is written for every concrete game object. Writing one by hand is "
            + "usually a leftover rather than a decision; suppress this where it genuinely is one.");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                static (syntaxContext, cancellationToken) => Describe(syntaxContext, cancellationToken))
            .Where(target => target is not null)
            .Select((target, _) => target!);

        context.RegisterSourceOutput(targets, Emit);
    }

    private static AcceptTarget? Describe(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var declaration = (TypeDeclarationSyntax)context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type) return null;
        if (type.IsAbstract || type.IsStatic) return null;
        if (!DerivesFromGameObject(type)) return null;

        // A partial type arrives here once per part. Only the first one answers for it, so a type
        // split across files neither generates twice nor reports the same diagnostic twice.
        if (type.DeclaringSyntaxReferences.Length > 1
            && type.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) != declaration)
        {
            return null;
        }

        if (DeclaredAccept(type) is { } existing)
        {
            return AcceptTarget.Reporting(Diagnostic.Create(
                acceptIsGenerated,
                existing.Locations.FirstOrDefault(),
                type.Name));
        }

        if (!IsPartialThroughout(type))
        {
            return AcceptTarget.Reporting(Diagnostic.Create(
                mustBePartial,
                declaration.Identifier.GetLocation(),
                type.Name));
        }

        return AcceptTarget.Emitting(HintNameFor(type), Render(type));
    }

    private static void Emit(SourceProductionContext context, AcceptTarget target)
    {
        if (target.Diagnostic is { } diagnostic)
        {
            context.ReportDiagnostic(diagnostic);
            return;
        }

        context.AddSource(target.HintName!, SourceText.From(target.Source!, Encoding.UTF8));
    }

    private static bool DerivesFromGameObject(INamedTypeSymbol type)
        => type.BaseType is { } baseType
            && (baseType.ToDisplayString() == GameObjectBaseName || DerivesFromGameObject(baseType));

    /// <summary>
    /// The type's own <c>Accept</c>, if it has one. Inherited ones don't count - every concrete game
    /// object needs its own, which is the point of the base declaring it abstract.
    /// </summary>
    private static IMethodSymbol? DeclaredAccept(INamedTypeSymbol type)
        => type.GetMembers("Accept")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(method => method.Arity == 1 && method.Parameters.Length == 1);

    private static bool IsPartialThroughout(INamedTypeSymbol type)
        => IsPartial(type) && (type.ContainingType is null || IsPartialThroughout(type.ContainingType));

    private static bool IsPartial(INamedTypeSymbol type)
        => type.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(syntax => syntax.Modifiers.Any(SyntaxKind.PartialKeyword));

    /// <summary>
    /// The containing types of <paramref name="type"/>, outermost first, so a nested game object can
    /// be reopened through its enclosing declarations.
    /// </summary>
    private static IEnumerable<INamedTypeSymbol> Containers(INamedTypeSymbol type)
        => type.ContainingType is null
            ? []
            : Containers(type.ContainingType).Append(type.ContainingType);

    private static string Render(INamedTypeSymbol type)
    {
        StringBuilder builder = new();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();

        var depth = 0;

        if (!type.ContainingNamespace.IsGlobalNamespace)
        {
            builder.AppendLine($"namespace {type.ContainingNamespace.ToDisplayString()}");
            builder.AppendLine("{");
            depth++;
        }

        foreach (var container in Containers(type))
        {
            builder.AppendLine($"{Indent(depth)}partial {Keyword(container)} {NameWithTypeParameters(container)}");
            builder.AppendLine($"{Indent(depth)}{{");
            depth++;
        }

        builder.AppendLine($"{Indent(depth)}partial {Keyword(type)} {NameWithTypeParameters(type)}");
        builder.AppendLine($"{Indent(depth)}{{");
        builder.AppendLine($"{Indent(depth + 1)}/// <inheritdoc />");
        builder.AppendLine($"{Indent(depth + 1)}public override TResult Accept<TResult>({VisitorName}<TResult> visitor) => visitor.Visit(this);");
        builder.AppendLine($"{Indent(depth)}}}");

        while (depth > 0)
        {
            depth--;
            builder.AppendLine($"{Indent(depth)}}}");
        }

        return builder.ToString();
    }

    private static string Indent(int depth) => new(' ', depth * 4);

    private static string Keyword(INamedTypeSymbol type) => type.IsRecord ? "record" : "class";

    /// <summary>
    /// Constraints are deliberately left off: a partial declaration may omit them entirely, and
    /// repeating them would only create a second place for them to disagree.
    /// </summary>
    private static string NameWithTypeParameters(INamedTypeSymbol type)
        => type.TypeParameters.IsEmpty
            ? type.Name
            : $"{type.Name}<{string.Join(", ", type.TypeParameters.Select(parameter => parameter.Name))}>";

    private static string HintNameFor(INamedTypeSymbol type)
    {
        var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty);
        string sanitized = new(name.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray());

        return $"{sanitized}.Accept.g.cs";
    }
}
