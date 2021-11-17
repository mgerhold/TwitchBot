using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace TwitchBot {

    abstract class Command {
        public TimeSpan? Cooldown { get; init; } = null;
        public IAuthenticator Authenticator { get; init; } = Authenticators.Pleb;
        public ITrigger Trigger { get; init; } = null;
        public bool ModOverridesCooldown { get; init; } = true;
        protected DateTime? lastInvoked = null;
        private bool CooldownAllowsInvocation => !Cooldown.HasValue || !lastInvoked.HasValue || DateTime.Now >= lastInvoked + Cooldown;

        private bool IsCooldownOverriden(ChatMessage message) {
            return message.IsBroadcaster || (message.IsModerator && ModOverridesCooldown);
        }

        public void Invoke(Bot bot, ChatMessage message) {
            if (!Authenticator.Authenticate(message)) {
                bot.SendMessage($"@{message.Username} Ah, ah, ah! Du hast das Zauberwort nicht gesagt! Ah, ah, ah!");
                return;
            }
            if (IsCooldownOverriden(message)) {
                InvokeImplementation(bot, message);
                return;
            }
            if (CooldownAllowsInvocation) {
                lastInvoked = DateTime.Now;
                InvokeImplementation(bot, message);
            } else {
                Console.WriteLine("Cannot invoke command since cooldown stuff");
            }
        }

        protected abstract void InvokeImplementation(Bot bot, ChatMessage message);
    }
}
