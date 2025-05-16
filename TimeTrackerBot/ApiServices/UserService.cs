using System.Net;
using System.Text.Json;
using TimeTrackerBot.Methods;

namespace TimeTrackerBot.ApiServices
{
    public class UserService
    {
        private readonly HttpClient httpClient = new();

        /// <summary>
        /// Получение всех пользователей
        /// </summary>
        public async Task<List<User>?> GetUsers(long chatId)
        {
            var response = await httpClient.GetAsync($"http://localhost:8080/api/Users");
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
            var response = await httpClient.GetAsync($"http://localhost:8080/api/Users/by-chatId/{chatId}");
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
            var response = await httpClient.GetAsync($"http://localhost:8080/api/Users/{userId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<User>(jsonString);
            return result;
        }
    }
}
