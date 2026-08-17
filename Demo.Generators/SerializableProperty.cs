namespace Demo.Generators;

internal class SerializableProperty(string name, string typeName)
{
  public string Name { get; } = name;
  public string TypeName { get; } = typeName;
}
