using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace TwitchBot {

    abstract class Command {
        public TimeSpan? Cooldown { get; init; } = null;
        public bool IsTimer { get; set; } = false;
        public IAuthenticator Authenticator { get; init; } = Authenticators.Pleb;
        public ITrigger Trigger { get; init; } = null;
        public bool ModOverridesCooldown { get; init; } = true;
        public DateTime LastInvoked { get; set; } = DateTime.MinValue;
        private bool CooldownAllowsInvocation => !Cooldown.HasValue || DateTime.Now >= LastInvoked + Cooldown;

        private static readonly string parameterPattern = @"\$[0-9]+";

        private bool IsCooldownOverriden(ChatMessage message) {
            return message.IsBroadcaster || (message.IsModerator && ModOverridesCooldown);
        }

        public void Invoke(Bot bot, ChatMessage message = null) {
            if (message is null) {
                LastInvoked = DateTime.Now;
                InvokeImplementation(bot);
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
            Dictionary<string, string> placeholders = new() {
                { "$user", message.Username },
                // TODO: { "$subcount", }
            };
            var responseString = response;
            foreach (var (placeholder, replacement) in placeholders) {                
                responseString = responseString.Replace(placeholder, replacement);
            }
            var originalMessageParts = message.Message.Split(' ');
            var evaluator = new MatchEvaluator(match => {
                if (uint.TryParse(match.Value.Substring(1), out var index) &&
                    index <= originalMessageParts.Length - 1) {
                    var replacement = originalMessageParts[index];
                    if (replacement.StartsWith('@')) {
                        return replacement.Substring(1);
                    }
                    return originalMessageParts[index];
                }                
                return match.Value;
            });
            responseString = Regex.Replace(responseString, parameterPattern, evaluator);
            // only /me is allowed at the start of the message
            if ((responseString.StartsWith('/') || responseString.StartsWith('.'))
                && !responseString.StartsWith("/me")) {
                responseString = $"@{message.Username} Du kannst mich nicht manipulieren, du Hund!";
            }
            return responseString;
        }

        protected abstract void InvokeImplementation(Bot bot);
        protected abstract void InvokeImplementation(Bot bot, ChatMessage message);
    }
}
