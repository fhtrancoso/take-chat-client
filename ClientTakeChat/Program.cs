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
        private const string WEBSOCKET_URI_STRING = "ws://localhost:5000/ws";
        private const string API_REST_REQUEST_URI = "http://localhost:5000/api/users";

        static HttpClient _client = new HttpClient();
        static User _user = new User();

        static void Main(string[] args)
        {
            ValidatingUser().GetAwaiter().GetResult();
            StartWebSockets().GetAwaiter().GetResult();
        }

        static async Task<bool> CreateValidUser(User user)
        {
            var response = await _client.PostAsJsonAsync(
                API_REST_REQUEST_URI, user);
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

            await socket.ConnectAsync(new Uri(WEBSOCKET_URI_STRING), CancellationToken.None);

            var welcomeMessage = Encoding.UTF8.GetBytes($"User [{_user.Name}] connected to the group.");
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
