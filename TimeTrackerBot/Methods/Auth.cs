using TimeTrackerBot.ApiServices;

namespace TimeTrackerBot.Methods
{
    public class Auth
    {
        private readonly AuthService AuthApi = new();

        public async Task Register(long chatId, string username)
        {
            await AuthApi.Register(chatId, username);
        }

        public async Task Login(long chatId, string username)
        {
            await AuthApi.Login(chatId, username);
        }
    }
}
