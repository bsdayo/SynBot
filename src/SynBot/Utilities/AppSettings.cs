using System.Text.Json;
using SynBot.Options;

namespace SynBot.Utilities;

public sealed class AppSettings : AppOptions
{
    public static void CreateIfNotExists()
    {
        const string filename = "appsettings.json";
        if (File.Exists(filename))
            return;
        File.WriteAllText(filename, JsonSerializer.Serialize(new AppSettings(),
            new JsonSerializerOptions { WriteIndented = true }));
    }

    public PowerShellOptions PowerShell { get; set; } = new();
}