using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using TwitchBot.Models;
using System.Linq;

namespace TwitchBot
{
    public class PersistendUserInfo
    {
        public static PersistendUserInfo Instance = PersistendUserInfo.LoadOrCreate("userInfo.json");

        public List<UserInfo> UserInfos;

        private string filename;

        public PersistendUserInfo(string filename, List<UserInfo> userInfos)
        {
            this.filename = filename;
            UserInfos = userInfos;
        }

        public static PersistendUserInfo LoadOrCreate(string filename)
        {
            if (File.Exists(filename))
            {
                return Load(filename);
            }
            Console.WriteLine("Creating new empty commands list");
            var result = new PersistendUserInfo(filename, new List<UserInfo>());
            result.Save();
            return result;
        }

        public static PersistendUserInfo Load(string filename)
        {
            string lines;
            try
            {
                lines = File.ReadAllText(filename);
            }
            catch (Exception e)
            {
                throw new Exception("Unable to open userinfo file", e);
            }
            var deserialized = JsonConvert.DeserializeObject<List<UserInfo>>(lines,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });

            if (deserialized is null)
            {
                throw new Exception("Unable to deserialize command list");
            }
            return new PersistendUserInfo(filename, deserialized);
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(filename, JsonConvert.SerializeObject(UserInfos, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    }));
            }
            catch (Exception e)
            {
                throw new Exception("Unable to save userinfo list", e);
            }
        }

        public int GetPointsOf(string userId)
        {
            var userInfo = GetUserInfo(userId);

            if(userInfo is not null) {
                return userInfo.Points;
            }

            return 0;
        }

        public void AddPointsTo(string userId, int points = 1)
        {
            var userInfo = GetUserInfo(userId);

            if (userInfo is not null)
            {
                userInfo.Points += points;
            }
            else
            {
                userInfo = new UserInfo();
                userInfo.Points = points;
                userInfo.UserId = userId;

                UserInfos.Add(userInfo);
            }

            Save();
        }

        public void RemovePointsFrom(string userId, int points = 1)
        {
            var userInfo = GetUserInfo(userId);

            if (userInfo is not null && userInfo.Points >= points)
            {
                userInfo.Points -= points;
            }
            else {
                throw new Exception($"{userId} has no Points to use");
            }

            Save();
        }

        private UserInfo GetUserInfo(string userId)
        {
            return (from userInfo in UserInfos
                   where userInfo.UserId == userId
                   select userInfo).FirstOrDefault();
        }
    }
}
