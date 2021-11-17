using System;
using System.Collections.Generic;
using System.Linq;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TwitchBot {
    class Bot {
        private TwitchClient client;
        private bool logging = true; // output logging messages? gets automatically disabled on successful connect
        private Config config;
        private CommandList nativeCommands = new(); // commands that execute C# code
        private CommandList customCommands = PersistentCommandList.LoadOrCreate("commands.json"); // commands that can be set through the Twitch chat

        public Bot(Config config) {
            SetupNativeCommands();
            this.config = config;
            var credentials = new ConnectionCredentials(config.Username, config.OAuthToken);
            var clientOptions = new ClientOptions {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            var customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, config.Channel);

            client.OnLog += OnClientLog;
            client.OnJoinedChannel += OnClientJoinedChannel;
            client.OnMessageReceived += OnClientMessageReceived;
            client.OnConnected += OnClientConnected;

            client.Connect();
        }

        private bool Authenticate(ChatMessage message) {
            if (!message.IsModerator && !message.IsBroadcaster) {
                SendMessage($"@{message.Username} Ah ah ah! Du hast das Zauberwort nicht gesagt! Ah ah ah!");
                return false;
            }
            return true;
        }

        private void SetupNativeCommands() {
            nativeCommands.Commands.Add(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!add"),
                Authenticator = Authenticators.ModOrBroadcaster,
                Handler = (bot, message) => {                    
                    var parts = message.Message.Split(" ", 3);
                    if (parts.Length != 3) {
                        bot.SendMessage($"@{message.Username} Syntax: !add <trigger> <message>");
                        return;
                    }
                    customCommands.Add(new EchoCommand {
                        Trigger = Triggers.StartsWithWord(parts[1]),
                        Message = parts[2],
                        Cooldown = TimeSpan.FromSeconds(29)
                    });
                    bot.SendMessage($"@{message.Username} Kommando {parts[1]} hinzugefügt!");
                },
                Cooldown = null
            });
            nativeCommands.Commands.Add(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!remove"),
                Authenticator = Authenticators.ModOrBroadcaster,
                Handler = (bot, message) => {
                    var parts = message.Message.Split(" ", 2);
                    if (parts.Length != 2) {
                        bot.SendMessage($"@{message.Username} Syntax: !remove <trigger>");
                        return;
                    }
                    var trigger = parts[1];
                    var findResult = customCommands.Commands.FirstOrDefault(
                        command => command.Trigger.ShouldTrigger(trigger));
                    if (findResult is null) {
                        bot.SendMessage($"@{message.Username} Unbekannter Trigger: \"{trigger}\"");
                        return;
                    }
                    customCommands.Remove(findResult);
                    bot.SendMessage($"@{message.Username} Kommando {trigger} entfernt!");
                },
                Cooldown = null
            });
            nativeCommands.Commands.Add(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!list"),
                Handler = (bot, message) => {
                    var listString = nativeCommands.Commands
                            .Concat(customCommands.Commands)
                            .Where(command => command.Trigger.HasListRepresentation())
                            .Where(command => command.Authenticator.Authenticate(message))
                            .Select(command => command.Trigger.GetListRepresentation())
                            .OrderBy(representationString => representationString)
                            .Aggregate((previous, toAppend) => $"{previous} {toAppend}");
                    bot.SendMessage($"@{message.Username} Verfügbare Kommandos: {listString}");
                },
                Cooldown = TimeSpan.FromSeconds(30)
            });
        }

        private void OnClientLog(object sender, OnLogArgs args) {
            if (!logging) {
                return;
            }
            Console.WriteLine($"{args.DateTime}: {args.BotUsername} - {args.Data}");
        }

        private void OnClientJoinedChannel(object sender, OnJoinedChannelArgs args) {
            logging = false;
            SendMessage("Achtung, Achtung, der persönliche Bedienstete von Lord coder2k ist eingetroffen!");
        }

        private bool HandleCommands(ChatMessage message) {
            foreach (var command in nativeCommands.Commands.Concat(customCommands.Commands)) {
                if (command.Trigger.ShouldTrigger(message)) {
                    command.Invoke(this, message);
                    return true;
                }
            }
            return false;
        }

        private void OnClientMessageReceived(object sender, OnMessageReceivedArgs args) {
            PrintUserMessage(args.ChatMessage.Username,
                             args.ChatMessage.Message,
                             args.ChatMessage.IsMe,
                             args.ChatMessage.IsHighlighted ? Color.Highlighted : null);
            if (HandleCommands(args.ChatMessage)) {
                Console.WriteLine("Handled command");
            }
        }

        private void OnClientConnected(object sender, OnConnectedArgs args) {
            Console.WriteLine($"Connected to {args.AutoJoinChannel}");
        }

        public void SendMessage(String message) {
            PrintUserMessage(config.Username, message,
                             message.StartsWith("/me"),
                             Color.BotHighlighted);
            client.SendMessage(config.Channel, message);
        }

        private void PrintUserMessage(String username, String message, bool isMe = false,
                                      Color color = null) {
            color ??= Color.Default;
            color.Apply();
            if (isMe) {
                Console.Write($"{DateTime.Now.ToString("HH:mm:ss")}: {username} {message}");
            } else {
                Console.Write($"{DateTime.Now.ToString("HH:mm:ss")} {username}: {message}");
            }
            Color.Default.Apply();
            Console.WriteLine();
        }
    }
}
