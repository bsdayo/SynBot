namespace SynBot.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ArgumentAttribute(string name, string? description = null) : Attribute
{
    public string Name { get; } = name;

    public string? Description { get; } = description;
}