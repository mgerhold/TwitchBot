using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace TwitchBot {

    class SimpleCommandList {
        public List<SimpleCommand> Commands { get; set; } = new();

        private const String filename = "commands.json";

        public static SimpleCommandList LoadOrCreate() {
            if (File.Exists(filename)) {
                return Load();
            }
            Console.WriteLine("Creating new empty commands list");
            var result = new SimpleCommandList();
            result.Save();
            return result;
        }

        public static SimpleCommandList Load() {
            String lines;
            try {
                lines = File.ReadAllText(filename);
            } catch (Exception e) {
                throw new Exception("Unable to open command list file", e);
            }
            var deserialized = JsonConvert.DeserializeObject<List<SimpleCommand>>(lines);
            if (deserialized is null) {
                throw new Exception("Unable to deserialize command list");
            }
            return new SimpleCommandList { Commands = deserialized };
        }

        public void Save() {
            try {
                File.WriteAllText(filename, JsonConvert.SerializeObject(Commands, Formatting.Indented));
            } catch (Exception e) {
                throw new Exception("Unable to save command list", e);
            }
        }

        private String ApplyPlaceholders(String response) {
            return response;
        }

        public String Handle(ChatMessage message) {
            var parts = message.Message.Trim().Split(" ", 2);
            if (parts.Length == 0) {
                return null;
            }
            foreach (var command in Commands) {
                if (command.Trigger == parts[0] && command.CanInvoke) {
                    command.Invoke(message);
                    return ApplyPlaceholders(command.Message);
                }
            }
            return null;
        }
    }

    class SimpleCommand : AbstractCommand {
        public String Trigger { get; set; }
        public String Message { get; set; }
    }

}
