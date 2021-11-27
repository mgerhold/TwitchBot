using Newtonsoft.Json;

namespace TwitchBot {
    internal class Triggers {

        public static ITrigger StartsWithWord(string word) {
            return new StartsWithWordTrigger(word);
        }

        private class StartsWithWordTrigger : ITrigger {
            [JsonProperty] private string word;
            [JsonProperty] private bool caseSensitive;

            public StartsWithWordTrigger(string word, bool caseSensitive = false) {
                this.word = word;
                this.caseSensitive = caseSensitive;
            }

            public string GetListRepresentation() {
                return word;
            }

            public bool ShouldTrigger(string message) {
                var parts = message.Trim().Split(" ", 2);
                return parts.Length > 0 && (
                            parts[0] == word || (!caseSensitive && parts[0].ToUpper() == word.ToUpper())
                       );
            }
        }
    }
}
