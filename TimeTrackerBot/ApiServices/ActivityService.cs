using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TimeTrackerBot.Methods;

namespace TimeTrackerBot.ApiServices
{
    public class ActivityService
    {
        private readonly ApiClient apiClient = new();

        /// <summary>
        /// Получение активностей
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="userId">id пользователя</param>
        /// <param name="onlyActive">неотслеживаемые</param>
        /// <param name="onlyInProcess">отслеживаемые</param>
        /// <param name="onlyArchived">в архиве</param>
        public async Task<List<Activity>> GetActivitiesAsync(long chatId, int userId, bool? onlyActive = null, bool? onlyInProcess = null, bool? onlyArchived = null)
        {
            var token = Token.GetToken(chatId);
            apiClient.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"{apiClient.BaseUrl}/Users/{userId}/activities";

            var queryParams = new List<string>();
            if (onlyArchived.HasValue)
                queryParams.Add($"onlyArchived={onlyArchived.Value.ToString().ToLower()}");
            if (onlyInProcess.HasValue)
                queryParams.Add($"onlyInProcess={onlyInProcess.Value.ToString().ToLower()}");
            if (onlyActive.HasValue)
                queryParams.Add($"onlyActive={onlyActive.Value.ToString().ToLower()}");

            if (queryParams.Any())
                url += "?" + string.Join("&", queryParams);

            var response = await apiClient.HttpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Не удалось получить список активностей");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new DateTimeConverter());

            var activities = await response.Content.ReadFromJsonAsync<List<Activity>>(options);
            return activities ?? new();
        }

        /// <summary>
        /// Получение активности по id
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="activityId">id активности</param>
        public async Task<Activity?> GetActivityById(long chatId, int activityId)
        {
            var token = Token.GetToken(chatId);
            apiClient.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await apiClient.HttpClient.GetAsync($"{apiClient.BaseUrl}/Activities/{activityId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            var jsonString = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new DateTimeConverter());
            var result = JsonSerializer.Deserialize<Activity>(jsonString, options);
            return result;
        }

        /// <summary>
        /// Создание активности
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="userId">id пользователя</param>
        /// <param name="name">Название новой активности</param>
        public async Task<Activity> CreateActivity(long chatId, int userId, string name)
        {
            var token = Token.GetToken(chatId);
            apiClient.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                userId = userId,
                activityName = name,
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await apiClient.HttpClient.PostAsync($"{apiClient.BaseUrl}/Activities", content);
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new DateTimeConverter());
            var result = JsonSerializer.Deserialize<Activity>(jsonString, options);
            return result;
        }

        /// <summary>
        /// Обновление названия активности
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="activityId">id активности</param>
        /// <param name="newname">новое имя</param>
        public async Task<HttpResponseMessage> UpdateActivityNameAsync(long chatId, int activityId, string newname)
        {
            var token = Token.GetToken(chatId);
            apiClient.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var payload = new
            {
                newName = newname
            };

            var response = await apiClient.HttpClient.PatchAsJsonAsync($"{apiClient.BaseUrl}/Activities/{activityId}", payload);

            return response;
        }

        /// <summary>
        /// Изменить статус архивности у актинвости
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="activityId">id активности</param>
        /// <param name="archive">поместить ли эту активности в архив</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> ChangeActivityStatus(long chatId, int activityId, bool archive)
        {
            var token = Token.GetToken(chatId);
            apiClient.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var payload = new
            {
                archived = archive
            };

            var response = await apiClient.HttpClient.PatchAsJsonAsync($"{apiClient.BaseUrl}/Activities/{activityId}", payload);
            return response;
        }

        /// <summary>
        /// Удалить активность 
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="activityId">id активности</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> DeleteActivityAsync(long chatId, int activityId)
        {
            var token = Token.GetToken(chatId);
            apiClient.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await apiClient.HttpClient.DeleteAsync($"{apiClient.BaseUrl}/Activities/{activityId}");

            return response;
        }
    }
}
