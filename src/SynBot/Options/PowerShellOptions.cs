namespace SynBot.Options;

public sealed class PowerShellOptions
{
    public TimeSpan InvocationTimeout { get; set; } = TimeSpan.FromSeconds(5);
}