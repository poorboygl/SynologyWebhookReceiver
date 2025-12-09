using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

List<WebSocket> _clients = new List<WebSocket>();

// WebSocket cho Tekla Plugin
app.UseWebSockets();
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var ws = await context.WebSockets.AcceptWebSocketAsync();
        _clients.Add(ws);
        Console.WriteLine("Client Tekla connected.");

        var buffer = new byte[2048];

        while (ws.State == WebSocketState.Open)
        {
            await ws.ReceiveAsync(buffer, CancellationToken.None);
        }

        _clients.Remove(ws);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

// Webhook từ Synology
app.MapPost("/synology/webhook", async (HttpRequest req) =>
{
    string body;
    using (var reader = new StreamReader(req.Body))
        body = await reader.ReadToEndAsync();

    Console.WriteLine("📥 Webhook Received:");
    Console.WriteLine(body);

    // Gửi realtime xuống Tekla plugin
    var msg = Encoding.UTF8.GetBytes(body);
    foreach (var ws in _clients.ToList())
    {
        if (ws.State == WebSocketState.Open)
        {
            await ws.SendAsync(msg, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    return Results.Ok();
});

// Lắng nghe toàn mạng LAN
app.Urls.Add("http://0.0.0.0:9455");
app.Run();