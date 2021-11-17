using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace TwitchBot {

    class EchoCommand : Command {
        public String Message { get; set; }

        protected override void InvokeImplementation(Bot bot, ChatMessage message) {
            bot.SendMessage(Message);
        }
    }

}
