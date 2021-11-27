using TwitchLib.Client.Models;

namespace TwitchBot.Commands
{
    internal class MyPointsCommand : Command {
        public MyPointsCommand() {
            Trigger = Triggers.StartsWithWord("!points");
            this.Cooldown = System.TimeSpan.FromSeconds(10);
        }

        protected override void InvokeImplementation(Bot bot) {

        }

        protected override void InvokeImplementation(Bot bot, ChatMessage message) {
            bot.SendMessage($"{message.Username} has {PersistendUserInfo.Instance.GetPointsOf(message.UserId)} Points");
        }
    }
}
