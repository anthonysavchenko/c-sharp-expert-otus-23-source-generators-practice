using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Demo.Generators
{
  internal class GenerateSerializerSyntaxReceiver : ISyntaxReceiver
  {
    public List<ClassDeclarationSyntax> Candidates { get; } = new List<ClassDeclarationSyntax>();
    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
      if (syntaxNode is ClassDeclarationSyntax cls && cls.AttributeLists.Count > 0)
      {
        Candidates.Add(cls);
      }
    }
  }
}