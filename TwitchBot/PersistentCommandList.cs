using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using TwitchLib.Client.Models;

namespace TwitchBot {
    class PersistentCommandList : CommandList {

        private PersistentCommandList(string filename, List<Command> commands) {
            this.filename = filename;
            Commands = commands;
        }

        private string filename;

        public static PersistentCommandList LoadOrCreate(string filename) {
            if (File.Exists(filename)) {
                return Load(filename);
            }
            Console.WriteLine("Creating new empty commands list");
            var result = new PersistentCommandList(filename, new List<Command>());
            result.Save();
            return result;
        }

        public static PersistentCommandList Load(string filename) {
            string lines;
            try {
                lines = File.ReadAllText(filename);
            } catch (Exception e) {
                throw new Exception("Unable to open command list file", e);
            }
            var deserialized = JsonConvert.DeserializeObject<List<Command>>(lines,
                new JsonSerializerSettings {
                    TypeNameHandling = TypeNameHandling.Auto
                });
            if (deserialized is null) {
                throw new Exception("Unable to deserialize command list");
            }
            return new PersistentCommandList(filename, deserialized);
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

        public override void Add(Command command) {
            base.Add(command);
            Save();
        }

        public override void Remove(Command command) {
            base.Remove(command);
            Save();
        }

    }

}
