using System.Reflection;
using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SynBot.Infrastructure;
using SynBot.Utilities;

namespace SynBot;

public sealed class SynBot
{
    private readonly IHost _host;
    private readonly ILogger<SynBot> _logger;

    public static string Version { get; }

#if DEBUG
    public static string Name { get; } = "SynBot/Dev";
#else
    public static string Name { get; } = "SynBot";
#endif

    public static string CommitHash { get; }

    private readonly CommandIndex _commandIndex;

    static SynBot()
    {
        var versionParts = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            .Split('+');
        Version = versionParts?[0] ?? "unknown";
        CommitHash = versionParts is { Length: > 1 } ? versionParts[0] : new string('0', 40);
    }

    public SynBot(IHost host)
    {
        _host = host;
        _logger = _host.Services.GetRequiredService<ILogger<SynBot>>();
        _commandIndex = new CommandIndex();
    }

    public async Task RunAsync()
    {
        var keystore = _host.Services.GetRequiredService<IOptions<BotKeystore>>().Value;
        var device = _host.Services.GetRequiredService<IOptions<BotDeviceInfo>>().Value;

        var config = new BotConfig();
        var bot = BotFactory.Create(config, device, keystore);

        // Register events
        bot.Invoker.OnBotLogEvent += OnBotLogEvent;
        bot.Invoker.OnBotCaptchaEvent += OnBotCaptchaEvent;
        bot.Invoker.OnBotNewDeviceVerify += OnBotNewDeviceVerify;
        bot.Invoker.OnBotOnlineEvent += OnBotOnlineEvent;
        bot.Invoker.OnGroupMessageReceived += OnGroupMessageReceived;

        // Login
        if (keystore.Uin == 0)
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

    private void OnBotLogEvent(BotContext _, BotLogEvent log)
    {
        _logger.Log(LogLevelConverter.Convert(log.Level), "({Tag}) {Message}", log.Tag, log.EventMessage);
    }

    private void OnBotCaptchaEvent(BotContext bot, BotCaptchaEvent captcha)
    {
        _logger.LogWarning("Captcha required: {Url}", captcha.Url);

        Console.Write("Enter ticket: ");
        var ticket = Console.ReadLine();
        Console.Write("Enter randStr: ");
        var randStr = Console.ReadLine();

        ArgumentException.ThrowIfNullOrWhiteSpace(ticket);
        ArgumentException.ThrowIfNullOrWhiteSpace(randStr);

        bot.SubmitCaptcha(ticket, randStr);
    }


    private void OnBotNewDeviceVerify(BotContext bot, BotNewDeviceVerifyEvent verify)
    {
        File.WriteAllBytes("qrcode-newdevice.png", verify.QrCode);
        _logger.LogInformation("New device verification QRCode wrote.");
    }

    private void OnBotOnlineEvent(BotContext bot, BotOnlineEvent _)
    {
        AppConfiguration.SaveKeystore(bot.UpdateKeystore());
        _logger.LogInformation("Keystore updated.");
    }

    private void OnGroupMessageReceived(BotContext bot, GroupMessageEvent message)
    {
        // if (!_appOptions.AllowedGroups.Contains(@event.Chain.GroupUin!.Value))
        //     return;
        if (message.Chain.FriendUin == bot.BotUin)
            return;
        if (!message.Chain.HasTypeOf<TextEntity>())
            return;

        var input = string.Join('\n', message.Chain.OfType<TextEntity>().Select(entity => entity.Text));
        if (!input.StartsWith('#'))
            return;
        input = input.TrimStart('#');

        // Process command
        Task.Run(async () =>
        {
            var parseResult = _commandIndex.Parse(input);
            var reply = MessageBuilder.Group(message.Chain.GroupUin!.Value);

            if (parseResult.Errors.Any())
            {
                reply.Line("[Parse Error]");
                reply.Text(string.Join("\n", parseResult.Errors.Select(err => "- " + err.Message)));
                await bot.SendMessage(reply.Build());
                return;
            }

            using var scope = _host.Services.CreateScope();
            var instance = _commandIndex.CreateInstance(scope.ServiceProvider, parseResult);

            instance.SetRequiredProps(bot, message.Chain, reply);

            if (_commandIndex.ValidateInstance(scope.ServiceProvider, instance) is { IsValid: false } validationResult)
            {
                reply.Line("[Validation Error]");
                reply.Text(string.Join("\n", validationResult.Errors.Select(err => "- " + err.ErrorMessage)));
                await bot.SendMessage(reply.Build());
                return;
            }

            try
            {
                await instance.HandleAsync();
                await bot.SendMessage(reply.Build());
            }
            catch (Exception exception)
            {
                await bot.SendMessage(MessageBuilder
                    .Group(message.Chain.GroupUin!.Value)
                    .Line("[Execution Error]")
                    .Text(exception.ToString())
                    .Build());
                _logger.LogError(exception,
                    "An exception occurred while executing command: {ParseResult}", parseResult.ToString());
            }
        });
    }
}