using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BlazorTUI.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BlazorTuiUsageAnalyzer : DiagnosticAnalyzer
{
    public const string DuplicateControlNameId = "BTUI001";
    public const string InvalidDimensionId = "BTUI002";
    public const string DuplicateItemNameId = "BTUI003";
    public const string MissingFocusTargetId = "BTUI004";

    private static readonly DiagnosticDescriptor DuplicateControlNameRule = new(
        DuplicateControlNameId,
        "Duplicate BlazorTUI control name",
        "BlazorTUI control or container name '{0}' is duplicated in this scope",
        "BlazorTUI.Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Control names must be unique across the screen container tree.");

    private static readonly DiagnosticDescriptor InvalidDimensionRule = new(
        InvalidDimensionId,
        "Invalid BlazorTUI dimension",
        "BlazorTUI dimension '{0}' uses invalid literal value {1}",
        "BlazorTUI.Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Control and container width, height, and length arguments must be positive.");

    private static readonly DiagnosticDescriptor DuplicateItemNameRule = new(
        DuplicateItemNameId,
        "Duplicate BlazorTUI item name",
        "BlazorTUI item, menu, or node name '{0}' is duplicated in this scope",
        "BlazorTUI.Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Menu items, context menu items, command items, tree nodes, and similar named child elements should be unique within their owner.");

    private static readonly DiagnosticDescriptor MissingFocusTargetRule = new(
        MissingFocusTargetId,
        "BlazorTUI focus target is not created in this scope",
        "BlazorTUI focus target '{0}' is not a control created in this scope",
        "BlazorTUI.Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "SetFocus should target an existing focusable control name.");

    private static readonly ImmutableHashSet<string> DimensionParameterNames =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "width", "height", "length");

    private static readonly ImmutableHashSet<string> DimensionMemberNames =
        ImmutableHashSet.Create(StringComparer.Ordinal, "Width", "Height", "width", "height");

    private static readonly ImmutableHashSet<string> NamedItemTypes =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "BlazorTUI.TUI.Menu",
            "BlazorTUI.TUI.MenuItem",
            "BlazorTUI.TUI.ContextMenuItem",
            "BlazorTUI.TUI.CommandPaletteItem",
            "BlazorTUI.TUI.BreadcrumbItem",
            "BlazorTUI.TUI.RadioGroupOption",
            "BlazorTUI.TUI.TreeNode",
            "BlazorTUI.TUI.BarChartItem",
            "BlazorTUI.TUI.TimelineItem",
            "BlazorTUI.TUI.KeyValueListItem",
            "BlazorTUI.TUI.StatusBarItem");

    private static readonly ImmutableHashSet<string> NamedItemFactoryMethods =
        ImmutableHashSet.Create(StringComparer.Ordinal, "AddItem", "AddMenu", "AddNode", "AddOption");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(
            DuplicateControlNameRule,
            InvalidDimensionRule,
            DuplicateItemNameRule,
            MissingFocusTargetRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeBody,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.LocalFunctionStatement,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.AnonymousMethodExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreationDimensions, SyntaxKind.ObjectCreationExpression, SyntaxKind.ImplicitObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeAssignmentDimensions, SyntaxKind.SimpleAssignmentExpression);
    }

    private static void AnalyzeBody(SyntaxNodeAnalysisContext context)
    {
        var controlNames = new List<NamedLiteral>();
        var focusableControlNames = new HashSet<string>(StringComparer.Ordinal);
        var itemNamesByOwner = new Dictionary<string, List<NamedLiteral>>(StringComparer.Ordinal);
        var focusTargets = new List<NamedLiteral>();

        foreach (BaseObjectCreationExpressionSyntax creation in context.Node.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>())
        {
            if (!TryGetFirstStringArgument(creation.ArgumentList?.Arguments, context.SemanticModel, context.CancellationToken, out NamedLiteral literal))
                continue;

            if (context.SemanticModel.GetSymbolInfo(creation, context.CancellationToken).Symbol is not IMethodSymbol constructor)
                continue;

            ITypeSymbol type = constructor.ContainingType;
            string typeName = GetFullName(type);
            if (IsBlazorTuiVisualElement(type))
            {
                controlNames.Add(literal);
                if (IsAssignableTo(type, "BlazorTUI.TUI.Control"))
                    focusableControlNames.Add(literal.Value);
            }
            else if (NamedItemTypes.Contains(typeName))
            {
                AddNamedLiteral(itemNamesByOwner, typeName, literal);
            }
        }

        foreach (InvocationExpressionSyntax invocation in context.Node.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
                continue;

            if (method.Name == "SetFocus" &&
                IsBlazorTuiFocusOwner(method.ContainingType) &&
                TryGetFirstStringArgument(invocation.ArgumentList.Arguments, context.SemanticModel, context.CancellationToken, out NamedLiteral focusTarget))
            {
                focusTargets.Add(focusTarget);
                continue;
            }

            if (NamedItemFactoryMethods.Contains(method.Name) &&
                IsBlazorTuiNamespace(method.ContainingType) &&
                TryGetFirstStringArgument(invocation.ArgumentList.Arguments, context.SemanticModel, context.CancellationToken, out NamedLiteral itemLiteral))
            {
                AddNamedLiteral(itemNamesByOwner, GetFullName(method.ContainingType) + "." + method.Name, itemLiteral);
            }
        }

        ReportDuplicates(context, controlNames, DuplicateControlNameRule);
        foreach (List<NamedLiteral> itemGroup in itemNamesByOwner.Values)
            ReportDuplicates(context, itemGroup, DuplicateItemNameRule);

        if (focusableControlNames.Count == 0)
            return;

        foreach (NamedLiteral focusTarget in focusTargets)
        {
            if (!focusableControlNames.Contains(focusTarget.Value))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingFocusTargetRule,
                    focusTarget.Location,
                    focusTarget.Value));
            }
        }
    }

    private static void AnalyzeObjectCreationDimensions(SyntaxNodeAnalysisContext context)
    {
        var creation = (BaseObjectCreationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(creation, context.CancellationToken).Symbol is not IMethodSymbol constructor ||
            !IsBlazorTuiNamespace(constructor.ContainingType) ||
            creation.ArgumentList is null)
        {
            return;
        }

        for (int index = 0; index < creation.ArgumentList.Arguments.Count; index++)
        {
            ArgumentSyntax argument = creation.ArgumentList.Arguments[index];
            string? parameterName = GetParameterName(constructor, argument, index);
            if (parameterName is null || !DimensionParameterNames.Contains(parameterName))
                continue;

            if (TryGetIntegerConstant(argument.Expression, context.SemanticModel, context.CancellationToken, out long value) && value <= 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidDimensionRule,
                    argument.GetLocation(),
                    parameterName,
                    value));
            }
        }
    }

    private static void AnalyzeAssignmentDimensions(SyntaxNodeAnalysisContext context)
    {
        var assignment = (AssignmentExpressionSyntax)context.Node;
        if (!TryGetDimensionMemberName(assignment.Left, out string memberName) ||
            !TryGetIntegerConstant(assignment.Right, context.SemanticModel, context.CancellationToken, out long value) ||
            value > 0)
        {
            return;
        }

        ISymbol? symbol = context.SemanticModel.GetSymbolInfo(assignment.Left, context.CancellationToken).Symbol;
        if (symbol is null || !IsBlazorTuiNamespace(symbol.ContainingType))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            InvalidDimensionRule,
            assignment.Right.GetLocation(),
            memberName,
            value));
    }

    private static void ReportDuplicates(SyntaxNodeAnalysisContext context, IEnumerable<NamedLiteral> literals, DiagnosticDescriptor rule)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (NamedLiteral literal in literals)
        {
            if (!seen.Add(literal.Value))
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, literal.Location, literal.Value));
            }
        }
    }

    private static void AddNamedLiteral(Dictionary<string, List<NamedLiteral>> map, string owner, NamedLiteral literal)
    {
        if (!map.TryGetValue(owner, out List<NamedLiteral>? literals))
        {
            literals = new List<NamedLiteral>();
            map[owner] = literals;
        }

        literals.Add(literal);
    }

    private static bool TryGetFirstStringArgument(
        SeparatedSyntaxList<ArgumentSyntax>? arguments,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out NamedLiteral literal)
    {
        literal = default;
        if (arguments is null || arguments.Value.Count == 0)
            return false;

        ExpressionSyntax expression = arguments.Value[0].Expression;
        Optional<object?> constant = semanticModel.GetConstantValue(expression, cancellationToken);
        if (!constant.HasValue || constant.Value is not string value || string.IsNullOrWhiteSpace(value))
            return false;

        literal = new NamedLiteral(value, expression.GetLocation());
        return true;
    }

    private static string? GetParameterName(IMethodSymbol method, ArgumentSyntax argument, int argumentIndex)
    {
        if (argument.NameColon is not null)
            return argument.NameColon.Name.Identifier.ValueText;

        return argumentIndex < method.Parameters.Length
            ? method.Parameters[argumentIndex].Name
            : null;
    }

    private static bool TryGetIntegerConstant(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out long value)
    {
        value = 0;
        Optional<object?> constant = semanticModel.GetConstantValue(expression, cancellationToken);
        if (!constant.HasValue || constant.Value is null)
            return false;

        switch (constant.Value)
        {
            case byte byteValue:
                value = byteValue;
                return true;
            case sbyte signedByteValue:
                value = signedByteValue;
                return true;
            case short shortValue:
                value = shortValue;
                return true;
            case ushort unsignedShortValue:
                value = unsignedShortValue;
                return true;
            case int intValue:
                value = intValue;
                return true;
            case uint unsignedIntValue:
                value = unsignedIntValue;
                return true;
            case long longValue:
                value = longValue;
                return true;
            case ulong unsignedLongValue when unsignedLongValue <= long.MaxValue:
                value = (long)unsignedLongValue;
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetDimensionMemberName(ExpressionSyntax expression, out string memberName)
    {
        memberName = "";
        if (expression is MemberAccessExpressionSyntax memberAccess)
            memberName = memberAccess.Name.Identifier.ValueText;
        else if (expression is IdentifierNameSyntax identifier)
            memberName = identifier.Identifier.ValueText;

        return DimensionMemberNames.Contains(memberName);
    }

    private static bool IsBlazorTuiVisualElement(ITypeSymbol type)
        => IsBlazorTuiNamespace(type) &&
            (IsAssignableTo(type, "BlazorTUI.TUI.Control") || IsAssignableTo(type, "BlazorTUI.TUI.Container"));

    private static bool IsBlazorTuiFocusOwner(ITypeSymbol? type)
        => IsAssignableTo(type, "BlazorTUI.TUI.Container") || GetFullNameOrEmpty(type) == "BlazorTUI.TUI.Screen";

    private static bool IsBlazorTuiNamespace(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        string fullName = GetFullName(type);
        return fullName.StartsWith("BlazorTUI.TUI.", StringComparison.Ordinal);
    }

    private static bool IsAssignableTo(ITypeSymbol? type, string metadataName)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (GetFullName(current) == metadataName)
                return true;
        }

        return false;
    }

    private static string GetFullName(ITypeSymbol type)
        => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "");

    private static string GetFullNameOrEmpty(ITypeSymbol? type)
        => type is null ? "" : GetFullName(type);

    private readonly struct NamedLiteral
    {
        public NamedLiteral(string value, Location location)
        {
            Value = value;
            Location = location;
        }

        public string Value { get; }

        public Location Location { get; }
    }
}
