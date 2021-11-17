using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace TwitchBot {
    internal interface IAuthenticator {
        public abstract bool Authenticate(ChatMessage message);
    }
}
