using System.CommandLine;
using System.CommandLine.Parsing;

namespace SynBot.Infrastructure;

public class CommandIndexTemplate
{
    private readonly RootCommand _rootCommand = new();

    private readonly Command _aaaCommand;
    private readonly Command _bbbCommand;

    private readonly Dictionary<Command, Func<IServiceProvider, ParseResult, CommandBase>> _factories = new();

    public CommandIndexTemplate()
    {
        _bbbCommand = new Command("bbb");
        _aaaCommand = new Command("aaa");
        _rootCommand.AddCommand(_aaaCommand);
        _aaaCommand.AddCommand(_bbbCommand);
    }

    public CommandBase CreateInstance(IServiceProvider services, ParseResult result)
    {
        return _factories[result.CommandResult.Command](services, result);
    }
}