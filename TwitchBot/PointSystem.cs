using System;
using System.Threading;
using TwitchBot.Commands;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace TwitchBot {
    internal class PointSystem {
        private Mutex userInfoListsMutex = new Mutex();
        private Thread timedPointsThread = null;

        public void Init(TwitchClient client, Bot bot) {
            bot.AddNativeCommand(new MyPointsCommand());

            timedPointsThread = new Thread(new ThreadStart(HandlePoints));
            timedPointsThread.Start();
        }

        private void HandlePoints() {
            Console.WriteLine("Started thread to handle points...");

            while (true) {
                var persistentUserInfo = PersistentUserInfo.Instance;
                try {
                    userInfoListsMutex.WaitOne();
                    foreach (var userInfo in persistentUserInfo.UserInfos) {
                        if (userInfo.LastPointSet + persistentUserInfo.Settings.PointGivingDelay < DateTime.Now) {
                            persistentUserInfo.AddPointsTo(userInfo.UserId, persistentUserInfo.Settings.UserTimedAmount, false);
                        }
                    }
                    persistentUserInfo.Save();
                } finally {
                    userInfoListsMutex.ReleaseMutex();
                }
                Thread.Sleep(persistentUserInfo.Settings.PointGivingDelay);
            }
        }

        public void HandleFirstMessage(ChatMessage chatMessage) {
            if (PersistentUserInfo.Instance.GetUserInfo(chatMessage.UserId) is null) {
                PersistentUserInfo.Instance.AddPointsTo(chatMessage.UserId, PersistentUserInfo.Instance.Settings.UserJoinAmount);
            }
        }
    }
}
