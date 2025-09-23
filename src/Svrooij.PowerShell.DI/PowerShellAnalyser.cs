namespace Svrooij.PowerShell.DI.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PowerShellAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rules.PsGenerateBindingsNotPartialDescriptor];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }
    
    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;

        if (HasGenerateBindingsAttribute(typeDeclaration))
        {
            if (!IsPartial(typeDeclaration))
            {
                ReportGenerateBindingsNotPartialRule(context, typeDeclaration);
            }
        }
    }

    private static void ReportGenerateBindingsNotPartialRule(SyntaxNodeAnalysisContext context, TypeDeclarationSyntax typeDeclaration)
    {
        var diagnostic = Diagnostic.Create(
            Rules.PsGenerateBindingsNotPartialDescriptor,
            typeDeclaration.Identifier.GetLocation(),
            typeDeclaration.Identifier.ValueText);

        context.ReportDiagnostic(diagnostic);
    }
    
    private static bool HasGenerateBindingsAttribute(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.AttributeLists
            .SelectMany(attributeList => attributeList.Attributes)
            .Any(attribute =>
            {
                var attributeName = attribute.Name.ToString();
                return attributeName == "GenerateBindings" || attributeName == "GenerateBindingsAttribute";
            });
    }

    private static bool IsPartial(TypeDeclarationSyntax typeDeclaration)
        => typeDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
}

internal class Rules
{
    private const string PsGenerateBindingsNotPartialDiagnosticId = "PS28001";
    internal static readonly DiagnosticDescriptor PsGenerateBindingsNotPartialDescriptor = new (
        PsGenerateBindingsNotPartialDiagnosticId,
        "Cmdlet with GenerateBindings attribute should be partial",
        "Cmdlet '{0}' has the GenerateBindings attribute but is not marked as partial",
        "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Cmdlets with the GenerateBindings attribute should be marked as partial.");
}