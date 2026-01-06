using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace VariableRenamer
{
    public class VariableCollector : CSharpSyntaxRewriter
    {
        public List<RenamingRewriter.VariableInfo> Variables { get; } = new();

        public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            foreach (var variable in node.Declaration.Variables)
            {
                Variables.Add(new RenamingRewriter.VariableInfo
                {
                    OriginalName = variable.Identifier.Text,
                    Token = variable.Identifier,
                    Type = VariableType.Field,
                    Accessibility = GetAccessibility(node.Modifiers)
                });
            }
            
            return base.VisitFieldDeclaration(node);
        }

        public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            Variables.Add(new RenamingRewriter.VariableInfo
            {
                OriginalName = node.Identifier.Text,
                Token = node.Identifier,
                Type = VariableType.Property,
                Accessibility = GetAccessibility(node.Modifiers)
            });
            
            return base.VisitPropertyDeclaration(node);
        }

        public override SyntaxNode? VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            if (node.Parent is LocalDeclarationStatementSyntax)
            {
                foreach (var variable in node.Variables)
                {
                    Variables.Add(new RenamingRewriter.VariableInfo
                    {
                        OriginalName = variable.Identifier.Text,
                        Token = variable.Identifier,
                        Type = VariableType.Local,
                        Accessibility = Accessibility.Private
                    });
                }
            }
            
            return base.VisitVariableDeclaration(node);
        }

        public override SyntaxNode? VisitParameter(ParameterSyntax node)
        {
            Variables.Add(new RenamingRewriter.VariableInfo
            {
                OriginalName = node.Identifier.Text,
                Token = node.Identifier,
                Type = VariableType.Parameter,
                Accessibility = Accessibility.Private
            });
            
            return base.VisitParameter(node);
        }

        private Accessibility GetAccessibility(SyntaxTokenList modifiers)
        {
            if (modifiers.Any(m => m.Text == "public")) return Accessibility.Public;
            if (modifiers.Any(m => m.Text == "protected")) return Accessibility.Protected;
            if (modifiers.Any(m => m.Text == "internal")) return Accessibility.Internal;
            return Accessibility.Private;
        }
    }

    public enum Accessibility
    {
        Public,
        Protected,
        Internal,
        Private
    }
}