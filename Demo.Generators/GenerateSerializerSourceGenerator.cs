using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Demo.Generators;

[Generator]
public class GenerateSerializerSourceGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
      "GenerateSerializerAttribute.g.cs",
      SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));

    IncrementalValuesProvider<SerializableType?> serializableType = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
        transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
      .Where(static m => m is not null);

    // TODO: If you're targeting the .NET 7 SDK, use this version instead:
    // IncrementalValuesProvider<SerializableType?> serializableType = context.SyntaxProvider
    //     .ForAttributeWithMetadataName(
    //         "NetEscapades.EnumGenerators.EnumExtensionsAttribute",
    //         predicate: static (s, _) => true,
    //         transform: static (ctx, _) => GetEnumToGenerate(ctx.SemanticModel, ctx.TargetNode))
    //     .Where(static m => m is not null);

    context.RegisterSourceOutput(serializableType, static (spc, source) => Execute(source, spc));
  }

  static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

  static SerializableType? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
  {
    var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
    var semanticModel = context.SemanticModel.Compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

    // TODO: if (context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol) return null;
    if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol) return null;
    if (!HasGenerateSerializerAttribute(classSymbol)) return null;

    var serializableType = BuildSerializableType(classSymbol);

    return serializableType;
  }

  private static bool HasGenerateSerializerAttribute(INamedTypeSymbol classSymbol)
  {
    foreach (var attr in classSymbol.GetAttributes())
    {
      var attrClass = attr.AttributeClass;
      if (attrClass == null) continue;

      var name = attrClass.Name;
      var fullName = attrClass.ToDisplayString();

      if (name == "GenerateSerializerAttribute" || fullName == "Demo.App.GenerateSerializerAttribute")
      {
        return true;
      }
    }

    return false;
  }

  private static SerializableType BuildSerializableType(/*GeneratorExecutionContext context,*/ INamedTypeSymbol classSymbol)
  {
    var props = new List<SerializableProperty>();

    foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
    {
      if (member.DeclaredAccessibility != Accessibility.Public) continue;

      if (member.GetMethod == null) continue;

      var type = member.Type;
      if (!IsSupportedType(type))
      {
        // TODO: ReportUnsupportedProperty(context, member, classSymbol);
        continue;
      }

      var canonicalName = GetCanonicalTypeName(type);

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

  private static void ReportUnsupportedProperty(GeneratorExecutionContext context, IPropertySymbol member, INamedTypeSymbol classSymbol)
  {
    var location = member.Locations.FirstOrDefault();

    var diagnostic = Diagnostic.Create(
      UnsupportedTypeRule,
      location,
      member.Name,
      classSymbol.Name,
      member.Type.ToDisplayString()
    );

    context.ReportDiagnostic(diagnostic);
  }

  private static readonly DiagnosticDescriptor UnsupportedTypeRule = new(
    "SG0001",
    "Unsupported property type",
    "Property '{0}' in type '{1}' has unsupported type for binary serialization",
    "GenerationSerializer",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true
  );

  private static string GetCanonicalTypeName(ITypeSymbol type) => type.SpecialType switch
  {
    SpecialType.System_Int32 => "int",
    SpecialType.System_Int64 => "long",
    SpecialType.System_Double => "double",
    SpecialType.System_Boolean => "bool",
    SpecialType.System_String => "string",
    _ => type.ToDisplayString(),
  };

  static void Execute(SerializableType? serializableType, SourceProductionContext context)
  {
    if (serializableType is { } value)
    {
      string source = SourceGenerationHelper.GenerateSerializerClass(value);

      context.AddSource($"{value.TypeName}.Serializer.g.cs", SourceText.From(source, Encoding.UTF8));
    }
  }
}
