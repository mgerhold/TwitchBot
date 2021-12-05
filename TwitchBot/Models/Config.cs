using Newtonsoft.Json;
using System;
using System.IO;

namespace TwitchBot.Models {

    class Config {
        public string Username { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TwitchRedirectURL { get; set; }
        public string Channel { get; set; }
        public string BroadcasterId { get; set; }
        public TimeSpan TimedMessagesInterval { get; set; } = TimeSpan.FromMinutes(25);

        private const string filename = "config.json";

        public static Config LoadOrDefault() {
            if (File.Exists(filename)) {
                return Load();
            }
            Console.WriteLine("Creating new default config file");
            var result = new Config {
                Username = "name of bot",
                Channel = "channel name",
                ClientID = "client id",
                ClientSecret = "client secret",
            };
            result.Save();
            return result;
        }

        public static Config Load() {
            string lines;
            try {
                lines = File.ReadAllText(filename);
            } catch (Exception e) {
                throw new Exception("Unable to load config file", e);
            }
            var result = JsonConvert.DeserializeObject<Config>(lines);
            if (result == null) {
                throw new Exception("Unable to deserialize configuration file");
            }
            return result;
        }

        public void Save() {
            string jsonstring = JsonConvert.SerializeObject(this, Formatting.Indented);
            try {
                File.WriteAllText(filename, jsonstring);
            } catch (Exception e) {
                throw new Exception("Unable to write configuration file", e);
            }
        }

        public override string ToString() {
            return $"User name: {Username}, Channel: {Channel}";
        }
    }

}
