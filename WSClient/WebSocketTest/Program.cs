using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketTest
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static Dictionary<int, ClientWebSocket> _sockets;

        static async Task MainAsync(string[] args)
        {
            const string SERVER_URL = "ws://localhost:1337";
            const string PROTOCOL = "zap-test";
            const int MESSAGE_INTERVAL_MS = 2000;
            var mitigationInterval = TimeSpan.FromSeconds(10);

            _sockets = new Dictionary<int, ClientWebSocket>();

            for (var i=0; i<2; i++)
            {
                var socket = new ClientWebSocket();
                _sockets.Add(i, socket);

                socket.Options.AddSubProtocol(PROTOCOL);
                // Aggressive keep alive to trigger issue faster!
                socket.Options.KeepAliveInterval = TimeSpan.FromMilliseconds(1); 
            }

            await Task.WhenAll(_sockets.Select(kvp =>
            {
                var socket = kvp.Value;
                Console.WriteLine("Connecting client " + kvp.Key);
                return socket.ConnectAsync(new Uri(SERVER_URL), CancellationToken.None);
            }));

            Console.WriteLine("##### All clients connected, starting message tasks...");

            foreach (var kvpp in _sockets)
            {
                var socket = kvpp.Value;
                var key = kvpp.Key;

                DateTime? sendStarted = null;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                // Task for detecting a struck SendAsync call
                Task.Run(async () =>
                {
                    while(true)
                    {
                        if (sendStarted != null)
                        {
                            var sendDuration = DateTime.UtcNow - sendStarted.Value;
                            if (sendDuration > mitigationInterval)
                            {
                                Console.WriteLine($"SOCKET {key} SendAsync stuck for {Math.Floor(sendDuration.TotalSeconds)}s");
                            }
                        }
                        await Task.Delay(mitigationInterval);
                    }
                });

                // Task to periodically send messages over the socket
                Task.Run(async () =>
                {
                    while (true)
                    {
                        using (var cancellationTokenSource = new CancellationTokenSource(mitigationInterval))
                        {
                            var message = $"SOCKET {key} {Guid.NewGuid().ToString()}";
                            var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                            sendStarted = DateTime.UtcNow;

                            Console.WriteLine($"SOCKET {key} Sending socket: {message}");
                            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationTokenSource.Token);
                            Console.WriteLine($"SOCKET {key} Send complete");

                            sendStarted = null;
                        }

                        var buffer = new ArraySegment<byte>(new byte[65536]);
                        var result = await socket.ReceiveAsync(buffer, CancellationToken.None);

                        Console.WriteLine($"SOCKET {key} Received: {Encoding.UTF8.GetString(buffer.Array, 0, result.Count)}");

                        await Task.Delay(MESSAGE_INTERVAL_MS);
                    }
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            await Task.Delay(Timeout.Infinite);
        }
    }
}
