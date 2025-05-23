using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;
using TimeTrackerBot.Methods;

namespace TimeTrackerBot
{
    public class InlineKeyboard
    {
        //Главная клавиатура со списком активностей
        public static InlineKeyboardMarkup Main(List<Activity> activityList)
        {
            List<InlineKeyboardButton[]> rows = new()
            {
                new[] {InlineKeyboardButton.WithCallbackData("➕Добавить активность", "add_activity")}
            };

            foreach (Activity activity in activityList)
            {
                InlineKeyboardButton activityButton = new("");
                InlineKeyboardButton statusButton = new("");

                if (activity.statusId != 3)
                {
                    // Создаем кнопки для активности
                    activityButton = activity.statusId == 2
                        ? InlineKeyboardButton.WithCallbackData($"⏱️ {activity.name}", $"aboutAct{activity.id}")
                        : InlineKeyboardButton.WithCallbackData($"{activity.name}", $"aboutAct{activity.id}");
                    statusButton = activity.statusId == 2
                        ? InlineKeyboardButton.WithCallbackData("⏹ СТОП", $"stop_{activity.id}")
                        : InlineKeyboardButton.WithCallbackData("❇️ СТАРТ", $"start_{activity.id}");
                }

                rows.Add(new[] { activityButton, statusButton });
            }

            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("Статистика активностей", "show_statistic") });

