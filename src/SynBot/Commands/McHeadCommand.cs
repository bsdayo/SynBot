using SynBot.Attributes;
using SynBot.Infrastructure;

namespace SynBot.Commands;

public enum McHeadType
{
    Avatar,
    Head,
    Body
}

[Command(["mchead"])]
public class McHeadCommand(IHttpClientFactory httpClientFactory) : CommandBase
{
    [Argument("username")]
    public string Username { get; set; } = null!;

    [Option(["-t", "--type"], "获取类别")]
    public McHeadType Type { get; set; } = McHeadType.Avatar;

    public override async Task HandleAsync()
    {
        var client = httpClientFactory.CreateClient();
        var image = await client.GetByteArrayAsync($"https://mc-heads.net/{Type}/{Username}");
        Reply.Image(image);
    }
}