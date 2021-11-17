using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace TwitchBot {
    internal class CommandList {
        public List<Command> Commands { get; set; } = new();

        public virtual void Add(Command command) {
            Commands.Add(command);
        }

        public virtual void Remove(Command command) {
            Commands.Remove(command);
        }

        private String ApplyPlaceholders(String response) {
            // TODO: implement
            return response;
        }

        public bool Handle(Bot bot, ChatMessage message) {
            foreach (var command in Commands) {
                if (command.Trigger.ShouldTrigger(message)) {
                    command.Invoke(bot, message);
                    return true;
                }
            }
            return false;
        }
    }
}
