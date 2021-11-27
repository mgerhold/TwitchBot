using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchBot.Commands;
using TwitchBot.Models;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace TwitchBot
{
    internal class PointSystemManager {
        private Mutex userInfoListsMutex = new Mutex();
        private Thread timedPointsThread = null;

        public void Init(TwitchClient client, Bot bot) {
            bot.AddCommand(new MyPointsCommand());

            timedPointsThread = new Thread(new ThreadStart(HandlePoints));
            timedPointsThread.Start();
        }

        private void HandlePoints() {
            Console.WriteLine("Started thread to handle points...");

            while (true)
            {
                try
                {
                    userInfoListsMutex.WaitOne();

                    var userToAddPoints = PersistendUserInfo.Instance.UserInfos;

                    for (int i = 0; i < userToAddPoints.Count; i++)
                    {
                        UserInfo userInfo = userToAddPoints[i];

                        if (userInfo.LastPointSet + PersistendUserInfo.Instance.Settings.PointGivingDelay < DateTime.Now)
                        {
                            PersistendUserInfo.Instance.AddPointsTo(userInfo.UserId, PersistendUserInfo.Instance.Settings.UserTimedAmount);
                        }
                    }
                }
                finally
                {
                    userInfoListsMutex.ReleaseMutex();
                }

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        public void HandleFirstMessage(ChatMessage chatMessage) {
            if (PersistendUserInfo.Instance.GetUserInfo(chatMessage.UserId) is null)
            {
                PersistendUserInfo.Instance.AddPointsTo(chatMessage.UserId, PersistendUserInfo.Instance.Settings.UserJoinAmount);
            }
        }
    }
}
