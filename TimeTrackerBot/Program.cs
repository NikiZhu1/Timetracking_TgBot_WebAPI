namespace TimeTrackerBot;

internal class Program
{
    static async Task Main(string[] args)
    {
        string token = "6761464907:AAHFMCFJJaRlEvt1obDsgYgqgliWw9mdyHg";
        var botClient = new Bot(token);
        Console.WriteLine("Бот запущен");
        await botClient.StartAsync();
        while (true)
        {
            Thread.Sleep(Timeout.Infinite);
        }
    }
}