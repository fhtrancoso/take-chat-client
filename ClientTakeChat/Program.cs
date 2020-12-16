using ClientTakeChat.Converters;
using ClientTakeChat.Model;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientTakeChat
{
    class Program
    {
        static HttpClient _client = new HttpClient();
        static User _user = new User();

        static void Main(string[] args)
        {
            //if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Count() <= 2)
            //{
            //    Process.Start("ClientTakeChat.exe");
            //}

            ValidatingUser().GetAwaiter().GetResult();
            StartWebSockets().GetAwaiter().GetResult();
        }

        static async Task<bool> CreateValidUser(User user)
        {
            var response = await _client.PostAsJsonAsync(
                "http://localhost:23047/api/users", user);
            response.EnsureSuccessStatusCode();

            // return URI of the created resource.
            return await response.Content.ReadAsAsync<bool>();
        }

        public static async Task ValidatingUser()
        {
            Console.WriteLine("Welcome to our chat server. Please provide a nickname:");
            _user.Name = Console.ReadLine();            

            while (!await CreateValidUser(_user))
            {
                Console.WriteLine($"Sorry, the nickname {_user.Name} is already taken. Please choose a different one:");
                _user.Name = Console.ReadLine();
            }            
        }        

        public static async Task StartWebSockets()
        {
            var socket = new ClientWebSocket();

            await socket.ConnectAsync(new Uri($"ws://localhost:23047/ws?user={_user.Name}"), CancellationToken.None);

            var welcomeMessage = Encoding.UTF8.GetBytes($"You are registered as [{_user.Name}]. Joining #general.");
            await socket.SendAsync(new ArraySegment<byte>(welcomeMessage), WebSocketMessageType.Text, true, CancellationToken.None);

            var send = Task.Run(async () =>
            {
                string message;

                while ((message = Console.ReadLine()) != null && message != string.Empty && !message.IsExit())
                {
                    var bytes = Encoding.UTF8.GetBytes(message.ToChatSocketMessage(_user.Name));
                    await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }

                var byMessage = Encoding.UTF8.GetBytes($"[{_user.Name}] disconnected!");
                await socket.SendAsync(new ArraySegment<byte>(byMessage), WebSocketMessageType.Text, true, CancellationToken.None);
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            });

            var receive = ReceiveAsync(socket);
            await Task.WhenAll(send, receive);
        }

        public static async Task ReceiveAsync(ClientWebSocket socket)
        {
            var buffer = new byte[1024 * 4];
            while (true)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var messageReceived = Encoding.UTF8.GetString(buffer, 0, result.Count);

                Console.WriteLine(messageReceived);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    var byMessage = Encoding.UTF8.GetBytes($"[{_user.Name}] disconnected!");
                    await socket.SendAsync(new ArraySegment<byte>(byMessage), WebSocketMessageType.Text, true, CancellationToken.None);
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }
    }

}
