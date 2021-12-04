using System;
using System.Collections.Generic;
using TwitchLib.Client.Models;

namespace TwitchBot {

    abstract class Command {
        public TimeSpan? Cooldown { get; init; } = null;
        public bool IsTimer { get; set; } = false;
        public IAuthenticator Authenticator { get; init; } = Authenticators.Pleb;
        public ITrigger Trigger { get; init; } = null;
        public bool ModOverridesCooldown { get; init; } = true;
        public DateTime LastInvoked { get; private set; } = DateTime.MinValue;
        private bool CooldownAllowsInvocation => !Cooldown.HasValue || DateTime.Now >= LastInvoked + Cooldown;

        private bool IsCooldownOverriden(ChatMessage message) {
            return message.IsBroadcaster || (message.IsModerator && ModOverridesCooldown);
        }

        public void Invoke(Bot bot, ChatMessage message = null) {
            if (message is null) {
                LastInvoked = DateTime.Now;
                InvokeImplementation(bot);
                return;
            }
            if (message.IsMe) {
                bot.SendMessage("Server commands are blocked.");
                return;
            }
            if (!Authenticator.Authenticate(message)) {
                bot.SendMessage($"@{message.Username} Ah, ah, ah! Du hast das Zauberwort nicht gesagt! Ah, ah, ah!");
                return;
            }
            if (IsCooldownOverriden(message)) {
                LastInvoked = DateTime.Now;
                InvokeImplementation(bot, message);
                return;
            }
            if (CooldownAllowsInvocation) {
                LastInvoked = DateTime.Now;
                InvokeImplementation(bot, message);
            } else {
                Console.WriteLine("Cannot invoke command since cooldown stuff");
            }
        }

        public static string ApplyPlaceholders(string response, ChatMessage message) {
            var responseParts = response.Split(' ');
            var originalMessageParts = message.Message.Split(' ');
            Dictionary<string, string> placeholders = new() {
                { "$user", message.Username }
            };
            for (int i = 0; i < responseParts.Length; i++) {
                if (responseParts[i].StartsWith("$") &&
                    uint.TryParse(responseParts[i].Substring(1), out var index)) {
                    if (index > originalMessageParts.Length - 1) {
                        continue;
                    }
                    responseParts[i] = originalMessageParts[index];
                } else if (placeholders.ContainsKey(responseParts[i])) {
                    responseParts[i] = placeholders[responseParts[i]];
                }
            }
            return string.Join(" ", responseParts);
        }

        protected abstract void InvokeImplementation(Bot bot);
        protected abstract void InvokeImplementation(Bot bot, ChatMessage message);
    }
}
