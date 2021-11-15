using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace TwitchBot {
    class Command : AbstractCommand {
        public String Trigger { get; init; }
        public Action<ChatMessage> Handler { private get; init; }

        public void Invoke(ChatMessage message) {
            if (CanInvoke) {
                base.Invoke(message);
                Handler(message);
            }            
        }
    }

}
