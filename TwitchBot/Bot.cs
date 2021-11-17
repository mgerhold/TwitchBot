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
        private bool logging = true;
        private Config config;
        private List<NativeCommand> commands = new();
        private EchoCommandList simpleCommands = EchoCommandList.LoadOrCreate();

        public Bot(Config config) {
            SetupCommands();
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

        private void SetupCommands() {
            commands.Add(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!add"),
                Authenticator = Authenticators.ModOrBroadcaster,
                Handler = (bot, message) => {                    
                    var parts = message.Message.Split(" ", 3);
                    if (parts.Length != 3) {
                        bot.SendMessage($"@{message.Username} Syntax: !add <trigger> <message>");
                        return;
                    }
                    simpleCommands.Commands.Add(new EchoCommand {
                        Trigger = Triggers.StartsWithWord(parts[1]),
                        Message = parts[2],
                        Cooldown = TimeSpan.FromSeconds(29)
                    });
                    simpleCommands.Save();
                    bot.SendMessage($"@{message.Username} Kommando {parts[1]} hinzugefügt!");
                },
                Cooldown = null
            });
            commands.Add(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!remove"),
                Authenticator = Authenticators.ModOrBroadcaster,
                Handler = (bot, message) => {
                    var parts = message.Message.Split(" ", 2);
                    if (parts.Length != 2) {
                        bot.SendMessage($"@{message.Username} Syntax: !remove <trigger>");
                        return;
                    }
                    var trigger = parts[1];
                    var findResult = simpleCommands.Commands.FirstOrDefault(
                        command => command.Trigger.ShouldTrigger(trigger));
                    if (findResult is null) {
                        bot.SendMessage($"@{message.Username} Unbekannter Trigger: \"{trigger}\"");
                        return;
                    }
                    simpleCommands.Commands.Remove(findResult);
                    simpleCommands.Save();
                    bot.SendMessage($"@{message.Username} Kommando {trigger} entfernt!");
                },
                Cooldown = null
            });
            commands.Add(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!list"),
                Handler = (bot, message) => {
                    List<String> commandList = new();
                    commandList.AddRange(commands
                        .Where(command => command.Trigger.HasListRepresentation())
                        .Where(command => command.Authenticator.Authenticate(message))
                        .Select(command => command.Trigger.GetListRepresentation()));
                    commandList.AddRange(simpleCommands.Commands
                        .Where(command => command.Trigger.HasListRepresentation())
                        .Where(command => command.Authenticator.Authenticate(message))
                        .Select(command => command.Trigger.GetListRepresentation()));
                    commandList.Sort();
                    var listString = String.Join(" ", commandList);
                    bot.SendMessage($"@{message.Username} Verfügbare Kommandos: {listString}");
                },
                Cooldown = null //TimeSpan.FromSeconds(30)
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
            var parts = message.Message.Trim().Split(" ", 2);
            if (parts.Length == 0) {
                return false;
            }
            String trigger = parts[0];
            foreach (var command in commands) {
                if (command.Trigger.ShouldTrigger(message)) {
                    command.Invoke(this, message);
                    return true;
                }
            }
            return simpleCommands.Handle(this, message);
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
