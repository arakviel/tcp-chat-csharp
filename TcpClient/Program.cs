using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpClientDemo;

class ChatClient
{
    private readonly TcpClient _client;

    public ChatClient(string ip, int port)
    {
        _client = new TcpClient();
        _client.Connect(ip, port);
    }

    public async Task StartAsync()
    {
        Console.WriteLine("Connected to the server.");
        var stream = _client.GetStream();

        _ = ReceiveMessagesAsync(stream);

        while (true)
        {
            string message = Console.ReadLine() ?? string.Empty;
            var buffer = Encoding.UTF8.GetBytes(message);

            await stream.WriteAsync(buffer);

            if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Disconnected from the server.");
                break;
            }
        }

        _client.Close();
    }

    private async Task ReceiveMessagesAsync(NetworkStream stream)
    {
        var buffer = new byte[1024];

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Server: {message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection lost: {ex.Message}");
        }
    }
}

class Program
{
    static async Task Main()
    {
        // Запитуємо у користувача домен (наприклад, 0.tcp.ngrok.io:18572)
        Console.WriteLine("Введіть домен сервера (наприклад, 0.tcp.ngrok.io:18572): ");
        string serverDomainWithPort = Console.ReadLine();

        // Відокремлюємо домен від порту
        string serverDomain = GetDomainFromInput(serverDomainWithPort);

        // Отримуємо IP-адресу для домену
        string serverIp = GetServerIpAddress(serverDomain);
        if (serverIp == null)
        {
            Console.WriteLine("Не вдалося розшифрувати домен.");
            return;
        }

        // Запитуємо порт
        int port = GetPortFromInput(serverDomainWithPort);


        var client = new ChatClient(serverIp, port);
        await client.StartAsync();
    }

    // Функція для відокремлення домену від порту
    static string GetDomainFromInput(string input)
    {
        int colonIndex = input.IndexOf(':');
        return colonIndex >= 0 ? input.Substring(0, colonIndex) : input;
    }

    // Функція для отримання порту з введеного рядка
    static int GetPortFromInput(string input)
    {
        int colonIndex = input.IndexOf(':');
        return colonIndex >= 0 ? int.Parse(input.Substring(colonIndex + 1)) : 0;
    }

    // Функція для отримання IP-адреси за доменом
    static string GetServerIpAddress(string serverDomain)
    {
        try
        {
            // Отримуємо список IP-адрес для домену
            IPAddress[] ipAddresses = Dns.GetHostAddresses(serverDomain);
            if (ipAddresses.Length > 0)
            {
                return ipAddresses[0].ToString(); // Беремо перший IP-адрес з списку
            }
            else
            {
                return null; // Якщо немає жодної IP-адреси
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Помилка при розшифровці домену: " + ex.Message);
            return null;
        }
    }
}