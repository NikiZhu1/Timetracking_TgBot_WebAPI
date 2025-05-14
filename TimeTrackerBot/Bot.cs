using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TimeTrackerBot;

public class Bot
{
    private readonly TelegramBotClient client;

    public Bot(string token)
    {
        client = new TelegramBotClient(token);
    }

    public async Task StartAsync()
    {
        client.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync
        );
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        var handler = new CommandHandler(botClient);
        switch (update.Type)
        {
            case UpdateType.Message:
                var message = update.Message;
                await handler.HandleMessageAsync(message);
                break;

            case UpdateType.CallbackQuery:
                var callbackQuery = update.CallbackQuery;
                await handler.HandleCallbackQueryAsync(callbackQuery);
                break;
        }
    }

    private async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken token)
    {
        Console.WriteLine("Произошла ошибка: " + exception.Message);
    }
}
