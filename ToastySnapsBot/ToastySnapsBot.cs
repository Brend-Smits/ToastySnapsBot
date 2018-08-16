using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace ToastySnapsBot
{
    public class ToastySnapsBot
    {
        private readonly DiscordSocketClient _client;
        private readonly Dictionary<ulong, bool> _messageDictionary = new Dictionary<ulong, bool>();
        private ulong channelId = 468012261177294849;

        static void Main(string[] args)
            => new ToastySnapsBot().MainAsync().GetAwaiter().GetResult();

        public ToastySnapsBot()
        {
            _client = new DiscordSocketClient();
            _client.Ready += async () => { };
        }

        public async Task MainAsync()
        {
            _client.Log += Log;
            _client.MessageReceived += MessageReceived;
            await _client.LoginAsync(TokenType.Bot, "");
            await _client.StartAsync();
            await RunPeriodicAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), CancellationToken.None);
            await Task.Delay(-1);
        }

        // The `onTick` method will be called periodically unless cancelled.
        private async Task RunPeriodicAsync(
            TimeSpan dueTime,
            TimeSpan interval,
            CancellationToken token)
        {
            // Initial wait time before we begin the periodic loop.
            if (dueTime > TimeSpan.Zero)
                await Task.Delay(dueTime, token);

            // Repeat this loop until cancelled.
            while (!token.IsCancellationRequested)
            {
                foreach (var id in _messageDictionary.Where(x => x.Value == false).Select(c => c.Key))
                {
                    await Task.Factory.StartNew(async () => { await CheckGreenTick(id); }, token);
                }


                // Wait to repeat again.
                if (interval > TimeSpan.Zero)
                    await Task.Delay(interval, token);
            }
        }

        private async Task CheckGreenTick(ulong messageId)
        {
            try
            {
                Console.WriteLine("We are taking care of message: " + messageId);

                if (_client.GetChannel(channelId) is IMessageChannel channel)
                {
                    var newMessage = await channel.GetMessageAsync(messageId);
                    var value = _messageDictionary.FirstOrDefault(x => x.Key == messageId).Value;
                    Console.WriteLine("Value is: " + value);
                    if (newMessage != null)
                    {
                        Console.WriteLine("Value is: " + value);
                        await CheckMessageForGreenTick(newMessage);
                        return;
                    }
                }

                _messageDictionary.Remove(messageId);
                    Console.WriteLine("No message exists with that ID");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task AddReaction(IMessage message)
        {
            try
            {
                var messa = (RestUserMessage) await message.Channel.GetMessageAsync(message.Id);
                SocketGuild guild = ((SocketGuildChannel) message.Channel).Guild;
                IEmote greentick = guild.Emotes.First(e => e.Name == "toastygreentick");
                await messa.AddReactionAsync(greentick);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task MessageReceived(SocketMessage message)
        {
            try
            {
                if (message.Channel.Id == channelId)
                {
                    await CheckMessageForGreenTick(message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        private async Task CheckMessageForGreenTick(IMessage message)
        {
            try
            {
                Console.WriteLine("Message found!" + message.Content);
                Regex regex = new Regex("(http)?s?:?(\\/\\/[^\"\']*\\.(?:png|jpg|jpeg|gif|png|svg|JPG))");
                if (message.Attachments.Any())
                {
                    string url = message.Attachments.FirstOrDefault()?.Url.ToLowerInvariant();
                    if (url != null && (url.EndsWith("png") || url.EndsWith("jpg") || url.EndsWith("jpeg")))
                    {
                        Console.WriteLine("URL End match found. Reaction should be set of image with url: " + url);
                        _messageDictionary[message.Id] = true;
                        await AddReaction(message);
                    }
                }
                else if (message.Embeds.Any())
                {
                    string embedUrl = message.Embeds.FirstOrDefault()?.Thumbnail?.Url.ToLowerInvariant();
                    if (embedUrl != null && regex.IsMatch(embedUrl))
                    {
                        Console.WriteLine("URL Embed Regex Match found. Reaction added! + " + embedUrl);
                        _messageDictionary[message.Id] = true;
                        await AddReaction(message);
                    }
                }
                else
                {
                    if (_messageDictionary.ContainsKey(message.Id))
                    {
                        Console.WriteLine("Set the value to true in dictionary");
                        _messageDictionary[message.Id] = true;
                        return;
                    }
                        Console.WriteLine("Added to Dictionary with value set to false");
                        _messageDictionary.Add(message.Id, false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        }
}