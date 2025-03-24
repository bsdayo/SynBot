using System.Text;

namespace SynBot.Copilot.Utilities;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendIndent(this StringBuilder sb, int level)
    {
        return sb.Append(new string(' ', level * 4));
    }
}