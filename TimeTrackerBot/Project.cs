using System.Net;
using TimeTrackerBot.ApiServices;

namespace TimeTrackerBot
{
    public class Project
    {
        public int projectId { get; set; }
        public string projectName { get; set; }
        public string projectKey { get; set; }
        public DateTime? creationDate { get; set; }
        public DateTime? finishDate { get; set; }

        private readonly ProjectService projectApi = new();
        private readonly UserService userApi = new();
        private readonly ActivityService activityApi = new();

        public async Task CreateProject(long chatId, int userId, string name)
        {
            await projectApi.CreateProject(chatId, userId, name);
        }

        public async Task<Project> GetProjectById(long chatId, int projectId)
        {
            var project = await projectApi.GetProjectById(chatId, projectId);
            return project;
        }

        // проекты пользователя
        public async Task<List<Project>?> GetProjectsByUserId(long chatId, int userId, bool current)
        {
            var projects = await projectApi.GetProjects(chatId, current);
            var userProjects = await projectApi.GetUserProjectsAsync(chatId, userId);
            if (userProjects.Count != 0)
            {
                var result = new List<Project>();
                foreach (var project in projects)
                {
                    foreach (var pr in userProjects)
                    {
                        if (pr.projectId == project.projectId)
                            result.Add(project);
                    }
                }
                return result;
            }
            else { return new(); }
        }

        // участники проекта
        public async Task<List<User>> GetProjectUsers(long chatId, int projectid)
        {
            List<ProjectUser> participants = await GetProjectParticipants(chatId, projectid);

            List<User> users = new List<User>();
            foreach (var participant in participants)
            {
                var user = await userApi.GetUserById(participant.userId);
                users.Add(user);
            }
            return users;
        }

        public async Task<List<ProjectUser>> GetProjectParticipants(long chatId, int projectId)
        {
            var participants = await projectApi.GetProjectParticipants(chatId, projectId);
            return participants;
        }

        public async Task<User?> GetCreator(long chatId, int projectId)
        {
            var participants = await projectApi.GetProjectParticipants(chatId, projectId);
            User creator = null;
            foreach (var participant in participants)
            {
                if (participant.isCreator)
                    creator = await userApi.GetUserById(participant.userId);
            }
            return creator;
        }

        // активности проекта
        public async Task<List<Activity>?> GetProjectActivities(long chatId, int projectid)
        {
            var projectActs = await projectApi.GetProjectActivities(chatId, projectid);
            User creator = await GetCreator(chatId, projectid);
            var activities = await activityApi.GetActivitiesAsync(chatId, creator.id, true, true, false);
            var result = new List<Activity>();
            foreach (var activity in activities)
            {
                foreach (var act in projectActs)
                {
                    if (act.activityId == activity.id)
                        result.Add(activity);
                }
            }
            return result ?? new();
        }

        // Добавить активность в проект
        public async Task<string> AddActivityInProject(long chatId, int projectId, int userId, string activityName)
        {
            var activities = await activityApi.GetActivitiesAsync(chatId, userId, true, true, false);
            var act = activities.FirstOrDefault(a => a.name == activityName);
            if (act == null)
            {
                act = await activityApi.CreateActivity(chatId, userId, activityName);
            }
            ProjectActivity projectActivity = await projectApi.AddActivityInProject(chatId, act.id, projectId);
            if (projectActivity != null)
            {
                return "☑️ Активность добавлена в проект";
            }
            else return "❗ Не удалось добавить активность в проект";
        }

        // Добавить пользователя в проект
        public async Task<string> AddUserInProject(long chatId, int projectId, string username)
        {
            var users = await userApi.GetUsers(chatId);
            User user = users.FirstOrDefault(a => a.name == username);
            if (user == null)
                return $"Пользователь с именем {username} не найден";

            ProjectUser newParticipant = await projectApi.AddUserInProject(chatId, user.id, projectId);
            if (newParticipant != null)
            {
                return "☑️ Пользователь добавлен в проект";
            }
            else return "❗ Не удалось добавить пользователя в проект";
        }

