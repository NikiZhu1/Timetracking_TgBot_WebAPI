using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TimeTrackerBot.Methods;

namespace TimeTrackerBot.ApiServices
{
    public class TrackingService
    {
        private readonly ApiClient apiClient = new();

        /// <summary>
        /// Управление отслеживанием активности
        /// </summary>
        /// <param name="chatId">id пользователя телеграма</param>
        /// <param name="activityId">id активности</param>
        /// <param name="isStart">начать отслеживание или остановить</param>
        public async Task<List<ActivityPeriod>?> TrackingAsync(long chatId, int activityId, bool isStart)
        {
            var token = Token.GetToken(chatId);
            apiClient.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var dto = new
            {
                activityId = activityId,
                isStarted = isStart
            };
            var response = await apiClient.HttpClient.PostAsJsonAsync($"{apiClient.BaseUrl}/ActivityPeriods", dto);
            var jsonString = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new DateTimeConverter());
            var result = JsonSerializer.Deserialize<List<ActivityPeriod>>(jsonString, options);
            return result;
        }

        /// <summary>
        /// Получение статистики
        /// </summary>
        /// <param name="chatId">id пользователя телеграма</param>
        /// <param name="userId">id пользователя</param>
        /// <param name="activityId">id активности</param>
        /// <param name="from">1 дата</param>
        /// <param name="to">2 дата</param>
        public async Task<List<ActivityPeriod>?> GetStatisticsAsync(long chatId, int userId = 0, int activityId = 0, DateTime? from = null, DateTime? to = null)
        {
            var token = Token.GetToken(chatId);
            apiClient.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"{apiClient.BaseUrl}/ActivityPeriods?activityId={activityId}&userId={userId}";
            if (from.HasValue) url += $"&data1={from.Value:yyyy-MM-ddTHH:mm:ss}";
            if (to.HasValue) url += $"&data2={to.Value:yyyy-MM-ddTHH:mm:ss}";

            var response = await apiClient.HttpClient.GetAsync(url);

            var jsonString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Ошибка API: {response.StatusCode}, content: {jsonString}");
            }
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new DateTimeConverter());

            var result = JsonSerializer.Deserialize<List<ActivityPeriod>>(jsonString, options);
            return result;
        }

    }
}
