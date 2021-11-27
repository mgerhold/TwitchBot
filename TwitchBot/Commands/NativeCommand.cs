using System;
using TwitchLib.Client.Models;

namespace TwitchBot.Commands {

    class NativeCommand : Command {
        public Action<Bot, ChatMessage> Handler { private get; init; }

        protected override void InvokeImplementation(Bot bot, ChatMessage message) {
            Handler(bot, message);
        }

        protected override void InvokeImplementation(Bot bot) {
            Handler(bot, null);
        }
    }

}
