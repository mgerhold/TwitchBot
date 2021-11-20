using TwitchLib.Client.Models;

namespace TwitchBot
{
    internal interface IAuthenticator {
        public abstract bool Authenticate(ChatMessage message);

        public static IAuthenticator operator |(IAuthenticator lhs, IAuthenticator rhs)
        {
            return new CombinedAuthenticator(lhs, rhs, (l, r, cm) =>
            {
                return l.Authenticate(cm) || r.Authenticate(cm);
            });
        }

        public static IAuthenticator operator &(IAuthenticator lhs, IAuthenticator rhs)
        {
            return new CombinedAuthenticator(lhs, rhs, (l, r, cm) =>
            {
                return l.Authenticate(cm) && r.Authenticate(cm);
            });
        }
    }
}
