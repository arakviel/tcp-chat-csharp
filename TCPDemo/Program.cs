using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpServer;

class ChatServer
{
    private readonly TcpListener _listener;
    private readonly List<TcpClient> _clients = new();

    public ChatServer(string ip, int port)
    {
        _listener = new TcpListener(IPAddress.Parse(ip), port);
    }

    public async Task StartAsync()
    {
        _listener.Start();
        Console.WriteLine("Server started. Waiting for clients...");

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            _clients.Add(client);
            Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            var buffer = new byte[1024];
            var stream = client.GetStream();

            try
            {
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine($"Received: {message}");

                    if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
                        _clients.Remove(client);
                        break;
                    }

                    await BroadcastMessageAsync(message, client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with client {client.Client.RemoteEndPoint}: {ex.Message}");
            }
        }
    }

    private async Task BroadcastMessageAsync(string message, TcpClient sender)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        var tasks = _clients.Where(c => c != sender).Select(c => c.GetStream().WriteAsync(buffer).AsTask());
        await Task.WhenAll(tasks);
    }
}

class Program
{
    static async Task Main()
    {
        var server = new ChatServer(IPAddress.Any.ToString(), 5123);
        await server.StartAsync();
    }
}