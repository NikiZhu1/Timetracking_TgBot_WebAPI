using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TimeTrackerBot;

public class CommandHandler
{
    private readonly ITelegramBotClient botClient;

    private readonly Auth Auth = new Auth();
    private readonly User User = new User();
    private readonly Activity Activity = new Activity();
    private readonly ActivityPeriod ActivityPeriod = new ActivityPeriod();
    private readonly Project Project = new Project();

    User? currentUser;

    public CommandHandler(ITelegramBotClient botClient)
    {
        this.botClient = botClient;
    }

    public async Task HandleMessageAsync(Message message)
    {
        var chatId = message.Chat.Id;
        var text = message.Text;
        (User.State state, int? activityId) userInfo = User.GetState(chatId);
        currentUser = await User.GetUserByChatId(chatId);

        if (currentUser == null) await Auth.Register(chatId, message.Chat.Username);
        else await Auth.Login(chatId, message.Chat.Username);

        Console.WriteLine($"Получено сообщение: {text}");

        if (message.Text != null && message.Text == "/start")
        {
            await botClient.SendTextMessageAsync(chatId,
               text: "*Старт* — запускается таймер активности,\n" +
               "*Стоп* — таймер активности останавливается.\n" +
               "\n" +
               "📊 Активности могут отслеживатся одновременно\n" +
               "⚠️ Главное не забывайте их останавливать\n" +
               "\n" +
               "Узнать больше о функциях бота — /help",
               parseMode: ParseMode.Markdown);
            User.SetTrackingState(chatId, User.TrackingState.PersonalTracking);
            await Init(chatId);
        }

        else if (message.Text != null && message.Text == "/archive")
        {
            User.ResetTrackingState(chatId);
            User.SetTrackingState(chatId, User.TrackingState.PersonalTracking);
            await InitArchive(chatId);
        }

        else if (message.Text != null && message.Text == "/projects")
        {
            User.ResetTrackingState(chatId);
            User.SetTrackingState(chatId, User.TrackingState.ProjectsTracking);
            InitProjects(chatId, true);
        }

        else if (message.Text != null && message.Text == "/activities")
        {
            User.SetTrackingState(chatId, User.TrackingState.PersonalTracking);
            var activities = await Activity.GetActivities(chatId, currentUser.id, true, true, false);
            if (activities.Count == 0) await botClient.SendTextMessageAsync(chatId, "У Вас пока нет активностей 📭");
            else await Init(chatId);
        }

        else if (message.Text != null && message.Text == "/help")
        {
            await botClient.SendTextMessageAsync(chatId, text: "Чтобы запустить бота нажмите на команду /start\n" +
            "Хотите узнать больше? В нашей <a href=\"https://telegra.ph/Lovec-vremeni--Spravka-05-26\">справке</a> есть вся информация о функциях бота!", parseMode: ParseMode.Html, replyMarkup: InlineKeyboard.Help());
        }

        else if (message.Text != null && message.Text == "/link")
        {

        }

        // Изменение названия активности
        else if (userInfo.state == User.State.WaitMessageForChangeAct && userInfo.activityId.HasValue)
        {
            if (message.Text == null) await botClient.SendTextMessageAsync(chatId, text: "❗ В качестве названия введите текст или смайлик.");
            else
            {
                string resultMessage = await Activity.UpdateActivityName(chatId, (int)userInfo.activityId, message.Text);
                await botClient.SendTextMessageAsync(chatId, resultMessage);
                User.ResetState(chatId);
                await Init(chatId);
            }
        }

        // Добавление активности
        else if (userInfo.state == User.State.WaitMessageForAddAct)
        {
            if (message.Text == null) await botClient.SendTextMessageAsync(chatId: chatId, text: "❗ В качестве названия введите текст или смайлик.");
            else
            {
                (User.TrackingState state, int? projectId)? trackingState = User.GetTrackingState(chatId);
                if (trackingState.Value.state == User.TrackingState.PersonalTracking)
                {
                    // обработать ситуации конла такое имя есть, когда такая активность есть в архиве итд
                    Activity newAct = await Activity.CreateActivity(chatId, currentUser.id, message.Text);
                    await Init(chatId);
                }

                if (trackingState.Value.state == User.TrackingState.ProjectsTracking)
                {
                    Project project = new Project();
                    int projectid = (int)trackingState.Value.projectId;
                    string resultMessage = await project.AddActivityInProject(chatId, projectid, currentUser.id, message.Text);
                    await botClient.SendTextMessageAsync(chatId, resultMessage);
                }
                User.ResetState(chatId);
            }
        }

        //Ввод периода статистики
        else if (userInfo.state == User.State.WaitingPeriodDates)
        {
            string? input = message.Text?.Trim();
            (User.TrackingState state, int? projectId)? trackingState = User.GetTrackingState(chatId);
            if (string.IsNullOrWhiteSpace(text))
                return;
            if (Regex.IsMatch(input, @"^\d{2}\.\d{2}\.\d{4}\s*-\s*\d{2}\.\d{2}\.\d{4}$"))
            {
                var dates = input.Split('-');
                if (DateTime.TryParseExact(dates[0].Trim(), "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime startDate) &&
                    DateTime.TryParseExact(dates[1].Trim(), "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime endDate))
                {
                    if (trackingState.Value.state == User.TrackingState.PersonalTracking)
                    {
                        var activities = await Activity.GetActivities(chatId, currentUser.id, true, true, false);
                        var list = await ActivityPeriod.GetStatisticList(chatId, activities, currentUser.id, startDate, endDate);
                        if (list.Count != 0)
                        {
                            string answer = await ActivityPeriod.SendStatictic(chatId, list, $"Статистика за определенный период:\n");
                            await botClient.SendTextMessageAsync(chatId: chatId, text: answer);
                        }
                        else await botClient.SendTextMessageAsync(chatId, "Записей отслеживания нет");
                    }
                    if (trackingState.Value.state == User.TrackingState.ProjectsTracking)
                    {
                        Project project = new Project();
                        int projectid = (int)trackingState.Value.projectId;
                        var users = await project.GetProjectUsers(chatId, projectid);
                        bool records = false;
                        foreach (var user in users)
                        {
                            var activities = await project.GetProjectActivities(chatId, projectid);
                            var list = await ActivityPeriod.GetStatisticList(chatId, activities, user.id, startDate, endDate);
                            if (list.Count != 0)
                            {
                                records = true;
                                string answer = await ActivityPeriod.SendStatictic(chatId, list, $"Статистика пользователя {user.name}:\n");
                                await botClient.SendTextMessageAsync(chatId: chatId, text: answer);
                            }
                        }
                        if (!records) await botClient.SendTextMessageAsync(chatId, "Для этого проекта записей отслеживания нет");
                    }
                }
                else await botClient.SendTextMessageAsync(chatId, "⚠️ Неверный формат дат. Используй формат `дд.мм.гггг - дд.мм.гггг`");
            }
            else await botClient.SendTextMessageAsync(chatId, "📅 Введите период в формате:\n*дд.мм.гггг - дд.мм.гггг*");
            User.ResetState(chatId);
        }

        // Ввод опредленного дня для статистики
        else if (userInfo.state == User.State.WaitingCertainDate)
        {
            (User.TrackingState state, int? projectId)? trackingState = User.GetTrackingState(chatId);

            if (DateTime.TryParseExact(text.Trim(), "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date))
            {
                if (trackingState.Value.state == User.TrackingState.PersonalTracking)
                {
                    var activities = await Activity.GetActivities(chatId, currentUser.id, true, true, false);
                    var list = await ActivityPeriod.GetStatisticList(chatId, activities, currentUser.id, date);
                    if (list.Count != 0)
                    {
                        string answer = await ActivityPeriod.SendStatictic(chatId, list, "Статистика за определенный день:");
                        await botClient.SendTextMessageAsync(chatId: chatId, text: answer);
                    }
                    else await botClient.SendTextMessageAsync(chatId, "Записей отслеживания нет");
                }
                if (trackingState.Value.state == User.TrackingState.ProjectsTracking)
                {
                    Project project = new Project();
                    int projectid = (int)trackingState.Value.projectId;
                    var users = await project.GetProjectUsers(chatId, projectid);
                    bool records = false;
                    foreach (var user in users)
                    {
                        var activities = await project.GetProjectActivities(chatId, projectid);
                        var list = await ActivityPeriod.GetStatisticList(chatId, activities, user.id, date);
                        if (list.Count != 0)
                        {
                            records = true;
                            string answer = await ActivityPeriod.SendStatictic(chatId, list, $"Статистика пользователя {user.name}:\n");
                            await botClient.SendTextMessageAsync(chatId: chatId, text: answer);
                        }
                    }
                    if (!records) await botClient.SendTextMessageAsync(chatId, "Для этого проекта записей отслеживания нет");
                }
            }
            else await botClient.SendTextMessageAsync(chatId, "⚠️ Неверный формат дат. Используйте формат `дд.мм.гггг`");
            User.ResetState(chatId);
        }

        // Создание проекта
        else if (userInfo.state == User.State.WaitingMessageForAddProject)
        {
            if (message.Text == null) await botClient.SendTextMessageAsync(chatId: chatId, text: "❗ В качестве названия введите текст или смайлик.");
            else
            {
                await Project.CreateProject(chatId, currentUser.id, message.Text);
                User.ResetState(chatId);
                await InitProjects(chatId, true);
            }
        }

        else if (userInfo.state == User.State.WaitingMessageForConnectToProject)
        {
            Project Project = new Project();
            string resultMessage = await Project.Connect(chatId, message.Text);
            await botClient.SendTextMessageAsync(chatId, resultMessage);
            User.ResetState(chatId);
            await InitProjects(chatId, true);
        }

        // Изменение названия проекта
        else if (userInfo.state == User.State.WaitMessageForChangeProject)
        {
            (User.TrackingState state, int? projectId)? trackingState = User.GetTrackingState(chatId);
            int projectid = (int)trackingState.Value.projectId;
            string resultMessage = await Project.ChangeProjectName(chatId, projectid, message.Text);
            await botClient.SendTextMessageAsync(chatId, resultMessage);
            User.ResetState(chatId);
            await InitProjects(chatId, true);
        }

        // Добавление пользователя
        else if (userInfo.state == User.State.WaitingMessageForAddProjectUser)
        {
            (User.TrackingState state, int? projectId)? trackingState = User.GetTrackingState(chatId);
            if (trackingState.Value.state == User.TrackingState.ProjectsTracking)
            {
                Project project = new Project();
                int projectid = (int)trackingState.Value.projectId;
                string resultMessage = await project.AddUserInProject(chatId, projectid, message.Text);
                await botClient.SendTextMessageAsync(chatId, resultMessage);
                User.ResetState(chatId);
            }
        }
    }

    public async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
    {
        int messageId = callbackQuery.Message.MessageId;
        long chatId = callbackQuery.Message.Chat.Id;
        currentUser = await User.GetUserByChatId(chatId);
        await Auth.Login(chatId, currentUser.name);
        var activityList = await Activity.GetActivities(chatId, currentUser.id, true, true, false);

        switch (Regex.Replace(callbackQuery.Data, @"\d", ""))
        {
            case "add_activity":
                {
                    User.SetState(chatId, User.State.WaitMessageForAddAct);
                    await botClient.SendTextMessageAsync(chatId, text: $"✏ Введите название для новой активности");
                    // InlineKeyboard.SetMessageIdForDelete(chatId, messageId);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    break;
                }

            case "aboutAct":
                {
                    int activityId = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    Activity? activity = activityList.FirstOrDefault(a => a.id == activityId);

                    if (activity != null)
                    {
                        string status = activity.statusId == 2 ? ": Отслеживается ⏱" : "";
                        Message messageAct = await botClient.SendTextMessageAsync(chatId, text: $"{activity.name}{status}\n\n" +
                            $"Вы можете изменить название активности, отправить в архив или удалить её",
                            parseMode: ParseMode.Markdown, replyMarkup: InlineKeyboard.ChangeActivity(activityId));
                    }
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    break;
                }

            case "rename":
                {
                    int activityId = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    Activity? activity = activityList.FirstOrDefault(a => a.id == activityId);
                    User.SetState(chatId, User.State.WaitMessageForChangeAct, activityId);
                    await botClient.SendTextMessageAsync(chatId, text: $"✏️ Введите новое название для активности \"{activity.name}\"");
                    await botClient.DeleteMessageAsync(chatId, messageId);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    break;
                }

            case "delete":
                {
                    int activityId = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    Activity? activity = activityList.FirstOrDefault(a => a.id == activityId);
                    if (activity.statusId == 2)
                    {
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "⚙️ Вы удалили отслеживаемую активность.", showAlert: true);
                    }
                    try
                    {
                        string resultMessage = await Activity.DeleteActivity(chatId, activityId);
                        await botClient.SendTextMessageAsync(chatId, resultMessage);
                        await botClient.DeleteMessageAsync(chatId, messageId);
                        //await botClient.SendTextMessageAsync(chatId, text: $"🗑 {activity.name}: активность удалена");
                        await Init(chatId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка: " + chatId + " " + ex.Message);
                        await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением данных: {ex.Message}.\n"
                        + $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                    }

                    break;
                }

            case "archive":
                {
                    int activityId = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    Activity? activity = activityList.FirstOrDefault(a => a.id == activityId);

                    if (activity.statusId == 2)
                    {
                        await ActivityPeriod.Stop(chatId, activityId, activity);
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "⚙️ Вы отправили в архив отслеживаемую активность. Её таймер остановлен.", showAlert: true);
                    }
                    try
                    {
                        string result = await Activity.ChangeActivityStatus(chatId, activityId, true);
                        //int tempMessageId = InlineKeyboard.GetMessageIdForDelete(chatId);
                        //InlineKeyboard.RemoveMessageId(chatId);
                        //if (tempMessageId != 0) await botClient.DeleteMessageAsync(chatId, tempMessageId);
                        await botClient.SendTextMessageAsync(chatId, text: result);
                        await botClient.DeleteMessageAsync(chatId, messageId);
                        await Init(chatId);
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка: " + chatId + " " + ex.Message);
                        await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением данных: {ex.Message}.\n" + $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                    }
                    break;
                }

            case "aboutArchive":
                {
                    int activityId = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    List<Activity> archive = await Activity.GetActivities(chatId, currentUser.id, false, false, true);
                    Activity? activity = archive.FirstOrDefault(a => a.id == activityId);
                    await botClient.EditMessageTextAsync(chatId, messageId, text: $"🗂 {activity.name} в архиве\n\n" +
                        $"Вы можете восстановить её, чтобы снова отслеживать её, или полностью удалить.", replyMarkup: InlineKeyboard.ChangeArchive(activityId));

                    // int tempMessageId = InlineKeyboard.GetMessageIdForDelete(chatId);
                    // InlineKeyboard.RemoveMessageId(chatId);
                    //if (tempMessageId != 0) await botClient.DeleteMessageAsync(chatId, tempMessageId);
                    //InlineKeyboard.SetMessageIdForDelete(chatId, messageId);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    break;
                }

            case "backToArchive":
                {
                    List<Activity> archive = await Activity.GetActivities(chatId, currentUser.id, false, false, true);

                    if (archive.Count == 0)
                    {
                        await botClient.EditMessageTextAsync(chatId, messageId, "🗂 Архив пуст\n\n" + "ℹ️ Когда Вы захотите временно скрыть некоторые активности из главного меню и не отслеживать их, " +
                            "Вы можете добавить их в архив, и они будут храниться здесь.");
                        break;
                    }
                    InlineKeyboardMarkup archivedActivityKeyboard = InlineKeyboard.Archive(archive);
                    await botClient.EditMessageTextAsync(chatId, messageId,
                        "🗂 Архив\n\n" + "ℹ️ Эти активности в данный момент скрыты из главного меню, и их отслеживание недоступно. " +
                        "Вы можете восстановить их или удалить, нажав на нужную активность.", replyMarkup: archivedActivityKeyboard);

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    break;
                }

            case "recover":
                {
                    int activityId = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    List<Activity> archive = await Activity.GetActivities(chatId, currentUser.id, false, false, true);
                    Activity? activity = archive.FirstOrDefault(a => a.id == activityId);
                    try
                    {
                        string resultMessage = await Activity.ChangeActivityStatus(chatId, activityId, false);
                        await botClient.SendTextMessageAsync(chatId, resultMessage);
                        //await botClient.SendTextMessageAsync(chatId, text: $"📤 {activity.name}: восстановлено из архива");
                        await InitArchive(chatId);
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка: " + chatId + " " + ex.Message);
                        await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка: {ex.Message}.\n" + $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                    }
                    break;
                }

            case "deleteInArchive":
                {
                    int activityId = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    List<Activity> archive = await Activity.GetActivities(chatId, currentUser.id, false, false, true);
                    Activity? activity = archive.FirstOrDefault(a => a.id == activityId);

                    try
                    {
                        string resultMessage = await Activity.DeleteActivity(chatId, activityId);
                        await botClient.SendTextMessageAsync(chatId, resultMessage);
                        //await botClient.SendTextMessageAsync(chatId,text: $"🗑 {activity.name}: активность удалена");
                        await InitArchive(chatId);
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка: " + chatId + " " + ex.Message);
                        await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением данных: {ex.Message}.\n" + $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                    }
                    break;
                }

            case "start_":
                {
                    (User.TrackingState state, int? projectId)? trackingState = User.GetTrackingState(chatId);
                    int activityId = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));

                    Activity activity = await Activity.GetActivityById(chatId, activityId);
                    var result = await ActivityPeriod.Start(chatId, activityId, activity);

                    if (trackingState.Value.state == User.TrackingState.PersonalTracking)
                    {
                        var activities = await Activity.GetActivities(chatId, currentUser.id, true, true, false);
                        await botClient.EditMessageReplyMarkup(chatId, messageId, replyMarkup: InlineKeyboard.Main(activities));
                    }

                    else if (trackingState.Value.state == User.TrackingState.ProjectsTracking)
                    {
                        var activities = await Project.GetProjectActivities(chatId, (int)trackingState.Value.projectId);
                        await botClient.EditMessageReplyMarkup(chatId, messageId, replyMarkup: InlineKeyboard.ProjectActivitiesKB(activities));
                    }


                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                    break;
                }

            case "stop_":
                {
                    try
                    {
                        int activityId = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        (User.TrackingState state, int? projectId)? trackingState = User.GetTrackingState(chatId);

                        if (messageId != 0) await botClient.DeleteMessage(chatId, messageId);

                        Activity activity = await Activity.GetActivityById(chatId, activityId);
                        string ans = await ActivityPeriod.Stop(chatId, activityId, activity);
                        if (trackingState.Value.state == User.TrackingState.PersonalTracking)
                        {
                            Message messageAct = await botClient.SendTextMessageAsync(chatId,
                                 text: "⏱ Вот все Ваши активности. Нажмите на ту, которую хотите изменить или узнать подробности.",
                                 replyMarkup: InlineKeyboard.Main(await Activity.GetActivities(chatId, currentUser.id, true, true, false)));
                            InlineKeyboard.SetMessageIdForDelete(chatId, messageAct.MessageId);

                        }
                        if (trackingState.Value.state == User.TrackingState.ProjectsTracking)
                        {
                            var Project = new Project();
                            var activities = await Project.GetProjectActivities(chatId, (int)trackingState.Value.projectId);
                            Message messageAct = await botClient.SendTextMessageAsync(chatId,
                                text: "⏱ Вот активности проекта. Нажмите на ту, которую хотите изменить или узнать подробности.", replyMarkup: InlineKeyboard.ProjectActivitiesKB(activities));
                            InlineKeyboard.SetMessageIdForDelete(chatId, messageAct.MessageId);
                        }
                        await botClient.SendTextMessageAsync(chatId, ans);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Ошибка: " + chatId + " " + e.Message);
                    }
                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                    break;
                }

            case "show_statistic":
                {
                    await botClient.SendTextMessageAsync(chatId, text: "Выберете, в каком формате Вы хотите получить статистику",
                        parseMode: ParseMode.Markdown, replyMarkup: InlineKeyboard.StaticticType());
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    break;
                }

            case "statistic_":
                {
                    var period = new ActivityPeriod();
                    int statisticType = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    (User.TrackingState state, int? projectId)? trackingState = User.GetTrackingState(chatId);
                    //За всё время
                    if (statisticType == 1)
                    {
                        List<(Activity, TimeSpan?)> list = new List<(Activity, TimeSpan?)>();
                        if (trackingState.Value.state == User.TrackingState.PersonalTracking)
                        {
                            list = await period.GetStatisticList(chatId, await Activity.GetActivities(chatId, currentUser.id, true, true, false), currentUser.id);
                            if (list.Count != 0)
                            {
                                string answer = await period.SendStatictic(chatId, list, "Статистика за всё время:");
                                await botClient.SendTextMessageAsync(chatId: chatId, text: answer);
                            }
                            else await botClient.SendTextMessageAsync(chatId, "Записей отслеживания нет");
                        }

                        if (trackingState.Value.state == User.TrackingState.ProjectsTracking)
                        {
                            Project project = new Project();
                            int projectid = (int)trackingState.Value.projectId;
                            var users = await project.GetProjectUsers(chatId, projectid);
                            bool records = false;
                            foreach (var user in users)
                            {
                                list = await period.GetStatisticList(chatId, await project.GetProjectActivities(chatId, projectid), user.id);
                                if (list.Count != 0)
                                {
                                    records = true;
                                    string answer = await period.SendStatictic(chatId, list, $"Статистика пользователя {user.name}:");
                                    await botClient.SendTextMessageAsync(chatId: chatId, text: answer);
                                }
                            }
                            if (!records) await botClient.SendTextMessageAsync(chatId, "Для этого проекта записей отслеживания нет");
                        }
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    }

                    //За промежуток времени
                    else if (statisticType == 2)
                    {
                        User.SetState(chatId, User.State.WaitingPeriodDates);
                        await botClient.SendTextMessageAsync(chatId, "📅 Введите период в формате:\n*дд.мм.гггг - дд.мм.гггг*");
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    }

                    //За опредленный день
                    else if (statisticType == 3)
                    {
                        User.SetState(chatId, User.State.WaitingCertainDate);
                        await botClient.SendTextMessageAsync(chatId, "📅 Введите дату в формате:\n*дд.мм.гггг*");
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    }
                    break;
                }

            case "add_project":
            case "projectInfo":
            case "updateProject":
            case "projectActivities":
            case "closedProjects":
            case "close":
            case "deleteProject":
            case "addUserInProject":
            case "addActivityInProject":
            case "renameProject":
            case "conectTo":
            case "removeActivity":
            case "removeUser":
            case "deleteUser":
            case "deleteActivity":
            case "closedProjectInfo":
                {
                    await HandleCallBackProjects(callbackQuery);
                    break;
                }

        }
    }

    public async Task HandleCallBackProjects(CallbackQuery callbackQuery)
    {
        int messageId = callbackQuery.Message.MessageId;
        long chatId = callbackQuery.Message.Chat.Id;
        currentUser = await User.GetUserByChatId(chatId);
        await Auth.Login(chatId, currentUser.name);

        switch (Regex.Replace(callbackQuery.Data, @"\d", ""))
        {
            case "add_project":
                {
                    User.SetState(chatId, User.State.WaitingMessageForAddProject);
                    await botClient.SendTextMessageAsync(chatId, text: $"✏ Введите название проекта");
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    break;
                }

            case "closedProjects":
                {
                    await InitProjects(chatId, false);
                    break;
                }

            case "projectInfo":
                {
                    int projectid = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    User.SetTrackingState(chatId, User.TrackingState.ProjectsTracking, projectid);
                    Project? currProject = await Project.GetProjectById(chatId, projectid);
                    string participants = "";
                    var users = await Project.GetProjectParticipants(chatId, projectid);
                    foreach (var participant in users)
                    {
                        string role = participant.isCreator ? "Создатель проекта" : "Участник";
                        var user = await User.GetUserById(participant.userId);
                        participants += $"{user.name} - {role} \n";
                    }
                    if (currProject != null)
                    {
                        Message messageAct = await botClient.SendTextMessageAsync(chatId, text: $"{currProject.projectName}\n\n" + "Участники проекта:\n" + $"{participants}",
                         parseMode: ParseMode.Markdown, replyMarkup: InlineKeyboard.ProjectInfo(projectid));
                    }

                    break;
                }

            case "closedProjectInfo":
                {
                    int projectid = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    User.SetTrackingState(chatId, User.TrackingState.ProjectsTracking, projectid);
                    Project? currProject = await Project.GetProjectById(chatId, projectid);
                    string participants = "";
                    var users = await Project.GetProjectParticipants(chatId, projectid);
                    foreach (var participant in users)
                    {
                        string role = participant.isCreator ? "Создатель проекта" : "Участник";
                        var user = await User.GetUserById(participant.userId);
                        participants += $"{user.name} - {role} \n";
                    }

                    string activities = "";
                    var projectactivities = await Project.GetProjectActivities(chatId, projectid);
                    foreach (var act in projectactivities)
                    {
                        activities += $"{act.name} \n";
                    }

                    if (currProject != null)
                    {
                        Message messageAct = await botClient.SendTextMessageAsync(chatId, text: $"{currProject.projectName}\n\n"
                            + " 👤 Участники проекта:\n" + $"{participants}\n" + "✅ Активности проекта: \n" + $"{activities}",
                             parseMode: ParseMode.Markdown, replyMarkup: InlineKeyboard.ClosedProjectInfo());
                    }
                    break;
                }

            case "projectActivities":
                {
                    int projectid = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    var Project = new Project();
                    var activities = await Project.GetProjectActivities(chatId, projectid);
                    Project? currProject = await Project.GetProjectById(chatId, projectid);
                    if (activities.Count == 0) await botClient.SendTextMessageAsync(chatId, "В этом прoекте пока нет активностей");
                    else
                    {
                        InlineKeyboardMarkup activityKeyboard = InlineKeyboard.ProjectActivitiesKB(activities);
                        Message messageAct = await botClient.SendTextMessageAsync(chatId: chatId, text: $"Вот активности проекта {currProject.projectName}. Нажмите на ту, которую хотите изменить или узнать подробности.", replyMarkup: activityKeyboard);

                        await botClient.DeleteMessageAsync(chatId, messageId);
                    }
                    break;
                }

            case "updateProject":
                {
                    int projectId = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    var project = await Project.GetProjectById(chatId, projectId);

                    if (project != null)
                    {
                        Message messageAct = await botClient.SendTextMessageAsync(chatId, text: $"{project.projectName}\n" +
                            $"Вы можете изменить название проекта, добавить активности или участников",
                            parseMode: ParseMode.Markdown, replyMarkup: InlineKeyboard.ChangeProjectKB(projectId));
                    }
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);

                    break;
                }

            case "close":
                {
                    int projectid = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    string resultMessage = await Project.CloseProject(chatId, projectid);

                    await botClient.SendTextMessageAsync(chatId, resultMessage);
                    await botClient.DeleteMessageAsync(chatId, messageId);
                    break;
                }

            case "deleteProject":
                {
                    int projectid = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    string resultMessage = await Project.DeleteProject(chatId, projectid);

                    await botClient.SendTextMessageAsync(chatId, resultMessage);
                    await botClient.DeleteMessageAsync(chatId, messageId);
                    break;
                }

            case "renameProject":
                {
                    int projectid = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    Project project = await Project.GetProjectById(chatId, projectid);

                    User.SetState(chatId, User.State.WaitMessageForChangeProject);
                    await botClient.SendTextMessageAsync(chatId, text: $"✏️ Введите новое название для проекта \"{project.projectName}\"");
                    await botClient.DeleteMessageAsync(chatId, messageId);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    break;
                }

            case "addUserInProject":
                {
                    User.SetState(chatId, User.State.WaitingMessageForAddProjectUser);
                    await botClient.SendTextMessageAsync(chatId, text: $"✏ Введите имя пользователя, которого Вы хотите добавить в проект");
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    break;
                }

            case "addActivityInProject":
                {
                    User.SetState(chatId, User.State.WaitMessageForAddAct);
                    await botClient.SendTextMessageAsync(chatId, text: $"✏ Введите название новой активности или существующей активности, которую Вы хотите добавить в проект");
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    break;
                }

            case "conectTo":
                {
                    User.SetState(chatId, User.State.WaitingMessageForConnectToProject);
                    await botClient.SendTextMessageAsync(chatId, text: $"✏️ Введите ключ доступа проекта ");
                    await botClient.DeleteMessageAsync(chatId, messageId);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    break;
                }

            case "removeActivity":
                {
                    int projectid = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    var activities = await Project.GetProjectActivities(chatId, projectid);
                    Project? currProject = await Project.GetProjectById(chatId, projectid);
                    await botClient.SendTextMessageAsync(chatId, text: $"Активности проекта {currProject.projectName}.\n Нажмите на активность, которую хотите удалить из проекта. \n",
                        parseMode: ParseMode.Markdown, replyMarkup: InlineKeyboard.DeletingActivitiesKB(activities));
                    break;
                }

            case "deleteActivity":
                {
                    (User.TrackingState state, int? projectId)? trackingState = User.GetTrackingState(chatId);
                    int projectId = (int)trackingState.Value.projectId;
                    int actId = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    string resultMessage = await Project.DeleteProjectActivity(chatId, projectId, actId);
                    await botClient.SendTextMessageAsync(chatId, resultMessage);
                    await botClient.DeleteMessageAsync(chatId, messageId);
                    break;
                }

            case "removeUser":
                {
                    int projectid = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    Project Project = new Project();
                    var participants = await Project.GetProjectUsers(chatId, projectid);
                    Project? currProject = await Project.GetProjectById(chatId, projectid);
                    await botClient.SendTextMessageAsync(chatId, text: $"Участники проекта {currProject.projectName}.\n Нажмите на пользователя, которого хотите исключить из проекта. \n",
                        parseMode: ParseMode.Markdown, replyMarkup: InlineKeyboard.DeletingUsersKB(participants));
                    break;
                }

            case "deleteUser":
                {
                    (User.TrackingState state, int? projectId)? trackingState = User.GetTrackingState(chatId);
                    int projectId = (int)trackingState.Value.projectId;
                    int userId = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                    string resultMessage = await Project.DeleteProjectUser(chatId, projectId, userId);

                    await botClient.SendTextMessageAsync(chatId, resultMessage);
                    await botClient.DeleteMessageAsync(chatId, messageId);
                    break;
                }
        }
    }

    public async Task Init(long chatId)
    {
        List<Activity> activityList = await Activity.GetActivities(chatId, currentUser.id, true, true, false);
        InlineKeyboardMarkup activityKeyboard = InlineKeyboard.Main(activityList);
        Message messageAct = await botClient.SendTextMessageAsync(chatId: chatId, text: "⏱ Вот все Ваши активности. Нажмите на ту, которую хотите изменить или узнать подробности.", replyMarkup: activityKeyboard);

        int tempMessageId = InlineKeyboard.GetMessageIdForDelete(chatId);
        InlineKeyboard.RemoveMessageId(chatId);
        if (tempMessageId != 0) await botClient.DeleteMessageAsync(chatId, tempMessageId);
        InlineKeyboard.SetMessageIdForDelete(chatId, messageAct.MessageId);
    }

    public async Task InitArchive(long chatId)
    {
        List<Activity> archive = await Activity.GetActivities(chatId, currentUser.id, false, false, true);
        Message messageArchive = null;
        if (archive.Count != 0)
        {
            InlineKeyboardMarkup archivedActivityKeyboard = InlineKeyboard.Archive(archive);
            messageArchive = await botClient.SendTextMessageAsync(chatId, "🗂 Архив\n\n" + "ℹ️ Эти активности в данный момент скрыты из главного меню, и их отслеживание недоступно. " +
           "Вы можете восстановить их или удалить, нажав на нужную активность.", replyMarkup: archivedActivityKeyboard);
        }
        else
        {
            messageArchive = await botClient.SendTextMessageAsync(chatId, "🗂 Архив пуст\n\n" +
            "ℹ️ Когда вы захотите временно скрыть некоторые активности из главного меню и не отслеживать их, " + "вы можете добавить их в архив, и они будут храниться здесь.");
        }
        int tempMessageId = InlineKeyboard.GetMessageIdForDelete(chatId);
        await botClient.DeleteMessageAsync(chatId, tempMessageId);
        InlineKeyboard.SetMessageIdForDelete(chatId, messageArchive.MessageId);
    }

    public async Task InitProjects(long chatId, bool current)
    {
        var projects = await Project.GetProjectsByUserId(chatId, currentUser.id, current);
        if (projects.Count == 0) await botClient.SendTextMessageAsync(chatId, "У Вас пока нет проектов 📭");
        Message messageAct;
        InlineKeyboardMarkup projectKeyboard = InlineKeyboard.ProjectKB(projects, current);
        if (current)
            messageAct = await botClient.SendTextMessageAsync(chatId: chatId, text: "Вот все Ваши проекты. Нажмите на тот, который хотите изменить или узнать подробности.", replyMarkup: projectKeyboard);
        else
            messageAct = await botClient.SendTextMessageAsync(chatId: chatId, text: "Вот Ваши завершенные проекты. Нажмите, чтобы узнать подробности", replyMarkup: projectKeyboard);

        int tempMessageId = InlineKeyboard.GetMessageIdForDelete(chatId);
        InlineKeyboard.RemoveMessageId(chatId);
        if (tempMessageId != 0) await botClient.DeleteMessageAsync(chatId, tempMessageId);
        InlineKeyboard.SetMessageIdForDelete(chatId, messageAct.MessageId);
    }
}
