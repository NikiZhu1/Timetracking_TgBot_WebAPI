using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using TimeTrackerBot.Methods;

namespace TimeTrackerBot.ApiServices
{
    public class UserService
    {
        private readonly ApiClient apiClient = new();

        /// <summary>
        /// Получение всех пользователей
        /// </summary>
        public async Task<List<User>?> GetUsers(long chatId)
        {
            var response = await apiClient.HttpClient.GetAsync($"{apiClient.BaseUrl}/Users");
            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<User>>(jsonString);
            return result;
        }

        /// <summary>
        /// Получение userId по chatId
        /// </summary>
        /// <param name="chatId">id пользователя телеграма</param>
        public async Task<User?> GetUserByChatId(long chatId)
        {
            var response = await apiClient.HttpClient.GetAsync($"{apiClient.BaseUrl}/Users/by-chatId/{chatId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<User>(jsonString);
            return result;
        }

        /// <summary>
        /// Получение информации пользователя по id 
        /// </summary>
        /// <param name="userId">id пользователя</param>
        public async Task<User?> GetUserById(int userId)
        {
            var response = await apiClient.HttpClient.GetAsync($"{apiClient.BaseUrl}/Users/{userId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<User>(jsonString);
            return result;
        }

        public async Task<HttpResponseMessage> DeleteAccountAsync(long chatId, int userId)
        {
            var token = Token.GetToken(chatId);
            apiClient.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await apiClient.HttpClient.DeleteAsync($"{apiClient.BaseUrl}/Users/{userId}");

            return response;
        }
    }
}
