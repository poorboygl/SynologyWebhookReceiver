using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class SynologyMessage
{
    public string? Text { get; set; }
}

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔌 Tekla Plugin Listener (WebSocket Client)");
        Console.WriteLine("Đang kết nối đến ws://192.168.50.2:9455/ws ...");

        using var client = new ClientWebSocket();

        try
        {
            await client.ConnectAsync(new Uri("ws://192.168.50.2:9455/ws"), CancellationToken.None);
            Console.WriteLine("✅ Đã kết nối WebSocket\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Lỗi kết nối WebSocket: " + ex.Message);
            return;
        }

        var buffer = new byte[2048];

        while (client.State == WebSocketState.Open)
        {
            var result = await client.ReceiveAsync(buffer, CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("❌ WebSocket đóng.");
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                break;
            }

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine("📥 JSON nhận được:");
            Console.WriteLine(json);

            try
            {
                // Deserialize đúng kiểu JSON
                var msg = JsonSerializer.Deserialize<UpsMessage>(json);

                if (msg != null)
                    HandleMessage(msg.text);
                else
                    Console.WriteLine("⚠ JSON null");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi Deserialize JSON: " + ex.Message);
            }
        }
    }
    class UpsMessage
    {
        public required string text { get; set; }
    }

    static void HandleMessage(string text)
    {
        if (text.StartsWith("BATTERY-MODE"))
        {
            var minutes = ExtractMinutes(text);
            Console.WriteLine($"⚠️ Server đã mất điện — bạn còn {minutes} phút để Lưu");
        }
        else if (text.StartsWith("AC-MODE"))
        {
            Console.WriteLine("⚡ Server đã có điện lại");
        }
        else
        {
            Console.WriteLine("ℹ️ Không xác định: " + text);
        }
    }

    static int ExtractMinutes(string text)
    {
        // Ví dụ: "Estimated battery time: 38 minutes."
        var parts = text.Split(' ');
        for (int i = 0; i < parts.Length; i++)
            if (parts[i] == "minutes." && i > 0 && int.TryParse(parts[i - 1], out int value))
                return value;

        return -1;
    }
}

