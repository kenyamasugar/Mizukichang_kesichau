using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    private DiscordSocketClient _client;
    private ulong _targetChannelId;

    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages
        };
        _client = new DiscordSocketClient(config);
        _client.Log += msg => { Console.WriteLine(msg); return Task.CompletedTask; };
        _client.Ready += OnReadyAsync;

        string token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        if (string.IsNullOrEmpty(token)) throw new Exception("環境変数 DISCORD_TOKEN が未設定");
        string channelIdStr = Environment.GetEnvironmentVariable("TARGET_CHANNEL_ID");
        if (!ulong.TryParse(channelIdStr, out _targetChannelId)) throw new Exception("TARGET_CHANNEL_ID が無効");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private async Task OnReadyAsync()
    {
        Console.WriteLine("Bot is connected!");
        var channel = _client.GetChannel(_targetChannelId) as IMessageChannel;
        if (channel == null) return;

        while (true)
        {
            var now = DateTimeOffset.UtcNow;
            var threshold = now.AddHours(-6);
            var messages = await channel.GetMessagesAsync(limit: 500).FlattenAsync();

            foreach (var message in messages)
            {
                if (message.Timestamp < threshold)
                {
                    try
                    {
                        await channel.DeleteMessageAsync(message);
                        Console.WriteLine($"削除: {message.Id}");
                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"削除失敗: {ex.Message}");
                    }
                }
            }
            Console.WriteLine("巡回完了。1時間後に再実行。");
            await Task.Delay(TimeSpan.FromHours(1));
        }
    }
}
