using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Demo.Generators
{
  [Generator]
  public class GenerateSerializerSourceGenerator : ISourceGenerator
  {
    private static readonly DiagnosticDescriptor UnsupportedTypeRule =
      new DiagnosticDescriptor(
        "SG0001",
        "Unsupported property type",
        "Property '{0}' in type '{1}' has unsupported type for binary serialization",
        "GenerationSerializer",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
      );

    public void Execute(GeneratorExecutionContext context)
    {
      if (!(context.SyntaxReceiver is GenerateSerializerSyntaxReceiver receiver)) return;

      var compilation = context.Compilation;
      var serializableTypes = new List<SerializableType>();

      foreach (var classDecl in receiver.Candidates)
      {
        var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

        if (classSymbol == null) continue;

        if (!HasGenerateSerializerAttribute(classSymbol)) continue;

        var serializableType = BuildSerializableType(context, classSymbol);

        if (serializableType != null)
        {
          serializableTypes.Add(serializableType);
        }
      }

      foreach (var serializableType in serializableTypes)
      {
        var source = GenerateSerializerClass(serializableType);

        context.AddSource(serializableType.TypeName + ".Serializer.g.cs", SourceText.From(source, Encoding.UTF8));
      }
    }

    private string GenerateSerializerClass(SerializableType type)
    {
      var sb = new StringBuilder();

      sb.AppendLine("using System;");
      sb.AppendLine("using System.IO;");
      sb.AppendLine();

      if (!string.IsNullOrWhiteSpace(type.Namespace))
      {
        sb.Append("namespace ").Append(type.Namespace).AppendLine(";");
        sb.AppendLine();
      }

      sb.Append("public partial class ").Append(type.TypeName).AppendLine();
      sb.AppendLine("{");
      sb.AppendLine(" public void SerializeTo(Stream stream)");
      sb.AppendLine(" {");
      sb.AppendLine("   using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))");
      sb.AppendLine("   {");

      foreach (var prop in type.Properties)
      {
        AppendWriteForProperty(sb, prop);
      }

      sb.AppendLine("   }");
      sb.AppendLine(" }");
      sb.AppendLine("}");

      return sb.ToString();
    }

    private void AppendWriteForProperty(StringBuilder sb, SerializableProperty prop)
    {
      switch (prop.TypeName)
      {
        case "int":
        case "long":
        case "double":
        case "bool":
          sb.Append("     writer.Write(this.").Append(prop.Name).AppendLine(");");
          break;
        case "string":
          sb.AppendLine("     if (this." + prop.Name + " == null)");
          sb.AppendLine("     {");
          sb.AppendLine("       writer.Write(-1);");
          sb.AppendLine("     }");
          sb.AppendLine("     else");
          sb.AppendLine("     {");
          sb.AppendLine("       var bytes = System.Text.Encoding.UTF8.GetBytes(this." + prop.Name + ");");
          sb.AppendLine("       writer.Write(bytes.Length);");
          sb.AppendLine("     }");
          break;
        default:
          sb.Append("// Unsupported type:").Append(prop.TypeName).AppendLine();
          break;
      }
    }

    private SerializableType BuildSerializableType(GeneratorExecutionContext context, INamedTypeSymbol classSymbol)
    {
      var props = new List<SerializableProperty>();

      foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
      {
        if (member.DeclaredAccessibility != Accessibility.Public) continue;

        if (member.GetMethod == null) continue;

        var type = member.Type;
        if (!IsSupportedType(type))
        {
          ReportUnsupportedProperty(context, member, classSymbol);
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

    private string GetCanonicalTypeName(ITypeSymbol type)
    {
      switch (type.SpecialType)
      {
        case SpecialType.System_Int32:
          return "int";
        case SpecialType.System_Int64:
          return "long";
        case SpecialType.System_Double:
          return "double";
        case SpecialType.System_Boolean:
          return "bool";
        case SpecialType.System_String:
          return "string";
        default:
          return type.ToDisplayString();
      }
    }

    private void ReportUnsupportedProperty(GeneratorExecutionContext context, IPropertySymbol member, INamedTypeSymbol classSymbol)
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

    private bool IsSupportedType(ITypeSymbol type)
    {
      switch (type.SpecialType)
      {
        case SpecialType.System_Int32:
        case SpecialType.System_Int64:
        case SpecialType.System_Double:
        case SpecialType.System_Boolean:
        case SpecialType.System_String:
          return true;
        default:
          return false;
      }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
      context.RegisterForSyntaxNotifications(() => new GenerateSerializerSyntaxReceiver());
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
  }
}
