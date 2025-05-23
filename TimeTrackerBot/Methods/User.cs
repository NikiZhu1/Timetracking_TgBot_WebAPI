﻿using System.Net.Http.Headers;
using TimeTrackerBot.ApiServices;

namespace TimeTrackerBot.Methods
{
    public class User
    {
        public int id { get; set; }
        public long chatId { get; set; }
        public string name { get; set; } = string.Empty;

        private readonly UserService api = new();

        public enum State
        {
            None,
            WaitMessageForChangeAct, // Ожидает сообщения для изменения названия активности
            WaitMessageForAddAct, // Ожидает сообщения для добавления активности
            Deleting,
            WaitingPeriodDates,
            WaitingCertainDate,
            WaitingMessageForAddProject,
            WaitingMessageForAddProjectUser,
            WaitMessageForChangeProject,
            WaitingMessageForConnectToProject,
        }

        //Словаь в котором хранятся состояния пользователей
        private static readonly Dictionary<long, (State state, int? activityid)> userStates = new();

        //Установить состояние
        public static void SetState(long userId, State state, int? activityid = null)
        {
            userStates[userId] = (state, activityid);
        }

        //Получить текущее состояние
        public static (State state, int? activityid) GetState(long userId)
        {
            if (userStates.TryGetValue(userId, out var userInfo))
            {
                return userInfo;
            }

            return (State.None, null);
        }

        //Сбросить состояние
        public static void ResetState(long userId)
        {
            userStates.Remove(userId);
        }

        public enum TrackingState
        {
            ProjectsTracking,
            PersonalTracking
        }

        private static readonly Dictionary<long, (TrackingState trackingState, int? projectId)> trackingStates = new();

        public static void SetTrackingState(long userId, TrackingState state, int? projectId = null)
        {
            trackingStates[userId] = (state, projectId);
        }

        public static (TrackingState trackingState, int? projectId)? GetTrackingState(long userId)
        {
            if (trackingStates.TryGetValue(userId, out var trackingStateInfo))
            {
                return trackingStateInfo;
            }
            return null;
        }

        public static void ResetTrackingState(long userId)
        {
            trackingStates.Remove(userId);
        }

        public async Task<List<User>> GetUsers(long chatId)
        {
            List<User> users = new List<User>();
            users = await api.GetUsers(chatId);
            if (users == null) { throw new Exception(); }
            return users;
        }

        public async Task<User> GetUserByChatId(long chatId)
        {
            User user = await api.GetUserByChatId(chatId);
            if (user == null) { throw new Exception($"User with ChatID {chatId} not found"); }
            else { return user; }
        }

        public async Task<User> GetUserById(long chatId, int userId)
        {
            User user = await api.GetUserById(chatId, userId);
            if (user == null) { throw new Exception($"User with ID {userId} not found"); }
            else { return user; }
        }

        public async Task<HttpResponseMessage> DeleteAccountAsync(long chatId, int userId)
        {
            var response = await api.DeleteAccountAsync(chatId, userId);

            return response;
        }
    }
}
