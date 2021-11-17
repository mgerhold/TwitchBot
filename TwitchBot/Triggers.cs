using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace TwitchBot {
    internal class Triggers {

        public static ITrigger StartsWithWord(String word) {
            return new StartsWithWordTrigger(word);
        }

        private class StartsWithWordTrigger : ITrigger {
            [JsonProperty] private String word;

            public String GetListRepresentation() {
                return word;
            }

            public StartsWithWordTrigger(String word) {
                this.word = word;
            }

            public bool ShouldTrigger(String message) {
                var parts = message.Trim().Split(" ", 2);
                return parts.Length > 0 && parts[0] == word;
            }
        }
    }
}
