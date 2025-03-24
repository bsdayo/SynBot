namespace SynBot.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class OptionAttribute(string[] aliases, string? description = null) : Attribute
{
    public string[] Aliases { get; } = aliases;

    public string? Description { get; } = description;
}