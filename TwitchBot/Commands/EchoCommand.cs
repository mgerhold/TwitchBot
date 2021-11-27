using TwitchLib.Client.Models;

namespace TwitchBot.Commands {

    class EchoCommand : Command {
        public string Response { get; set; }

        protected override void InvokeImplementation(Bot bot, ChatMessage message) {
            bot.SendMessage(ApplyPlaceholders(Response, message));
        }

        protected override void InvokeImplementation(Bot bot) {
            bot.SendMessage(Response);
        }
    }

}
