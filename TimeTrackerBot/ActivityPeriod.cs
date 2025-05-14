using TimeTrackerBot.ApiServices;

namespace TimeTrackerBot;

public class ActivityPeriod
{
    public int activityPeriodId { get; set; }
    public int activityId { get; set; }
    public int executorId { get; set; }
    public DateTime? startTime { get; set; } = null;
    public DateTime? stopTime { get; set; } = null;
    public TimeSpan? totalTime { get; set; } = null;

    private readonly TrackingService api = new();

    public async Task<List<(Activity, TimeSpan?)>> GetStatisticList(long chatId, List<Activity> activityList, int userId = 0, DateTime? firstDate = null, DateTime? secondDate = null)
    {
        List<(Activity, TimeSpan?)> statis = new();

        try
        {
            foreach (Activity activity in activityList)
            {
                TimeSpan? total = TimeSpan.Zero;
                List<ActivityPeriod> activityPeriods = new();

                if (!firstDate.HasValue && !secondDate.HasValue) //за всё время
                {
                    activityPeriods = await api.GetStatisticsAsync(chatId, userId, activity.id);
                }
                else if (firstDate.HasValue && !secondDate.HasValue || !firstDate.HasValue && secondDate.HasValue) //за день
                {
                    activityPeriods = await api.GetStatisticsAsync(chatId, userId, activity.id, firstDate.Value.Date);
                }
                else //за определённый период (неделя, месяц)
                {
                    activityPeriods = await api.GetStatisticsAsync(chatId, userId, activity.id, firstDate, secondDate);
                }
                if (activityPeriods.Count != 0)
                {
                    total = await CountSumTime(activityPeriods);
                    statis.Add((activity, total));
                }

            }
        }
        catch (Exception)
        {
            throw;
        }
        return statis;
    }

    public async Task<TimeSpan?> CountSumTime(List<ActivityPeriod> activityPeriods)
    {
        TimeSpan? totaltime = TimeSpan.Zero;
        foreach (ActivityPeriod activityPeriod in activityPeriods)
        {
            totaltime += activityPeriod.totalTime;
        }
        return totaltime;
    }

    public async Task<string> SendStatictic(long chatId, List<(Activity, TimeSpan?)> statisticList, string message)
    {
        if (statisticList.Count == 0)
            return "У Вас пока нет записей об активностях за этот период.\n" + "🚀 Запускайте таймер и сможете отследить свой прогресс!";
        string text = message + "\n";
        for (int i = 0; i < statisticList.Count; i++)
        {
            if (statisticList[i].Item2 > TimeSpan.Zero)
                text += $"{statisticList[i].Item1.name}: {statisticList[i].Item2.Value.ToString(@"hh\:mm\:ss")}\n";
        }
        return text;
    }

    public async Task<bool> Start(long chatId, int activityId, Activity act)
    {
        if (act.statusId == 2) return false;
        var result = api.TrackingAsync(chatId, activityId, true);
        if (result != null)
            return true;
        return false;
    }

    public async Task<string> Stop(long chatId, int activityId, Activity act)
    {
        string text = "";
        if (act.statusId == 1)
        {
            await Console.Out.WriteLineAsync($"{chatId}: Активность уже остановленна");
        }
        User.SetState(chatId, User.State.Deleting);
        var periods = await api.TrackingAsync(chatId, activityId, false);
        foreach (var period in periods)
        {
            if (period is not null)
                text += $"🏁 {act.name}:\n" +
                    $"{period.startTime} - {period.stopTime} \n⏱ Затрачено: {period.totalTime?.ToString(@"hh\:mm\:ss")}\n";
        }
        User.ResetState(chatId);

        return text;
    }
}

