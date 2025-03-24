namespace SynBot.Infrastructure;

public static class TypeParser
{
    public static string String(string str)
    {
        return str;
    }

    public static int Int32(string str)
    {
        return int.Parse(str);
    }
}