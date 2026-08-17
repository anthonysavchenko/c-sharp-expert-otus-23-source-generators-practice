namespace Demo.Generators;

internal class SerializableType(string @namespace, string typeName, IReadOnlyList<SerializableProperty> properties)
{
  public string Namespace { get; } = @namespace;
  public string TypeName { get; } = typeName;
  public IReadOnlyList<SerializableProperty> Properties { get; } = properties;
}
