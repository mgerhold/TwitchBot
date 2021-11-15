using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchBot {

    class Config {
        public String Username { get; set; }
        public String OAuthToken { get; set; }
        public String Channel { get; set; }

        private const String filename = "config.json";

        public static Config LoadOrDefault() {
            if (File.Exists(filename)) {
                return Load();
            }
            Console.WriteLine("Creating new default config file");
            var result = new Config {
                Username = "name of bot",
                OAuthToken = "token",
                Channel = "channel name"
            };
            result.Save();
            return result;
        }

        public static Config Load() {
            String lines;
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
            String jsonString = JsonConvert.SerializeObject(this, Formatting.Indented);
            try {
                File.WriteAllText(filename, jsonString);
            } catch (Exception e) {
                throw new Exception("Unable to write configuration file", e);
            }
        }

        public override string ToString() {
            return $"User name: {Username}, Channel: {Channel}";
        }
    }

}