        public async Task<string> Connect(long chatId, string key)
        {
            var projects = await projectApi.GetProjects(chatId, true);
            var project = projects.FirstOrDefault(a => a.projectKey == key);
            if (project == null)
                return $"Проект с ключом доступа {key} не найден";
            ProjectUser newParticipant = await projectApi.ConnectToProjectAsync(chatId, key);
            if (newParticipant != null)
            {
                return $"☑️ Вы добавлены в проект {project.projectName}";
            }
            else return "❗ Не удалось добавить пользователя в проект";
        }


        public async Task<string> ChangeProjectName(long chatId, int projectId, string name)
        {
            var result = await projectApi.ChangeProjectName(chatId, projectId, name);
            if (result.IsSuccessStatusCode)
            {
                return "✅ Проект переименован";
            }
            var error = await result.Content.ReadAsStringAsync();
            return result.StatusCode switch
            {
                HttpStatusCode.BadRequest => $"⚠️ Ошибка: {error}",
                HttpStatusCode.NotFound => "❌ Проект не найден.",
                HttpStatusCode.InternalServerError => "🚨 Ошибка сервера при обновлении.",
                _ => $"❌ Неизвестная ошибка: {result.StatusCode}"
            };
        }

        public async Task<string> CloseProject(long chatId, int projectId)
        {
            var result = await projectApi.CloseProjectAsync(chatId, projectId);
            if (result.IsSuccessStatusCode)
            {
                return "✅ Проект закрыт";
            }
            var error = await result.Content.ReadAsStringAsync();
            return result.StatusCode switch
            {
                HttpStatusCode.BadRequest => $"⚠️ Ошибка: {error}",
                HttpStatusCode.NotFound => "❌ Проект не найден.",
                HttpStatusCode.InternalServerError => "🚨 Ошибка сервера при обновлении.",
                _ => $"❌ Неизвестная ошибка: {result.StatusCode}"
            };
        }

        public async Task<string> DeleteProject(long chatId, int projectId)
        {
            var result = await projectApi.DeleteProjectAsync(chatId, projectId);
            return result.StatusCode switch
            {
                HttpStatusCode.NoContent => "✅ Проект успешно удален.",
                HttpStatusCode.NotFound => $"❌ Проект с ID {projectId} не найден.",
                HttpStatusCode.Unauthorized => "🔒 Вы не авторизованы для выполнения этого действия.",
                HttpStatusCode.InternalServerError => "🚨 Ошибка сервера при удалении.",
                _ => $"⚠️ Неизвестная ошибка: {result.StatusCode}"
            };
        }

        public async Task<string> DeleteProjectUser(long chatId, int projectId, int userid)
        {
            var result = await projectApi.DeleteProjectUserAsync(chatId, projectId, userid);
            return result.StatusCode switch
            {
                HttpStatusCode.NoContent => "✅ Пользователь проекта успешно удален.",
                HttpStatusCode.NotFound => $"❌ Проект с ID {projectId} не найден.",
                HttpStatusCode.Unauthorized => "🔒 Вы не авторизованы для выполнения этого действия.",
                HttpStatusCode.InternalServerError => "🚨 Ошибка сервера при удалении.",
                _ => $"⚠️ Неизвестная ошибка: {result.StatusCode}"
            };
        }

        public async Task<string> DeleteProjectActivity(long chatId, int projectId, int activityId)
        {
            var result = await projectApi.DeleteProjectActivityAsync(chatId, projectId, activityId);
            return result.StatusCode switch
            {
                HttpStatusCode.NoContent => "✅ Активность удалена из проекта.",
                HttpStatusCode.NotFound => $"❌ Проект с ID {projectId} не найден.",
                HttpStatusCode.Unauthorized => "🔒 Вы не авторизованы для выполнения этого действия.",
                HttpStatusCode.InternalServerError => "🚨 Ошибка сервера при удалении.",
                _ => $"⚠️ Неизвестная ошибка: {result.StatusCode}"
            };
        }
    }
}
