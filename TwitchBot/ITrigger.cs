using TwitchLib.Client.Models;

namespace TwitchBot {
    internal interface ITrigger {
        public string GetListRepresentation();

        public bool HasListRepresentation() {
            return GetListRepresentation() != null;
        }

        public bool ShouldTrigger(ChatMessage message) {
            return ShouldTrigger(message.Message);
        }

        public bool ShouldTrigger(string message);
    }
}
