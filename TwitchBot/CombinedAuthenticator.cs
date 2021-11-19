using System;
using TwitchLib.Client.Models;

namespace TwitchBot
{
    internal class CombinedAuthenticator : IAuthenticator
    {
        private IAuthenticator lhs;
        private IAuthenticator rhs;
        readonly Func<IAuthenticator, IAuthenticator, ChatMessage, bool> callback;

        public CombinedAuthenticator(IAuthenticator lhs, IAuthenticator rhs, Func<IAuthenticator, IAuthenticator, ChatMessage, bool> callback)
        {
            this.lhs = lhs;
            this.rhs = rhs;
            this.callback = callback;
        }

        public bool Authenticate(ChatMessage message)
        {
            return callback(lhs, rhs, message);
        }
    }
}