using Microsoft.CodeAnalysis;

namespace Demo.Generators;

internal static class UnsupportedProperty
{
  internal static void Report(
    SourceProductionContext context,
    SerializableProperty prop,
    SerializableType type
  )
  {
    // var location = member.Locations.FirstOrDefault();

    var diagnostic = Diagnostic.Create(
      UnsupportedTypeRule,
      // location,
      null,
      prop.Name,
      type.TypeName,
      prop.TypeName
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
}
