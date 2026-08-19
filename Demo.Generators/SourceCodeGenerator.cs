using System.Text;
using Microsoft.CodeAnalysis;

namespace Demo.Generators;

public static class SourceCodeGenerator
{
  private const string ATTRIBUTE_NAMESPACE = "Demo.Generators";
  private const string ATTRIBUTE_NAME = "GenerateSerializerAttribute";

  internal static string GetAttributeFullName() => ATTRIBUTE_NAMESPACE + "." + ATTRIBUTE_NAME;

  internal static string GenerateAttributeDeclaration()
  {
    var sb = new StringBuilder();

    sb.AppendLine("namespace " + ATTRIBUTE_NAMESPACE + ";");
    sb.AppendLine();
    sb.AppendLine("[System.AttributeUsage(System.AttributeTargets.Class)]");
    sb.AppendLine("public class " + ATTRIBUTE_NAME + " : System.Attribute { }");

    var sourceCode = sb.ToString();

    return sourceCode;
  }

  internal static string GeneratePartialClass(SourceProductionContext context, SerializableType type)
  {
    var sb = new StringBuilder();

    sb.AppendLine("using System;");
    sb.AppendLine("using System.IO;");
    sb.AppendLine();

    if (!string.IsNullOrWhiteSpace(type.Namespace))
    {
      sb.AppendLine("namespace " + type.Namespace + ";");
      sb.AppendLine();
    }

    sb.AppendLine("public partial class " + type.TypeName);
    sb.AppendLine("{");
    sb.AppendLine("  public void SerializeTo(Stream stream)");
    sb.AppendLine("  {");
    sb.AppendLine("    using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))");
    sb.AppendLine("    {");

    foreach (var prop in type.Properties)
    {
      if (!TryAppendWriteForProperty(sb, prop)) UnsupportedProperty.Report(context, prop, type);
    }

    sb.AppendLine("    }");
    sb.AppendLine("  }");
    sb.AppendLine("}");

    var sourceCode = sb.ToString();

    return sourceCode;
  }

  private static bool TryAppendWriteForProperty(StringBuilder sb, SerializableProperty prop)
  {
    switch (prop.TypeName)
    {
      case "int":
      case "long":
      case "double":
      case "bool":
        sb.AppendLine("      writer.Write(this." + prop.Name + ");");
        return true;
      case "string":
        sb.AppendLine("      if (this." + prop.Name + " == null)");
        sb.AppendLine("      {");
        sb.AppendLine("        writer.Write(-1);");
        sb.AppendLine("      }");
        sb.AppendLine("      else");
        sb.AppendLine("      {");
        sb.AppendLine("        var bytes = System.Text.Encoding.UTF8.GetBytes(this." + prop.Name + ");");
        sb.AppendLine("        writer.Write(bytes.Length);");
        sb.AppendLine("      }");
        return true;
      default:
        sb.AppendLine("      // Unsupported type: " + prop.TypeName);
        return false;
    }
  }
}
