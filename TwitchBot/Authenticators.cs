﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace TwitchBot {
    internal class Authenticators {

        public static IAuthenticator Pleb { get; } = new PlebAuthenticator();
        public static IAuthenticator Mod { get; } = new ModAuthenticator();
        public static IAuthenticator Broadcaster { get; } = new BroadcasterAuthenticator();
        public static IAuthenticator ModOrBroadcaster { get; } = Mod | Broadcaster;

        private class ModAuthenticator : IAuthenticator {
            public bool Authenticate(ChatMessage message) {
                return message.IsModerator;
            }
        }

        private class BroadcasterAuthenticator : IAuthenticator {
            public bool Authenticate(ChatMessage message) {
                return message.IsBroadcaster;
            }
        }

        private class PlebAuthenticator : IAuthenticator {
            public bool Authenticate(ChatMessage message) {
                return true;
            }
        }

        private class ModOrBroadcasterAuthenticator : IAuthenticator {
            public bool Authenticate(ChatMessage message) {
                return message.IsModerator || message.IsBroadcaster;
            }
        }

    }
}
