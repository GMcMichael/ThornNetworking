using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ThornNetworking.ThreadParameters;

namespace ThornNetworking
{
    public class NetworkingManager
    {
        //static variables
        public static long CurrentTime => DateTimeOffset.Now.ToUnixTimeMilliseconds();
        public static NetworkingManager Instance = new();
        private static ConcurrentQueue<Socket> RemoteConnections = new();
        public static readonly int DeltaTime = 8;
        public static readonly int TimeoutSeconds = 30;
        public static readonly int BufferSize = 256;//1024 is a KB
        public static readonly int maxConnections = 10;
        public static readonly int DefaultPort = 48888;
        public static readonly IPAddress DefaultIP;

        //Local variables
        private Socket LocalSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private object _remoteConnectionFlagLock = new object();
        private bool _remoteConnectionFlag = false;
        private bool RemoteConnectionsUpdated {
            get
            {
                lock (_remoteConnectionFlagLock)
                    return _remoteConnectionFlag;
            }
            set
            {
                lock (_remoteConnectionFlagLock)
                    _remoteConnectionFlag = value;
            }
        }
        public bool IsConnected { get; private set; } = false;
        private ConcurrentQueue<byte[]> SendBuffer = new();

        static NetworkingManager()
        {
            try { DefaultIP = GetLocalIP(); }
            catch { DefaultIP = new(Encoding.UTF8.GetBytes("127.0.0.1")); }
        }
        public NetworkingManager() {}

        public static IPAddress GetLocalIP()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip;
            }
            throw new Exception("No IP address detected");
        }

        public void StartHost(IPAddress? ip = null, int? port = null)
        {
            if (IsConnected) return;

            ip ??= DefaultIP;
            port ??= DefaultPort;

            //open socket
            LocalSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            LocalSocket.Bind(new IPEndPoint(ip, (int)port));
            LocalSocket.Listen(maxConnections);

            IsConnected = true;

            //define threads
            Thread acceptConnectionsThread = new(async (obj) =>
            {
                if (obj == null) return;
                BaseParameters bP = (BaseParameters)obj;

                while(true)
                {
                    var remoteConn = await LocalSocket.AcceptAsync(bP.token);

                    if(bP.token.IsCancellationRequested) return;///TODO: instead of return I should try catch all and: bP.token.ThrowIfCancellationRequested();

                    bool authentication = true;///TODO: Add authentication

                    if (authentication && remoteConn != null)
                    {
                        RemoteConnections.Enqueue(remoteConn);
                        RemoteConnectionsUpdated = true;
                    }
                }
            });
            Thread receivingThread = new((obj) =>
            {
                if (obj == null) return;
                SentryParameters sP = (SentryParameters)obj;
                Dictionary<Socket, (CancellationTokenSource, long)> sockets = new();

                void CancelSocket(Socket socket)
                {
                    try { socket.Shutdown(SocketShutdown.Both); }
                    catch { }
                    socket.Close();

                    ///TODO: Make this more efficent
                    List<Socket> remainingSockets = RemoteConnections.Where(sock => !sock.Equals(socket)).ToList();
                    RemoteConnections.Clear();
                    remainingSockets.ForEach(sock => RemoteConnections.Enqueue(sock));
                }

                void StartWait(Socket connectionSocket)
                {
                    sockets.Add(connectionSocket, (new(), CurrentTime));

                    EventHandler<SocketAsyncEventArgs> recieveFunc = null;
                    recieveFunc = (object? sender, SocketAsyncEventArgs e) =>
                    {
                        (Socket socket, CancellationToken token) = (Tuple<Socket, CancellationToken>)e.UserToken;
                        //update last received time
                        var currState = sockets[socket];
                        sockets[socket] = (currState.Item1, CurrentTime);

                        if (token.IsCancellationRequested) return;
                        ///TODO: ProcessData
                        Console.WriteLine($"Data Processed. Socket Status: {e.SocketError}, Num Bytes: {e.BytesTransferred}, Buffer: {e.Buffer}");

                        e.SetBuffer(new byte[BufferSize]);

                        if (!socket.ReceiveAsync(e)) recieveFunc?.Invoke(this, e);
                    };

                    using (SocketAsyncEventArgs recieveEventArgs = new())
                    {
                        recieveEventArgs.SetBuffer(new byte[BufferSize]);

                        recieveEventArgs.Completed += recieveFunc;
                        recieveEventArgs.UserToken = (connectionSocket, sockets[connectionSocket].Item1.Token);

                        if (!connectionSocket.ReceiveAsync(recieveEventArgs)) recieveFunc(this, recieveEventArgs);
                    }
                }

                new List<Socket>(RemoteConnections).ForEach(s => StartWait(s));
                while (true)
                {
                    if (!IsConnected || sP.token.IsCancellationRequested)
                    {
                        sockets.ToList().ForEach(p => p.Value.Item1.Cancel());
                        break;
                    }

                    if (RemoteConnectionsUpdated)
                    {
                        RemoteConnectionsUpdated = false;
                        var socketsList = sockets.ToList();
                        foreach (Socket socket in new List<Socket>(RemoteConnections))
                            if (!socketsList.Any(p => p.Key.Equals(socket)))
                                StartWait(socket);
                    }
                    else
                    {
                        long currTime = CurrentTime;
                        sockets.Where(p => (currTime - p.Value.Item2) > (TimeoutSeconds * 1000)).ToList().ForEach(p => CancelSocket(p.Key));
                        Thread.Sleep(DeltaTime);
                    }
                }

                //quit
                sP.cancellationTokenSource.Cancel();
                Socket? sock;
                while (RemoteConnections.TryDequeue(out sock)) CancelSocket(sock);
                
            });
            Thread sendingThread = new((obj) =>
            {
                if (obj == null) return;
                BaseParameters bP = (BaseParameters)obj;

                byte[]? data = null;
                while (IsConnected)
                {
                    if (bP.token.IsCancellationRequested) return;
                    if (SendBuffer.IsEmpty) Thread.Sleep(DeltaTime);
                    else
                    {
                        data = null;
                        if (SendBuffer.TryDequeue(out data) && data != null)
                        {
                            ArraySegment<byte> buffer = new(data);
                            //send data to each recipient, for now all
                            //new List<Socket>(RemoteConnections).ForEach(async s => await s.SendAsync(buffer, SocketFlags.None));
                            Console.WriteLine($"\nData: {data}");
                            Console.WriteLine($"Buffer: {buffer}\n");
                        }
                    }
                }
            });

            //start threads
            //_CancellationTokenSource = new();
            SentryParameters sentryParameters = new();//might not work, could pass callback lambda such as: () => { _CancellationTokenSource.Cancel(); };
            using(BaseParameters baseParameters = new(sentryParameters))
            {
                acceptConnectionsThread.Start(baseParameters);
                receivingThread.Start(baseParameters);
            }
            sendingThread.Start(sentryParameters);
        }
    }
}
