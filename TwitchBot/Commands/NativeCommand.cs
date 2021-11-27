using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;
using TwitchBot;

namespace TwitchBot.Commands
{

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
