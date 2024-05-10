using System.Text.Json;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SynBot.Options;
using SynBot.Services;
using LagrangeLogLevel = Lagrange.Core.Event.EventArg.LogLevel;

namespace SynBot;

public sealed class Bot
{
    private const string KeystoreFilename = "keystore.json";
    private const string DeviceFilename = "device.json";
    private readonly AppOptions _appOptions;

    private readonly IHost _host;
    private readonly ILogger<Bot> _logger;

    public Bot(IHost host)
    {
        _host = host;
        _logger = _host.Services.GetRequiredService<ILogger<Bot>>();
        _appOptions = _host.Services.GetRequiredService<IOptions<AppOptions>>().Value;
    }

    public async Task RunAsync()
    {
        var keystore = await LoadKeystoreAsync();
        var device = await LoadDeviceAsync();
        var config = new BotConfig { CustomSignProvider = _host.Services.GetRequiredService<SignService>() };
        var bot = BotFactory.Create(config, device, keystore ?? new BotKeystore());

        bot.Invoker.OnBotLogEvent += (_, @event) =>
            _logger.Log(TransformLogLevel(@event.Level), "({Tag}) {Message}", @event.Tag, @event.EventMessage);

        bot.Invoker.OnBotOnlineEvent += async (_, _) =>
        {
            await SaveKeystoreAsync(bot.UpdateKeystore());
            _logger.LogInformation("Keystore updated.");
        };

        bot.Invoker.OnGroupMessageReceived += async (context, @event) =>
        {
            if (!_appOptions.AllowedGroups.Contains(@event.Chain.GroupUin!.Value))
                return;
            if (@event.Chain.FriendUin == bot.BotUin)
                return;
            if (!@event.Chain.HasTypeOf<TextEntity>())
                return;

            var script = string.Join('\n', @event.Chain.OfType<TextEntity>().Select(entity => entity.Text));
            if (int.TryParse(script, out _))
                return;

            var psService = _host.Services.GetRequiredService<PowerShellService>();
            try
            {
                var result = psService.RunScript(script);
                var message = MessageBuilder
                    .Group(@event.Chain.GroupUin!.Value)
                    .Text(string.Join('\n', result.Take(10)) +
                          (result.Count > 10 ? $"\n... ({result.Count - 10} more)" : ""))
                    .Build();
                await bot.SendMessage(message);
            }
            catch
            {
                // ignore
            }
        };

        // Login
        if (keystore is null)
        {
            var qrcode = await bot.FetchQrCode();
            if (qrcode is not { } data) return;
            await File.WriteAllBytesAsync("qrcode.png", data.QrCode);
            _logger.LogInformation("QRCode wrote.");
            await bot.LoginByQrCode();
        }
        else
        {
            await bot.LoginByPassword();
        }
    }

    private static LogLevel TransformLogLevel(LagrangeLogLevel level)
    {
        return level switch
        {
            LagrangeLogLevel.Debug => LogLevel.Debug,
            LagrangeLogLevel.Verbose => LogLevel.Trace,
            LagrangeLogLevel.Information => LogLevel.Information,
            LagrangeLogLevel.Warning => LogLevel.Warning,
            LagrangeLogLevel.Exception => LogLevel.Error,
            LagrangeLogLevel.Fatal => LogLevel.Critical,
            _ => LogLevel.None
        };
    }

    private static async Task SaveKeystoreAsync(BotKeystore keystore)
    {
        await File.WriteAllTextAsync(KeystoreFilename, JsonSerializer.Serialize(keystore));
    }

    private static async Task<BotKeystore?> LoadKeystoreAsync()
    {
        return File.Exists(KeystoreFilename)
            ? JsonSerializer.Deserialize<BotKeystore>(await File.ReadAllTextAsync(KeystoreFilename))!
            : null;
    }

    private static async Task<BotDeviceInfo> LoadDeviceAsync()
    {
        if (File.Exists(DeviceFilename))
            return JsonSerializer.Deserialize<BotDeviceInfo>(await File.ReadAllTextAsync(DeviceFilename))!;
        var device = BotDeviceInfo.GenerateInfo();
        await File.WriteAllTextAsync(DeviceFilename, JsonSerializer.Serialize(device));
        return device;
    }
}