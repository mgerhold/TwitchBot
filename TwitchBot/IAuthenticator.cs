using TwitchLib.Client.Models;

namespace TwitchBot
{
    internal interface IAuthenticator {
        public abstract bool Authenticate(ChatMessage message);

        public static IAuthenticator operator |(IAuthenticator lhs, IAuthenticator rhs)
        {
            return new CombinedAuthenticator(lhs, rhs, (firstAuthenticator, secondAuthenticator, chatMessage) =>
            {
                return firstAuthenticator.Authenticate(chatMessage) || secondAuthenticator.Authenticate(chatMessage);
            });
        }

        public static IAuthenticator operator &(IAuthenticator lhs, IAuthenticator rhs)
        {
            return new CombinedAuthenticator(lhs, rhs, (firstAuthenticator, secondAuthenticator, chatMessage) =>
            {
                return firstAuthenticator.Authenticate(chatMessage) && secondAuthenticator.Authenticate(chatMessage);
            });
        }
    }
}
