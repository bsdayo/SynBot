using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;

namespace SynBot.Utilities;

public static class InitialSessionStateExtensions
{
    public static void AddCmdlet<TCmdlet>(this InitialSessionStateEntryCollection<SessionStateCommandEntry> commands)
        where TCmdlet : Cmdlet
    {
        var type = typeof(TCmdlet);
        var cmdletAttr = type.GetCustomAttribute<CmdletAttribute>();
        if (cmdletAttr is null)
            throw new InvalidOperationException($"{nameof(Cmdlet)} class should have {nameof(CmdletAttribute)}.");
        var cmdletName = $"{cmdletAttr.VerbName}-{cmdletAttr.NounName}";
        commands.Add(new SessionStateCmdletEntry(cmdletName, type, null));

        var aliasAttr = type.GetCustomAttribute<AliasAttribute>();
        if (aliasAttr is not null)
            commands.Add(aliasAttr.AliasNames.Select(name => new SessionStateAliasEntry(name, cmdletName)));
    }
}