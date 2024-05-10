using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.Extensions.Options;
using SynBot.Cmdlets;
using SynBot.Options;
using SynBot.Utilities;

namespace SynBot.Services;

public sealed class PowerShellService
{
    private readonly PowerShellOptions _options;
    private readonly Runspace _runspace;

    public PowerShellService(IOptions<PowerShellOptions> options)
    {
        _options = options.Value;
        var iss = InitialSessionState.Create();
        iss.LanguageMode = PSLanguageMode.ConstrainedLanguage;

        iss.Commands.AddCmdlet<GetRandomChoiceCmdlet>();

        _runspace = RunspaceFactory.CreateRunspace(iss);
        _runspace.Open();
    }

    public ICollection<PSObject> RunScript(string script)
    {
        var ps = PowerShell.Create(_runspace);
        var cts = new CancellationTokenSource(_options.InvocationTimeout);
        cts.Token.Register(() =>
        {
            ps.Stop();
            ps.Dispose();
        });
        var result = ps.AddScript(script).Invoke();
        if (ps.HadErrors)
            throw new PowerShellInvokeException(ps.Streams.Error);
        return result;
    }
}

public class PowerShellInvokeException(IEnumerable<ErrorRecord> errorRecords)
    : Exception(string.Join('\n', errorRecords));