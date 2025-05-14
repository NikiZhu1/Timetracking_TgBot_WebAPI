using Telegram.Bot.Types.ReplyMarkups;

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
                }
            }
            );

            return technicalSupportKeyboard;
        }

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


        public static InlineKeyboardMarkup ProjectKB(List<Project> projectList, bool current)
        {
            List<InlineKeyboardButton[]> rows = new();
            if (current)
            {
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("➕Добавить проект", "add_project") });
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🗝 Подключиться к проекту", $"conectTo") });
            }

            foreach (Project project in projectList)
            {
                if (current)
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{project.projectName}", $"projectInfo{project.projectId}") });
                else
                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{project.projectName}", $"closedProjectInfo{project.projectId}") });
            }

            if (current)
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔒 Завершенные проекты", "closedProjects") });

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ProjectInfo(int projectId)
        {
            InlineKeyboardMarkup projectInfoKeyboard = new(
            new List<InlineKeyboardButton[]>()
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Активности проекта", $"projectActivities{projectId}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("✏️ Изменить", $"updateProject{projectId}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("❌ Завершить проект", $"close{projectId}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("🗑 Удалить", $"deleteProject{projectId}")
                },
            });

            return projectInfoKeyboard;
        }

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

                rows.Add(new[] { activityButton, statusButton });
            }

            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("Статистика проекта", "show_statistic") });

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ChangeProjectKB(int projectId)
        {
            InlineKeyboardMarkup changeProjectKeyboard = new(
            new List<InlineKeyboardButton[]>()
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("👤 Добавить участника", $"addUserInProject{projectId}"),
                    InlineKeyboardButton.WithCallbackData("🗑 Удалить участника", $"removeUser{projectId}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Добавить активность", $"addActivityInProject{projectId}"),
                    InlineKeyboardButton.WithCallbackData("🗑 Удалить активность", $"removeActivity{projectId}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("✏️ Изменить название", $"renameProject{projectId}"),
                },
            });

            return changeProjectKeyboard;
        }

        public static InlineKeyboardMarkup DeletingUsersKB(List<User> users)
        {
            List<InlineKeyboardButton[]> rows = new();

            foreach (User user in users)
            {
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{user.name}", $"deleteUser{user.id}") });
            }
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup DeletingActivitiesKB(List<Activity> activities)
        {
            List<InlineKeyboardButton[]> rows = new();

            foreach (Activity act in activities)
            {
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{act.name}", $"deleteActivity{act.id}") });
            }
            return new InlineKeyboardMarkup(rows);
        }

        //Словаь, в котором хранятся состояния для удаления
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
