using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Svrooij.PowerShell.DependencyInjection.Generator
{
    public class DependencyInjectionReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Check if the syntax node is a class declaration
            if (syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                if (classDeclaration.Members
                    .Any(p => p.AttributeLists
                        .Any(al => al.Attributes.Any(a => a.Name.ToString().Contains("ServiceDependency")))))
                {
                    CandidateClasses.Add(classDeclaration);
                }
            }
        }
    }
}