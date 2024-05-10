using System.Management.Automation;

namespace SynBot.Cmdlets;

[Alias("帮我选")]
[Cmdlet(VerbsCommon.Get, "RandomChoice")]
public sealed class GetRandomChoiceCmdlet : Cmdlet
{
    [Parameter(ValueFromRemainingArguments = true, ValueFromPipeline = true)]
    public string[] Choices { get; set; } = [];

    protected override void ProcessRecord()
    {
        WriteObject("建议你选择" + Choices[Random.Shared.Next(Choices.Length)]);
    }
}