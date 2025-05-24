using System.Configuration;

namespace TimeTrackerBot;

internal class Program
{
    static async Task Main(string[] args)
    {
        //Console.WriteLine($"app {ConfigurationManager.AppSettings["TELEGRAM_BOT_TOKEN"]}");
        //Console.WriteLine($"env {Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")}");
        //Console.WriteLine($"url {ConfigurationManager.AppSettings["BASE_API_URL"]}");

        string token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") ?? ConfigurationManager.AppSettings["TELEGRAM_BOT_TOKEN"];
        if (token == null)
        {
            Console.WriteLine("Токен не найден");
            return;
        }
        var botClient = new Bot(token);
        Console.WriteLine("Бот запущен");
        await botClient.StartAsync();
        while (true)
        {
            Thread.Sleep(Timeout.Infinite);
        }
    }
}