using System.Runtime.InteropServices;
using SynBot.Attributes;
using SynBot.Infrastructure;

namespace SynBot.Commands;

[Command(["status"], "状态")]
public class StatusCommand : CommandBase
{
    public override Task HandleAsync()
    {
        Reply.Text($"""
                    [{SynBot.Name} {SynBot.Version} @ {SynBot.CommitHash[..7]}]
                    - RID: {RuntimeInformation.RuntimeIdentifier}
                    - OS: {RuntimeInformation.OSDescription}
                    - Runtime: {RuntimeInformation.FrameworkDescription}
                    """);
        return Task.CompletedTask;
    }
}