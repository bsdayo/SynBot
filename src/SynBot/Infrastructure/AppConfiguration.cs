using System.Text.Json;
using Lagrange.Core.Common;
using Microsoft.Extensions.Configuration;

namespace SynBot.Infrastructure;

public static class AppConfiguration
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void CreateIfNotExists()
    {
        if (!File.Exists(AppConstants.DeviceFile))
            File.WriteAllText(AppConstants.DeviceFile, JsonSerializer.Serialize(new
            {
                Device = BotDeviceInfo.GenerateInfo()
            }, JsonOptions));

        if (!File.Exists(AppConstants.KeystoreFile))
            SaveKeystore(new BotKeystore());
    }

    public static void AddAppConfigurationSources(this IConfigurationBuilder configuration, string[] cmdArgs)
    {
        configuration.AddJsonFile(AppConstants.DeviceFile);
        configuration.AddJsonFile(AppConstants.KeystoreFile);
        configuration.AddJsonFile(AppConstants.AppSettingsFile);
        configuration.AddEnvironmentVariables("SYNBOT_");
        if (cmdArgs is { Length: > 0 })
            configuration.AddCommandLine(cmdArgs);
    }

    public static void SaveKeystore(BotKeystore keystore)
    {
        File.WriteAllText(AppConstants.KeystoreFile,
            JsonSerializer.Serialize(new { Keystore = keystore }, JsonOptions));
    }
}