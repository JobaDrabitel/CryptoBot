using Telegram.Bot;
using TokenBot;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var token = builder.Configuration["BotSettings:token"];
var chatId = builder.Configuration["BotSettings:chatId"];
var url = builder.Configuration["TokenSettings:url"];
builder.Services.AddSingleton<HttpClient>()
    .AddSingleton(new TelegramBotClient(token));

builder.Services.AddSingleton(sp =>
    new CoinTracker(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<TelegramBotClient>(),
        chatId,
        url
    )
);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var coinTracker = app.Services.GetRequiredService<CoinTracker>();
await coinTracker.RunAsync();

app.UseHttpsRedirection();

app.Run();
