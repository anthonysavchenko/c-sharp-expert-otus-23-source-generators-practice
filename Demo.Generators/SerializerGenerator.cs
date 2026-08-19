using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Demo.Generators;

[Generator]
public class SerializerGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
      "GenerateSerializerAttribute.g.cs",
      SourceText.From(SourceCodeGenerator.GenerateAttributeDeclaration(), Encoding.UTF8)
    ));

    IncrementalValuesProvider<SerializableType?> serializableType = context.SyntaxProvider
        .ForAttributeWithMetadataName(
          SourceCodeGenerator.GetAttributeFullName(),
          predicate: static (s, _) => true,
          transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx.SemanticModel, ctx.TargetNode)
        )
        .Where(static m => m is not null);

    context.RegisterSourceOutput(serializableType, static (spc, source) => Execute(spc, source));
  }

  static SerializableType? GetSemanticTargetForGeneration(SemanticModel semanticModel, SyntaxNode classDeclarationSyntax)
  {
    if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol) return null;

    var serializableType = BuildSerializableType(classSymbol);

    return serializableType;
  }

  private static SerializableType BuildSerializableType(INamedTypeSymbol classSymbol)
  {
    var props = new List<SerializableProperty>();

    foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
    {
      if (member.DeclaredAccessibility != Accessibility.Public) continue;
      if (member.GetMethod == null) continue;
      if (!IsSupportedType(member.Type)) continue;

      var canonicalName = GetCanonicalTypeName(member.Type);

      props.Add(new SerializableProperty(member.Name, canonicalName));
    }

    var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
      ? string.Empty
      : classSymbol.ContainingNamespace.ToDisplayString();

    return new SerializableType(ns, classSymbol.Name, props);
  }

  private static bool IsSupportedType(ITypeSymbol type) => type.SpecialType switch
  {
    SpecialType.System_Int32 or
    SpecialType.System_Int64 or
    SpecialType.System_Double or
    SpecialType.System_Boolean or
    SpecialType.System_String
      => true,

    _ => false,
  };

  private static string GetCanonicalTypeName(ITypeSymbol type) => type.SpecialType switch
  {
    SpecialType.System_Int32 => "int",
    SpecialType.System_Int64 => "long",
    SpecialType.System_Double => "double",
    SpecialType.System_Boolean => "bool",
    SpecialType.System_String => "string",
    _ => type.ToDisplayString(),
  };

  static void Execute(SourceProductionContext context, SerializableType? serializableType)
  {
    if (serializableType is { } value)
    {
      string source = SourceCodeGenerator.GeneratePartialClass(context, value);

      context.AddSource($"{value.TypeName}.Serializer.g.cs", SourceText.From(source, Encoding.UTF8));
    }
  }
}
