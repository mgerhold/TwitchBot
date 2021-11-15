using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace TwitchBot {

    abstract class AbstractCommand {
        public TimeSpan? Cooldown { get; init; } = null;
        public bool CanInvoke => !Cooldown.HasValue || !lastInvoked.HasValue || DateTime.Now >= lastInvoked + Cooldown;

        public void Invoke(ChatMessage message) {
            if (CanInvoke) {
                lastInvoked = DateTime.Now;
            } else {
                Console.WriteLine("Cannot invoke command since cooldown stuff");
            }
        }

        protected DateTime? lastInvoked = null;
    }
}
