using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Svrooij.PowerShell.DI.Generator;

[Generator]
public class PowerShellGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Create the code files in the project (so you don't have to include an additional DLL)
        context.RegisterPostInitializationOutput(static ctx => {
            ctx.AddSource("Attributes.g.cs", SourceGenerationHelper.Attributes);
            ctx.AddSource("Logging.g.cs", LoggingClasses.Classes);
            ctx.AddSource("ThreadAffinitiveSynchronizationContext.g.cs", ThreadSynchronisation.ThreadAffinitiveSynchronizationContext);
            ctx.AddSource("DependencyCmdLet.g.cs", SourceGenerationHelper.Classes);
        });

        // Register a generator for the `BindDependencies` method
        var classDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Svrooij.PowerShell.DI.GenerateBindingsAttribute",
                predicate: static (s, _) => IsPartialClassWithAttributes(s),
                transform: static (ctx, _) => GetBindingToGenerate(ctx))
            .Where(static n => n is not null);

        // Combine the selected classes with the compilation
        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        // Register the source output
        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Left, source.Right, spc));

    }

    private static bool IsPartialClassWithAttributes(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        // Fast check for 'partial'
        if (!classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
            return false;

        // Only check fields/properties if class is partial
        foreach (var member in classDecl.Members)
        {
            if (member is FieldDeclarationSyntax field && field.AttributeLists.Count > 0)
                return true;
            if (member is PropertyDeclarationSyntax prop && prop.AttributeLists.Count > 0)
                return true;
        }
        return false;
    }

    private static BindingToGenerate? GetBindingToGenerate(GeneratorAttributeSyntaxContext context)
    {
        if (!(context.TargetNode is ClassDeclarationSyntax classDeclarationSyntax))
        {
            return null;
        }

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (classSymbol is null)
        {
            return null;
        }
        var @namespace = (classDeclarationSyntax.Parent as NamespaceDeclarationSyntax)?.Name.ToString() ?? (classDeclarationSyntax.Parent as FileScopedNamespaceDeclarationSyntax)?.Name.ToString();
        // Get all the fields that have the ServiceDependency attribute, and get the name of the field and the type, and the Named property `Required` from the attribute
        // This is almost fine, but we need to get the property of the attribute instead of a static `false` value
        var fields = classDeclarationSyntax.Members.OfType<FieldDeclarationSyntax>()
            .Where(f => f.AttributeLists.Any(a =>
                    a.Attributes.Any(at => at.Name.ToString().StartsWith("ServiceDependency")))
                && f.Modifiers.Any(SyntaxKind.PrivateKeyword) || f.Modifiers.Any(SyntaxKind.InternalKeyword))
            .Select(f => new
            {
                Name = f.Declaration.Variables[0].Identifier.ToString(),
                f.Declaration.Type,
                Declaration = f.Declaration,
                Required = f.AttributeLists.SelectMany(a => a.Attributes)
                    .FirstOrDefault(x => x.Name.ToString().StartsWith("ServiceDependency"))
                    ?.ArgumentList?.Arguments.FirstOrDefault(a1 => a1.ToString().StartsWith("Required"))
                    ?.ToString() == "Required = true"
                //.FirstOrDefault(a => (a.Name as IdentifierNameSyntax)?.Identifier.Text == "ServiceDependencyAttribute")
            })//.ToList();
            .Select(f => new BindingPropertyToGenerate(
                f.Declaration.Variables[0].Identifier.ToString(),
                f.Declaration.Type.ToString(), // needs namespace
                f.Required
                )).ToImmutableArray();

            return new BindingToGenerate(
                classDeclarationSyntax.BaseList!.Types[0].ToString(),
                classDeclarationSyntax.Identifier.ToString(),
                @namespace ?? "oops",
                fields);
            //fields.ToImmutableArray());
    }

    private static void Execute(Compilation compilation, ImmutableArray<BindingToGenerate?> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var bindingToGenerate in classes)
        {
            if (bindingToGenerate is not null)
            {
                GenerateSource(context, bindingToGenerate.Value);
            }
        }
    }

    private static void GenerateSource(SourceProductionContext context, BindingToGenerate bindingToGenerate)
    {
        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sourceBuilder.AppendLine("using Microsoft.Extensions.Logging;");
        sourceBuilder.AppendLine("using Svrooij.PowerShell.DI;");
        sourceBuilder.AppendLine("using System;");

        sourceBuilder.AppendLine($"namespace {bindingToGenerate.Namespace}");
        sourceBuilder.AppendLine("{");
        sourceBuilder.AppendLine($"    public partial class {bindingToGenerate.ClassName}");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        #nullable enable");
        sourceBuilder.AppendLine($"        /// <summary>Source generated dependency binding for: <see cref=\"{bindingToGenerate.ClassName}\"/></summary>");
        sourceBuilder.AppendLine(
            $"        protected override Action<object, IServiceProvider> BindDependencies =>");
        sourceBuilder.AppendLine("            (_, serviceProvider) =>");
        sourceBuilder.AppendLine("            {");
        foreach (BindingPropertyToGenerate property in bindingToGenerate.Properties)
        {
            if (property.Required)
            {
                sourceBuilder.AppendLine(
                    $"                {property.PropertyName} = serviceProvider.GetRequiredService<{property.PropertyType}>();");
            }
            else
            {
                sourceBuilder.AppendLine(
                    $"                {property.PropertyName} = serviceProvider.GetService<{property.PropertyType}>();");
            }
        }
        
        sourceBuilder.AppendLine("            };");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine("}");

        context.AddSource($"{bindingToGenerate.ClassName}_BindDependencies.g.cs",
            SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }
}
