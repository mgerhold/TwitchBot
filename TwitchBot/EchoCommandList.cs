using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using TwitchLib.Client.Models;

namespace TwitchBot {
    class EchoCommandList {
        public List<EchoCommand> Commands { get; set; } = new();

        private const String filename = "commands.json";

        public static EchoCommandList LoadOrCreate() {
            if (File.Exists(filename)) {
                return Load();
            }
            Console.WriteLine("Creating new empty commands list");
            var result = new EchoCommandList();
            result.Save();
            return result;
        }

        public static EchoCommandList Load() {
            String lines;
            try {
                lines = File.ReadAllText(filename);
            } catch (Exception e) {
                throw new Exception("Unable to open command list file", e);
            }
            var deserialized = JsonConvert.DeserializeObject<List<EchoCommand>>(lines,
                new JsonSerializerSettings {
                    TypeNameHandling = TypeNameHandling.Auto
                });
            if (deserialized is null) {
                throw new Exception("Unable to deserialize command list");
            }
            return new EchoCommandList { Commands = deserialized };
        }

        public void Save() {
            try {
                File.WriteAllText(filename, JsonConvert.SerializeObject(Commands, Formatting.Indented,
                    new JsonSerializerSettings {
                        TypeNameHandling = TypeNameHandling.All
                    }));
            } catch (Exception e) {
                throw new Exception("Unable to save command list", e);
            }
        }

        private String ApplyPlaceholders(String response) {
            return response;
        }

        public bool Handle(Bot bot, ChatMessage message) {
            var parts = message.Message.Trim().Split(" ", 2);
            if (parts.Length == 0) {
                return false;
            }
            foreach (var command in Commands) {
                if (command.Trigger.ShouldTrigger(message)) {
                    command.Invoke(bot, message);
                    return true;
                }
            }
            return false;
        }
    }

}
