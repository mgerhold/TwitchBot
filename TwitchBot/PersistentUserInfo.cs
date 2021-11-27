using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TwitchBot.Models;

namespace TwitchBot {
    public class PersistentUserInfo {
        public static PersistentUserInfo Instance = PersistentUserInfo.LoadOrCreate("userInfo.json");
        public List<UserInfo> UserInfos;
        public PointSystemSettings Settings = new PointSystemSettings();
        private string filename;
        private Mutex userInfoListMutex = new Mutex();

        private PersistentUserInfo(string filename, List<UserInfo> userInfos) {
            this.filename = filename;
            UserInfos = userInfos;
        }

        public static PersistentUserInfo LoadOrCreate(string filename) {
            if (File.Exists(filename)) {
                return Load(filename);
            }
            Console.WriteLine("Creating new empty userinfo list");
            var result = new PersistentUserInfo(filename, new List<UserInfo>());
            result.Save();
            return result;
        }

        public static PersistentUserInfo Load(string filename) {
            string lines;
            try {
                lines = File.ReadAllText(filename);
            } catch (Exception e) {
                throw new Exception("Unable to open userinfo file", e);
            }
            var deserialized = JsonConvert.DeserializeObject<List<UserInfo>>(lines,
                new JsonSerializerSettings {
                    TypeNameHandling = TypeNameHandling.Auto
                });

            if (deserialized is null) {
                throw new Exception("Unable to deserialize command list");
            }
            return new PersistentUserInfo(filename, deserialized);
        }

        public void Save() {
            try {
                userInfoListMutex.WaitOne();
                File.WriteAllText(filename, JsonConvert.SerializeObject(UserInfos, Formatting.Indented,
                    new JsonSerializerSettings {
                        TypeNameHandling = TypeNameHandling.All
                    }));
            } catch (Exception e) {
                throw new Exception("Unable to save userinfo list", e);
            } finally {
                userInfoListMutex.ReleaseMutex();
            }
        }

        public int GetPointsOf(string userId) {
            var userInfo = GetUserInfo(userId);

            if (userInfo is not null) {
                return userInfo.Points;
            }

            return 0;
        }

        public void AddPointsTo(string userId, int points = 1, bool writeToFile = true) {
            if (points > 0) {
                var userInfo = GetUserInfo(userId);

                if (userInfo is not null) {
                    userInfo.Points += points;
                } else {
                    userInfo = new UserInfo();
                    userInfo.Points = points;
                    userInfo.UserId = userId;

                    UserInfos.Add(userInfo);
                }

                userInfo.LastPointSet = DateTime.Now;

                if (writeToFile) {
                    Save();
                }
                Console.WriteLine($"Points added to {userId}");
            }
        }

        public void RemovePointsFrom(string userId, int points = 1) {
            var userInfo = GetUserInfo(userId);

            if (userInfo is not null && userInfo.Points >= points) {
                userInfo.Points -= points;
            } else {
                throw new Exception($"{userId} has no Points to use");
            }

            Save();

            Console.WriteLine($"Points removed from {userId}");
        }

        public UserInfo GetUserInfo(string userId) {
            return (from userInfo in UserInfos
                    where userInfo.UserId == userId
                    select userInfo).FirstOrDefault();
        }
    }
}
