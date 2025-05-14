namespace TimeTrackerBot;

public class Token
{
    private static Dictionary<long, string> _tokens = new();

    public static void SaveToken(long chatId, string token)
    {
        _tokens[chatId] = token;
    }

    public static string GetToken(long chatId)
    {
        return _tokens.TryGetValue(chatId, out var token) ? token : null;
    }
}