            return new InlineKeyboardMarkup(rows);
        }

        //Клавиатура в AboutAct
        public static InlineKeyboardMarkup ChangeActivity(int activityId)
        {
            InlineKeyboardMarkup changeActKeyboard = new(
            new List<InlineKeyboardButton[]>()
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("✏️ Изменить", $"rename{activityId}"), InlineKeyboardButton.WithCallbackData("🗑 Удалить", $"delete{activityId}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("🗂 Отправить в архив", $"archive{activityId}"),
                },
            });

            return changeActKeyboard;
        }

        //Клавиатура с архивированными активностями
        public static InlineKeyboardMarkup Archive(List<Activity> archivedActivity)
        {
            List<InlineKeyboardButton[]> rows = new();

            foreach (Activity activity in archivedActivity)
            {
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{activity.name}", $"aboutArchive{activity.id}") });
            }

            return new InlineKeyboardMarkup(rows);
        }

        //Помощь
        public static InlineKeyboardMarkup Help()
        {
            InlineKeyboardMarkup technicalSupportKeyboard = new(
            new List<InlineKeyboardButton[]>()
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithUrl("Техническая поддержка", "https://forms.gle/p87wy2ETYGC7WDMdA"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithUrl("Сотрудничество", "https://forms.gle/9W8C3epktot9inR66"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithUrl("🌐 Веб-приложение", "https://crow.ommat.ru/"),
                }
            }
            );

            return technicalSupportKeyboard;
        }

        //Активности в архиве
        public static InlineKeyboardMarkup ChangeArchive(int activityId)
        {
            var changeActKeyboard = new InlineKeyboardMarkup(
            new List<InlineKeyboardButton[]>()
            {
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("📤 Восстановить", $"recover{activityId}"), InlineKeyboardButton.WithCallbackData("🗑 Удалить", $"deleteInArchive{activityId}"),
                },
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("◀️ Назад в архив", "backToArchive"),
                },
            });

            return changeActKeyboard;
        }

        //Клавиатора с выбором типа статистики
        public static InlineKeyboardMarkup StaticticType()
        {
            var statisticKeyboard = new InlineKeyboardMarkup(
            new List<InlineKeyboardButton[]>()
            {
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("За всё время", $"statistic_1"),
                },
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("За опредленный период", $"statistic_2"),
                },
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("За определенный день", $"statistic_3"),
                },
            });

            return statisticKeyboard;
        }

        //Меню проектов
        public static InlineKeyboardMarkup ProjectKB(List<Project> projectList, bool current)
        {
            List<InlineKeyboardButton[]> rows = new();
            if (current)
            {
                InlineKeyboardButton addProject = InlineKeyboardButton.WithCallbackData("➕ Создать", "add_project");
                InlineKeyboardButton joinToProject = InlineKeyboardButton.WithCallbackData("🗝 Подключиться", $"conectTo");

                rows.Add([addProject, joinToProject]);
            }

            List<Project> myProjects = projectList.Where(project => project.UserIsCreator).ToList();
            List<Project> notMyProjects = projectList.Where(project => !project.UserIsCreator).ToList();

            foreach (Project project in myProjects)
            {
                if (current)
                    rows.Add([InlineKeyboardButton.WithCallbackData($"✳️ {project.projectName}", $"creatorProjectInfo{project.projectId}")]);
                else
                    rows.Add([InlineKeyboardButton.WithCallbackData($"✳️ {project.projectName}", $"creatorClosedProjectInfo{project.projectId}")]);
            }

            foreach (Project project in notMyProjects)
            {
                if (current)
                    rows.Add([InlineKeyboardButton.WithCallbackData($"👥 {project.projectName}", $"projectInfo{project.projectId}")]);
                else
                    rows.Add([InlineKeyboardButton.WithCallbackData($"👥 {project.projectName}", $"closedProjectInfo{project.projectId}")]);
            }

            if (current)
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔒 Завершенные проекты", "closedProjects") });

            return new InlineKeyboardMarkup(rows);
        }

        //О проекте (мой)
        public static InlineKeyboardMarkup CreatorProjectInfo(int projectId)
        {
            List<InlineKeyboardButton[]> rows = new()
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Активности проекта", $"projectActivities{projectId}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("⚙ Управление", $"updateProject{projectId}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("❌ Завершить ", $"close{projectId}"),
                    InlineKeyboardButton.WithCallbackData("🗑 Удалить", $"deleteProject{projectId}")
                }
            };

            InlineKeyboardMarkup projectInfoKeyboard = new(rows);

            return projectInfoKeyboard;
        }

        //О проекте (не мой)
        public static InlineKeyboardMarkup ProjectInfo(int projectId)
        {
            List<InlineKeyboardButton[]> rows = new()
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Активности проекта", $"projectActivities{projectId}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("❌ Покинуть", $"leaveProject{projectId}"),
                }
            };

            InlineKeyboardMarkup projectInfoKeyboard = new(rows);

            return projectInfoKeyboard;
        }

        //Завершённые проекты
        public static InlineKeyboardMarkup ClosedProjectInfo()
        {
            InlineKeyboardMarkup closedProjectInfoKeyboard = new(
            new List<InlineKeyboardButton[]>()
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Статистика проекта", $"show_statistic"),
                }
            });

            return closedProjectInfoKeyboard;
        }

        //Активности в проекте
        public static InlineKeyboardMarkup ProjectActivitiesKB(List<Activity> projectActivities)
        {
            List<InlineKeyboardButton[]> rows = new();
            foreach (Activity activity in projectActivities)
            {
                InlineKeyboardButton activityButton = new("");
                InlineKeyboardButton statusButton = new("");

                if (activity.statusId != 3)
                {
                    // Создаем кнопки для активности
                    activityButton = activity.statusId == 2
                        ? InlineKeyboardButton.WithCallbackData($"⏱️ {activity.name}", $"aboutAct{activity.id}")
                        : InlineKeyboardButton.WithCallbackData($"{activity.name}", $"aboutAct{activity.id}");
                    statusButton = activity.statusId == 2
                        ? InlineKeyboardButton.WithCallbackData("⏹ СТОП", $"stop_{activity.id}")
                        : InlineKeyboardButton.WithCallbackData("❇️ СТАРТ", $"start_{activity.id}");
                }

                rows.Add([activityButton, statusButton]);
            }

            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("Статистика проекта", "show_statistic") });

            return new InlineKeyboardMarkup(rows);
        }

        //Изменение проекта
        public static InlineKeyboardMarkup ChangeProjectKB(int projectId)
        {
            InlineKeyboardMarkup changeProjectKeyboard = new(
            new List<InlineKeyboardButton[]>()
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("✏️ Изменить название", $"renameProject{projectId}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("👤 Добавить участника", $"addUserInProject{projectId}"),
                    InlineKeyboardButton.WithCallbackData("🚷 Удалить участника", $"removeUser{projectId}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Добавить активность", $"addActivityInProject{projectId}"),
                    InlineKeyboardButton.WithCallbackData("🚳 Удалить активность", $"removeActivity{projectId}"),
                },
            });

            return changeProjectKeyboard;
        }

        //Удаление участников из проекта
        public static InlineKeyboardMarkup DeletingUsersKB(List<User> users)
        {
            List<InlineKeyboardButton[]> rows = new();

            foreach (User user in users)
            {
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{user.name}", $"deleteUser{user.id}") });
            }
            return new InlineKeyboardMarkup(rows);
        }

        //Удаление активностей из проекта
        public static InlineKeyboardMarkup DeletingActivitiesKB(List<Activity> activities)
        {
            List<InlineKeyboardButton[]> rows = new();

            foreach (Activity act in activities)
            {
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{act.name}", $"deleteActivity{act.id}") });
            }
            return new InlineKeyboardMarkup(rows);
        }

        //Словарь, в котором хранятся состояния для удаления
        private static readonly Dictionary<long, int> messageIdsForDelete = new();

        //Записать message.id для удаления
        public static void SetMessageIdForDelete(long userId, int messageId)
        {
            messageIdsForDelete[userId] = messageId;
        }

        //Получить message.id для удаления
        public static int GetMessageIdForDelete(long userId)
        {
            if (messageIdsForDelete.TryGetValue(userId, out var messageId))
            {
                return messageId;
            }
            return 0;
        }

        //Удалить message.id после удаленния
        public static void RemoveMessageId(long userId)
        {
            messageIdsForDelete.Remove(userId);
        }
    }
}
