using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace TwitchBot {

    class EchoCommand : Command {
        public String Response { get; set; }

        protected override void InvokeImplementation(Bot bot, ChatMessage message) {
            bot.SendMessage(ApplyPlaceholders(Response, message));
        }

        protected override void InvokeImplementation(Bot bot) {            
            bot.SendMessage(Response);
        }
    }

}
