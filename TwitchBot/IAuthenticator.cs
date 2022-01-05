using TwitchLib.Client.Models;

namespace TwitchBot {
    internal interface IAuthenticator {
        public abstract bool Authenticate(ChatMessage message);
    }
}
