﻿using TwitchLib.Client.Models;

namespace TwitchBot {
    internal class Authenticators {

        public static IAuthenticator Pleb { get; } = new PlebAuthenticator();
        public static IAuthenticator Mod { get; } = new ModAuthenticator();
        public static IAuthenticator Broadcaster { get; } = new BroadcasterAuthenticator();
        public static IAuthenticator ModOrBroadcaster { get; } = Mod | Broadcaster;

        public static IAuthenticator SingleUser(string userId) {
            return new SingleUserAuthenticator(userId);
        }

        public static IAuthenticator Points(int points) {
            return new UserPointAuthenticator(points);
        }

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

        private class SingleUserAuthenticator : IAuthenticator {
            private string userID;

            public SingleUserAuthenticator(string userID) {
                this.userID = userID;
            }

            public bool Authenticate(ChatMessage message) {
                return message.UserId == userID;
            }
        }

        private class UserPointAuthenticator : IAuthenticator {
            private int points;

            public UserPointAuthenticator(int points) {
                this.points = points;
            }

            public bool Authenticate(ChatMessage message) {
                var userPoints = PersistentUserInfo.Instance.GetPointsOf(message.UserId);

                return userPoints >= points;
            }
        }
    }
}
