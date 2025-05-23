using System.Configuration;

namespace TimeTrackerBot;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine($"app {ConfigurationManager.AppSettings["TELEGRAM_BOT_TOKEN"]}");
        Console.WriteLine($"env {Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")}");

        string token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") ?? ConfigurationManager.AppSettings["TELEGRAM_BOT_TOKEN"];
        var botClient = new Bot(token);
        Console.WriteLine("Бот запущен");
        await botClient.StartAsync();
        while (true)
        {
            Thread.Sleep(Timeout.Infinite);
        }
    }
}