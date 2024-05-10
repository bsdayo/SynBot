using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Lagrange.Core.Utility.Sign;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SynBot.Options;
using SynBot.Utilities;

namespace SynBot.Services;

// Based on https://github.com/LagrangeDev/Lagrange.Core/blob/master/Lagrange.Core/Utility/Sign/LinuxSigner.cs
public sealed class SignService : SignProvider
{
    private readonly HttpClient _client = new();
    private readonly ILogger<SignService> _logger;
    private readonly AppOptions _options;
    private readonly Timer _timer;

    public SignService(IOptions<AppOptions> appSettings, ILogger<SignService> logger)
    {
        _logger = logger;
        _options = appSettings.Value;
        _timer = new Timer(_ =>
        {
            var reconnect = Available = Test();
            if (reconnect) _timer?.Change(-1, 5000);
        });
    }

    public override byte[]? Sign(string cmd, uint seq, byte[] body, out byte[]? ver, out string? token)
    {
        ver = null;
        token = null;
        if (!WhiteListCommand.Contains(cmd)) return null;
        if (!Available || string.IsNullOrEmpty(_options.SignUrl)) return new byte[35]; // Dummy signature

        var payload = new
        {
            cmd,
            seq,
            src = body.Hex()
        };

        try
        {
            var message = _client.PostAsJsonAsync(_options.SignUrl, payload).Result;
            var response = message.Content.ReadAsStringAsync().Result;
            var value = JsonSerializer.Deserialize<JsonElement>(response).GetProperty("value");

            ver = value.GetProperty("extra").GetString()?.UnHex() ?? [];
            token = Encoding.ASCII.GetString(value.GetProperty("token").GetString()?.UnHex() ?? []);
            return value.GetProperty("sign").GetString()?.UnHex() ?? new byte[20];
        }
        catch (Exception)
        {
            Available = false;
            _timer.Change(0, 5000);
            _logger.LogWarning("Failed to get signature, using dummy signature");
            return new byte[20]; // Dummy signature
        }
    }

    public override bool Test()
    {
        try
        {
            var response = _client.GetStringAsync($"{_options.SignUrl}/ping").GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<JsonElement>(response).GetProperty("code").GetInt32() == 0;
        }
        catch
        {
            return false;
        }
    }
}