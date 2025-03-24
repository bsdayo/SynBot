using Lagrange.Core.Message;

namespace SynBot.Utilities;

public static class MessageBuilderExtensions
{
    public static MessageBuilder Line(this MessageBuilder builder, string text)
    {
        builder.Text(text + '\n');
        return builder;
    }
}