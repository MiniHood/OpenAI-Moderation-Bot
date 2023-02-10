using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using OpenAIWrapper;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OpenAI_Bot
{
    internal class Program
    {
        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private OpenAI _openAI;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _openAI = new OpenAI("your_openai_api_key", "text-davinci-003");

            _client.MessageReceived += HandleMessageReceived;

            await _client.LoginAsync(TokenType.Bot, "your_discord_bot_token");
            await _client.StartAsync();

            await Task.Delay(-1);
        }
        private async Task HandleMessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot)
            {
                return;
            }

            string prompt = message.Content;
            string response = await _openAI.GenerateText("Is this immoral, illegal or downright rude? Only answer with yes or no. Do not answer with anything else.:\n" + prompt);

            if (response.Substring(0, 3).Contains("Yes"))
            {
                await AddWarning(message.Author as SocketGuildUser);
                await message.Channel.SendMessageAsync("That message is illegal, immoral or rude. Please refrain from saying it.");
            }
        }

        private async Task AddWarning(SocketGuildUser user)
        {
            // Load the warnings from the json file
            string json = File.ReadAllText("warnings.json");
            var warnings = JsonConvert.DeserializeObject<Dictionary<ulong, int>>(json) ?? new Dictionary<ulong, int>();

            // Increment the warning count for the user
            if (!warnings.ContainsKey(user.Id))
                warnings[user.Id] = 0;

            warnings[user.Id]++;

            // Save the updated warnings back to the json file
            File.WriteAllText("warnings.json", JsonConvert.SerializeObject(warnings));

            if (warnings[user.Id] >= 3)
            {
                // Kick the user if they have reached 3 warnings
                await user.KickAsync();
                await user.Guild.DefaultChannel.SendMessageAsync($"{user.Mention} has been kicked for reaching 3 warnings.");
            }
            else if (warnings[user.Id] >= 5)
            {
                // Ban the user if they have reached 5 warnings
                await user.Guild.AddBanAsync(user, reason: "Reached 5 warnings");
                await user.Guild.DefaultChannel.SendMessageAsync($"{user.Mention} has been banned for reaching 5 warnings.");
            }
        }
    }
}
