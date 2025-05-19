using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TimeTrackerBot.ApiServices
{
    public class AuthService
    {
        private readonly ApiClient apiClient = new();

        public class TokenResponse
        {
            public string Token { get; set; }
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="username">логин</param>
        public async Task Register(long chatId, string username)
        {
            var existingToken = Token.GetToken(chatId);
            if (!string.IsNullOrEmpty(existingToken) && !IsTokenExpired(existingToken))
                return;

            var payload = new
            {
                name = username + "(tg)",
                password = "string",
                chatId = chatId
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await apiClient.HttpClient.PostAsync($"{apiClient.BaseUrl}/Users", content);

            if (!response.IsSuccessStatusCode)
                throw new Exception($" Ошибка при получении токена: {response.RequestMessage}");
            
            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TokenResponse>(jsonString);
            string token = result.Token;
            Token.SaveToken(chatId, token);
            apiClient.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Авторизация пользователя
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="username">логин</param>
        public async Task Login(long chatId, string username)
        {
            var existingToken = Token.GetToken(chatId);

            if (!string.IsNullOrEmpty(existingToken) && !IsTokenExpired(existingToken))
                return;

            var payload = new
            {
                name = username + "(tg)",
                password = "string",
                chatId = chatId
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await apiClient.HttpClient.PostAsync($"{apiClient.BaseUrl}/Auth/login", content);
            if (!response.IsSuccessStatusCode)
                throw new Exception($" Ошибка при получении токена: {response.RequestMessage}");
            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TokenResponse>(jsonString);
            string token = result.Token;
            Token.SaveToken(chatId, token);
            apiClient.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Проверка действия токена
        /// </summary>
        /// <param name="token">токен</param>
        /// <returns></returns>
        private bool IsTokenExpired(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var exp = jwt.ValidTo;
            return exp < DateTime.UtcNow;
        }
    }
}
