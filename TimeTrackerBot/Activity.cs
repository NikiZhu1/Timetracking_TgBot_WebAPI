using System.Net;
using TimeTrackerBot.ApiServices;

namespace TimeTrackerBot
{
    public class Activity
    {
        public int id { get; set; }
        public string name { get; set; }
        public DateTime? activeFrom { get; set; }
        public int userId { get; set; }
        public int statusId { get; set; }

        private readonly ActivityService api = new();

        public async Task<List<Activity>> GetActivities(long chatId, int userId, bool? onlyActive = null, bool? onlyInProcess = null, bool? onlyArchived = null)
        {
            try
            {
                List<Activity> activities = await api.GetActivitiesAsync(chatId, userId, onlyActive, onlyInProcess, onlyArchived);
                if (activities == null)
                    return null;
                return activities;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении активностей: {ex.Message}");
            }
        }

        public async Task<Activity> GetActivityById(long chatId, int activityId)
        {
            Activity activity = await api.GetActivityById(chatId, activityId);
            return activity;
        }

        public async Task<Activity> CreateActivity(long chatId, int userId, string name)
        {
            Activity activity = await api.CreateActivity(chatId, userId, name);
            if (activity == null) return null;
            return activity;
        }

        public async Task<string> UpdateActivityName(long chatId, int activityId, string newname)
        {
            var result = await api.UpdateActivityNameAsync(chatId, activityId, newname);
            var error = await result.Content.ReadAsStringAsync();
            return result.StatusCode switch
            {
                HttpStatusCode.OK => "Активность переименована",
                HttpStatusCode.BadRequest => $"⚠️ Ошибка: {error}",
                HttpStatusCode.NotFound => "❌ Активность не найдена.",
                HttpStatusCode.InternalServerError => "🚨 Ошибка сервера при обновлении.",
                _ => $"❌ Неизвестная ошибка: {result.StatusCode}"
            };
        }

        public async Task<string> ChangeActivityStatus(long chatId, int activityId, bool archive)
        {
            var result = await api.ChangeActivityStatus(chatId, activityId, archive);
            var error = await result.Content.ReadAsStringAsync();
            return result.StatusCode switch
            {
                HttpStatusCode.OK => "Активность отправлена в архив\nПоказать архив — /archive",
                HttpStatusCode.BadRequest => $"⚠️ Ошибка: {error}",
                HttpStatusCode.NotFound => "❌ Активность не найдена.",
                HttpStatusCode.InternalServerError => "🚨 Ошибка сервера при обновлении.",
                _ => $"❌ Неизвестная ошибка: {result.StatusCode}"
            };
        }

        public async Task<string> DeleteActivity(long chatId, int activityId)
        {
            var result = await api.DeleteActivityAsync(chatId, activityId);

            return result.StatusCode switch
            {
                HttpStatusCode.NoContent => "✅ Активность успешно удалена.",
                HttpStatusCode.NotFound => $"❌ Активность с ID {activityId} не найдена.",
                HttpStatusCode.Unauthorized => "🔒 Вы не авторизованы для выполнения этого действия.",
                HttpStatusCode.InternalServerError => "🚨 Ошибка сервера при удалении.",
                _ => $"⚠️ Неизвестная ошибка: {result.StatusCode}"
            };
        }
    }
}
