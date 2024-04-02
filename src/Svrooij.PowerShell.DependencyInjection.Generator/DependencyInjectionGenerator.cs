using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Svrooij.PowerShell.DependencyInjection.Generator
{
    [Generator]
    public class DependencyInjectionGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new DependencyInjectionReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = context.SyntaxReceiver as DependencyInjectionReceiver;
            if (receiver == null)
                return;

            if (receiver.CandidateClasses.Count == 0)
                return;

            foreach (var classDeclaration in receiver.CandidateClasses)
            {
                var classSource = GenerateClassSource(classDeclaration);
                context.AddSource($"{classDeclaration.Identifier}.g.cs", SourceText.From(classSource, Encoding.UTF8));
            }
        }

        private string GenerateClassSource(ClassDeclarationSyntax classSymbol)
        {
            var namespaceName = (classSymbol.Parent as NamespaceDeclarationSyntax)!.Name.ToString();
            // Get base class type from class declaration
            var baseClassType = classSymbol.BaseList!.Types.First().Type;
            // Get all usings for this class
            var usingsForClass = classSymbol.SyntaxTree.GetCompilationUnitRoot().Usings;

            var builder = new StringBuilder($@"
using Microsoft.Extensions.DependencyInjection;
using System;
");
            foreach (var usingDirectiveSyntax in usingsForClass)
            {
                builder.AppendLine(usingDirectiveSyntax.ToString());
            }

            builder.AppendLine($@"
namespace {namespaceName}
{{
    public partial class {classSymbol.Identifier}
    {{
        protected override Action<{baseClassType}, IServiceProvider> BindDependencies => (obj, serviceProvider) =>
        {{
            if (obj is {classSymbol.Identifier} cmdlet)
            {{");

            foreach (var member in classSymbol.Members)
            {
                if (member.AttributeLists.Any(a =>
                        a.Attributes.Any(attr => attr.Name.ToString().Contains("ServiceDependency"))))
                {
                    if (member is PropertyDeclarationSyntax property)
                    {
                        builder.AppendLine($@"
                cmdlet.{property.Identifier} = ({property.Type})serviceProvider.GetService(typeof({property.Type}));");
                    }
                    else if (member is FieldDeclarationSyntax field)
                    {
                        // Get the name of the field
                        var fieldName = field.Declaration.Variables.First().Identifier;
                        // Get the type of the field
                        var fieldType = field.Declaration.Type;
                        builder.AppendLine($@"
                cmdlet.{fieldName} = ({fieldType})serviceProvider.GetService(typeof({fieldType}));");
                    }
                }
            }

            builder.Append(@"
            }
        };
    }
}");

            return builder.ToString();
        }
    }
}