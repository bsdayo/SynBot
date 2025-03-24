namespace SynBot.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute(string[] path, string? description = null) : Attribute
{
    public string[] Path { get; } = path;

    public string? Description { get; } = description;
}