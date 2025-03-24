using Lagrange.Core;
using Lagrange.Core.Message;

namespace SynBot.Infrastructure;

public abstract class CommandBase
{
    protected BotContext Bot { get; private set; } = null!;

    protected MessageChain Message { get; private set; } = null!;

    protected MessageBuilder Reply { get; private set; } = null!;

    internal void SetRequiredProps(BotContext bot, MessageChain message, MessageBuilder reply)
    {
        Bot = bot;
        Message = message;
        Reply = reply;
    }

    public abstract Task HandleAsync();
}