namespace TimeTrackerBot;

internal class Program
{
    static async Task Main(string[] args)
    {
        string token = " ";
        var botClient = new Bot(token);
        Console.WriteLine("Бот запущен");
        await botClient.StartAsync();
        while (true)
        {
            Thread.Sleep(Timeout.Infinite);
        }
    }
}