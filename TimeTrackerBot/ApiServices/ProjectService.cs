using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TimeTrackerBot.Methods;

namespace TimeTrackerBot.ApiServices
{
    public class ProjectService
    {
        private readonly HttpClient httpClient = new();

        /// <summary>
        /// Создать проект
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="userId">id создателя</param>
        /// <param name="name">Название проекта</param>
        public async Task CreateProject(long chatId, int userId, string name)
        {
            var token = Token.GetToken(chatId);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                projectName = name
            };

            var jsonData = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("http://localhost:8080/api/Projects", content);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Получение списка проектов
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="current">список текущих проектов или завершённых</param>
        public async Task<List<Project>?> GetProjects(long chatId, bool current)
        {
            var token = Token.GetToken(chatId);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var url = $"http://localhost:8080/api/Projects?current={current}";
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Не удалось получить список проектов");
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new DateTimeConverter());
            var projects = await response.Content.ReadFromJsonAsync<List<Project>>(options);
            return projects ?? new();
        }

        /// <summary>
        /// Получение проекта по id
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="projectId">id проекта</param>
        public async Task<Project> GetProjectById(long chatId, int projectId)
        {
            var token = Token.GetToken(chatId);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"http://localhost:8080/api/Projects/{projectId}";
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Не удалось получить проект");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            options.Converters.Add(new DateTimeConverter());
            var project = await response.Content.ReadFromJsonAsync<Project>(options);
            return project ?? new();
        }

        /// <summary>
        /// Получение проектов, в которых состоит пользователь
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="userId">id пользователя</param>
        public async Task<List<ProjectUser>?> GetUserProjectsAsync(long chatId, int userId)
        {
            var token = Token.GetToken(chatId);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"http://localhost:8080/api/Users/{userId}/projects";
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Не удалось получить список проектов пользователя");
            var jsonString = await response.Content.ReadAsStringAsync();
            var userProjects = JsonSerializer.Deserialize<List<ProjectUser>>(jsonString);
            return userProjects ?? new();
        }

        /// <summary>
        /// Получение участников проекта
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="projectId">id проекта</param>
        public async Task<List<ProjectUser>> GetProjectParticipants(long chatId, int projectId)
        {
            var token = Token.GetToken(chatId);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var url = $"http://localhost:8080/api/Projects/{projectId}/users";
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Не удалось получить список участников проекта");
            var jsonString = await response.Content.ReadAsStringAsync();
            var projectUsers = JsonSerializer.Deserialize<List<ProjectUser>>(jsonString);
            return projectUsers;
        }

        /// <summary>
        /// Получение активностей проекта
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="projectId">id проекта</param>
        public async Task<List<ProjectActivity>?> GetProjectActivities(long chatId, int projectId)
        {
            var token = Token.GetToken(chatId);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var url = $"http://localhost:8080/api/Projects/{projectId}/activities";
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Не удалось получить список активностей проекта");
            var jsonString = await response.Content.ReadAsStringAsync();
            var projectActivities = JsonSerializer.Deserialize<List<ProjectActivity>>(jsonString);
            return projectActivities ?? new();
        }

        /// <summary>
        /// Добавить активность в проект
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="activityId">id активности</param>
        /// <param name="projectId">id проекта</param>
        public async Task<ProjectActivity> AddActivityInProject(long chatId, int activityId, int projectId)
        {
            var token = Token.GetToken(chatId);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var payload = new
            {
                activityId,
                projectId
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("http://localhost:8080/api/Projects/activity", content);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var projectActivity = JsonSerializer.Deserialize<ProjectActivity>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return projectActivity;
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                return null;
            }
        }

        /// <summary>
        /// Добавить пользователя в проект
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="userId">id пользователя</param>
        /// <param name="projectId">id проекта</param>
        public async Task<ProjectUser> AddUserInProject(long chatId, int userId, int projectId)
        {
            var token = Token.GetToken(chatId);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var payload = new
            {
                userId,
                projectId
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("http://localhost:8080/api/Projects/user", content);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var projectUser = JsonSerializer.Deserialize<ProjectUser>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return projectUser;
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                return null;
            }
        }

        /// <summary>
        /// Сменить название проекта
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="projectId">id проекта</param>
        /// <param name="name">Новое название</param>
        public async Task<HttpResponseMessage> ChangeProjectName(long chatId, int projectId, string name)
        {
            var token = Token.GetToken(chatId);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var payload = new
            {
                projectName = name
            };

            var response = await httpClient.PatchAsJsonAsync($"http://localhost:8080/api/Projects/{projectId}", payload);
            return response;
        }

        /// <summary>
        /// Завершить проект
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="projectId">id проекта</param>
        public async Task<HttpResponseMessage> CloseProjectAsync(long chatId, int projectId)
        {
            var token = Token.GetToken(chatId);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var payload = new
            {
                closeProject = true
            };
            var response = await httpClient.PatchAsJsonAsync($"http://localhost:8080/api/Projects/{projectId}", payload);
            return response;
        }

        /// <summary>
        /// Удалить проект навсегда
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="projectId">id проекта</param>
        public async Task<HttpResponseMessage> DeleteProjectAsync(long chatId, int projectId)
        {
            var token = Token.GetToken(chatId);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.DeleteAsync($"http://localhost:8080/api/Projects/{projectId}");
            return response;
        }

        /// <summary>
        /// Подключиться к проекту
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="key">код пдоступа проекта</param>
        public async Task<ProjectUser> ConnectToProjectAsync(long chatId, string key)
        {
            var token = Token.GetToken(chatId);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var payload = new
            {
                accessKey = key
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"http://localhost:8080/api/Users/project", content);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var projectUser = JsonSerializer.Deserialize<ProjectUser>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return projectUser;
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                return null;
            }
        }

        /// <summary>
        /// Удалить пользователя из проекта
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="projectId">id проекта</param>
        /// <param name="userid">id пользователя</param>
        public async Task<HttpResponseMessage> DeleteProjectUserAsync(long chatId, int projectId, int userid)
        {
            var token = Token.GetToken(chatId);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.DeleteAsync($"http://localhost:8080/api/Projects/{projectId}/user/{userid}");
            return response;
        }

        /// <summary>
        /// Удалить активность из проекта
        /// </summary>
        /// <param name="chatId">id пользователя телеграмма</param>
        /// <param name="projectId">id проекта</param>
        /// <param name="activityId">id активности</param>
        public async Task<HttpResponseMessage> DeleteProjectActivityAsync(long chatId, int projectId, int activityId)
        {
            var token = Token.GetToken(chatId);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.DeleteAsync($"http://localhost:8080/api/Projects/{projectId}/activity/{activityId}");
            return response;
        }
    }

    public class ProjectUser
    {
        public int id { get; set; }
        public int userId { get; set; }
        public int projectId { get; set; }
        public bool isCreator { get; set; }
    }

    public class ProjectActivity
    {
        public int id { get; set; }
        public int activityId { get; set; }
        public int projectId { get; set; }
    }
}
