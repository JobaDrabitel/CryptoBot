using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Telegram.Bot;

namespace TokenBot
{
    public class Token
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Mint { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CoinTracker
    {
        private readonly HttpClient _httpClient;
        private readonly TelegramBotClient _botClient;
        private readonly string _apiUrl;
        private readonly string _chatId;
        private readonly List<Token> _recentTokens = new();
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

        public CoinTracker(HttpClient httpClient, TelegramBotClient botClient, string chatId, string apiUrl)
        {
            _httpClient = httpClient;
            _botClient = botClient;
            _apiUrl = apiUrl;
            _chatId = chatId;
        }

        public async Task RunAsync()
        {
            while (true)
            {
                try
                {
                    var response = await _httpClient.GetStringAsync(_apiUrl);
                    var json = JObject.Parse(response);

                    var name = json["name"]?.ToString();
                    var symbol = json["symbol"]?.ToString();
                    var mint = json["mint"]?.ToString();
                    var createdTimestamp = json["created_timestamp"]?.ToObject<long>();

                    if (createdTimestamp != null && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(symbol) && !string.IsNullOrEmpty(mint))
                    {
                        var createdDate = DateTimeOffset.FromUnixTimeMilliseconds(createdTimestamp.Value).UtcDateTime;
                        var newToken = new Token
                        {
                            Name = name,
                            Symbol = symbol,
                            Mint = mint,
                            CreatedDate = createdDate
                        };

                        var existingToken = _recentTokens.Find(token => token.Name == name && token.Symbol == symbol && token.Mint == mint);
                        if (existingToken != null)
                        {
                            Console.WriteLine($"Токен {name} ({symbol}) с mint {mint} уже существует. {DateTime.Now}");
                            await Task.Delay(5000); // 5 seconds
                            continue;
                        }

                        var duplicateToken = _recentTokens.Find(token => token.Name == name || token.Symbol == symbol);
                        if (duplicateToken != null && duplicateToken.Mint != mint)
                        {
                            await _botClient.SendTextMessageAsync(_chatId, $"New token detected: {newToken.Name}" + $" ({newToken.Symbol}) with different mint. Last token: https://pump.fun/{newToken.Mint} | {DateTime.Now} \n Old Token: {duplicateToken.Name} ({duplicateToken.Symbol}) \nhttps://pump.fun/{duplicateToken.Mint} | {duplicateToken.CreatedDate}");
                            _recentTokens.Remove(duplicateToken);
                        }

                        _recentTokens.Add(newToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }

                _recentTokens.RemoveAll(token => DateTime.UtcNow - token.CreatedDate > _checkInterval);
                await Task.Delay(5000); // 10 seconds
            }
        }
    }
}
