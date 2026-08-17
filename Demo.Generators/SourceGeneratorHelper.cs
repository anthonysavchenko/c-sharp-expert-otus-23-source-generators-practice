using System.Text;

namespace Demo.Generators;

public static class SourceGenerationHelper
{
  public const string Attribute = @"
namespace Demo.Generators
{
  [System.AttributeUsage(System.AttributeTargets.Class)]
  public class GenerateSerializerAttribute : System.Attribute
  {
  }
}";

  internal static string GenerateSerializerClass(SerializableType type)
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
    sb.AppendLine("  public void SerializeTo(Stream stream)");
    sb.AppendLine("  {");
    sb.AppendLine("    using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))");
    sb.AppendLine("    {");

    foreach (var prop in type.Properties)
    {
      AppendWriteForProperty(sb, prop);
    }

    sb.AppendLine("    }");
    sb.AppendLine("  }");
    sb.AppendLine("}");

    return sb.ToString();
  }

  private static void AppendWriteForProperty(StringBuilder sb, SerializableProperty prop)
  {
    switch (prop.TypeName)
    {
      case "int":
      case "long":
      case "double":
      case "bool":
        sb.Append("      writer.Write(this.").Append(prop.Name).AppendLine(");");
        break;
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
        break;
      default:
        sb.Append("// Unsupported type:").Append(prop.TypeName).AppendLine();
        break;
    }
  }
}
