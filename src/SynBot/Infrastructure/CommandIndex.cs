using System.CommandLine;
using System.CommandLine.Parsing;
using FluentValidation.Results;

namespace SynBot.Infrastructure;

public partial class CommandIndex
{
    private readonly RootCommand _rootCommand = new();
    private readonly Dictionary<Command, Func<IServiceProvider, ParseResult, CommandBase>> _factories = new();
    private readonly Dictionary<Type, Func<IServiceProvider, CommandBase, ValidationResult?>> _validators = new();

    public ParseResult Parse(string input)
    {
        return _rootCommand.Parse(input);
    }

    public CommandBase CreateInstance(IServiceProvider services, ParseResult result)
    {
        return _factories[result.CommandResult.Command](services, result);
    }

    public ValidationResult? ValidateInstance(IServiceProvider services, CommandBase instance)
    {
        return _validators[instance.GetType()](services, instance);
    }
}