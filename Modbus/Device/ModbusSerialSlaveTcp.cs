﻿namespace Modbus.Device
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
#if TIMER
    using System.Timers;
#endif
    using IO;

    /// <summary>
    ///     Modbus TCP slave device.
    /// </summary>
    public class ModbusSerialSlaveTcp : ModbusSlave
    {
        private const int TimeWaitResponse = 1000;
        private readonly object _serverLock = new object();

        private readonly ConcurrentDictionary<string, ModbusMasterSerialTcpConnection> _masters =
            new ConcurrentDictionary<string, ModbusMasterSerialTcpConnection>();

        private TcpListener _server;
#if TIMER
        private Timer _timer;
#endif
        private ModbusSerialSlaveTcp(byte unitId, TcpListener tcpListener)
            : base(unitId, new EmptyTransport())
        {
            _server = tcpListener ?? throw new ArgumentNullException(nameof(tcpListener));
        }

#if TIMER
        private ModbusTcpSlave(byte unitId, TcpListener tcpListener, double timeInterval)
            : base(unitId, new EmptyTransport())
        {
            if (tcpListener == null)
            {
                throw new ArgumentNullException(nameof(tcpListener));
            }

            _server = tcpListener;
            _timer = new Timer(timeInterval);
            _timer.Elapsed += OnTimer;
            _timer.Enabled = true;
        }
#endif

        /// <summary>
        ///     Gets the Modbus TCP Masters connected to this Modbus TCP Slave.
        /// </summary>
        public ReadOnlyCollection<TcpClient> Masters
        {
            get
            {
                return new ReadOnlyCollection<TcpClient>(_masters.Values.Select(mc => mc.TcpClient).ToList());
            }
        }

        /// <summary>
        ///     Gets the server.
        /// </summary>
        /// <value>The server.</value>
        /// <remarks>
        ///     This property is not thread safe, it should only be consumed within a lock.
        /// </remarks>
        private TcpListener Server
        {
            get
            {
                if (_server == null)
                {
                    throw new ObjectDisposedException("Server");
                }

                return _server;
            }
        }

        /// <summary>
        ///     Modbus TCP slave factory method.
        /// </summary>
        public static ModbusSerialSlaveTcp CreateTcp(byte unitId, TcpListener tcpListener)
        {
            return new ModbusSerialSlaveTcp(unitId, tcpListener);
        }

#if TIMER
        /// <summary>
        ///     Creates ModbusTcpSlave with timer which polls connected clients every
        ///     <paramref name="pollInterval"/> milliseconds on that they are connected.
        /// </summary>
        public static ModbusSerialSlaveTcp CreateTcp(byte unitId, TcpListener tcpListener, double pollInterval)
        {
            return new ModbusSerialSlaveTcp(unitId, tcpListener, pollInterval);
        }
#endif

        /// <summary>
        ///     Start slave listening for requests.
        /// </summary>
        public override async Task ListenAsync()
        {
            Debug.WriteLine("Start Modbus Tcp Server.");
            // TODO: add state {stoped, listening} and check it before starting
            Server.Start();

            while (true)
            {
                TcpClient client = await Server.AcceptTcpClientAsync().ConfigureAwait(false);
                var masterConnection = new ModbusMasterSerialTcpConnection(client, this);
                masterConnection.ModbusMasterTcpConnectionClosed += OnMasterConnectionClosedHandler;
                _masters.TryAdd(client.Client.RemoteEndPoint.ToString(), masterConnection);
            }
        }
        /// <summary>
        ///     Start slave listening for requests with CancellationToken.
        /// </summary>
        public async Task ListenAsync(CancellationToken stoppingToken)
        {
            //var cancellation = new CancellationTokenSource();
            //await Task.Run(() => listener.AcceptTcpClientAsync(), cancellation.Token);

            //// somewhere in another thread
            //cancellation.Cancel();
            Debug.WriteLine("Start Modbus Tcp Server.");
            // TODO: add state {stoped, listening} and check it before starting
            Server.Start();

            while (true)
            {
                TcpClient client = await Server.AcceptTcpClientAsync().ConfigureAwait(false);
                var masterConnection = new ModbusMasterSerialTcpConnection(client, this);
                masterConnection.ModbusMasterTcpConnectionClosed += OnMasterConnectionClosedHandler;
                _masters.TryAdd(client.Client.RemoteEndPoint.ToString(), masterConnection);
            }
        }
        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
        ///     unmanaged resources.
        /// </param>
        /// <remarks>Dispose is thread-safe.</remarks>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // double-check locking
                if (_server != null)
                {
                    lock (_serverLock)
                    {
                        if (_server != null)
                        {
                            _server.Stop();
                            _server = null;

#if TIMER
                            if (_timer != null)
                            {
                                _timer.Dispose();
                                _timer = null;
                            }
#endif

                            foreach (var key in _masters.Keys)
                            {
                                if (_masters.TryRemove(key, out ModbusMasterSerialTcpConnection connection))
                                {
                                    connection.ModbusMasterTcpConnectionClosed -= OnMasterConnectionClosedHandler;
                                    connection.Dispose();
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool IsSocketConnected(Socket socket)
        {
            bool poll = socket.Poll(TimeWaitResponse, SelectMode.SelectRead);
            bool available = (socket.Available == 0);
            return poll && available;
        }

#if TIMER
        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            foreach (var master in _masters.ToList())
            {
                if (IsSocketConnected(master.Value.TcpClient.Client) == false)
                {
                    master.Value.Dispose();
                }
            }
        }
#endif
        private void OnMasterConnectionClosedHandler(object sender, TcpConnectionEventArgs e)
        {
            ModbusMasterSerialTcpConnection connection;

            if (!_masters.TryRemove(e.EndPoint, out connection))
            {
                string msg = $"EndPoint {e.EndPoint} cannot be removed, it does not exist.";
                throw new ArgumentException(msg);
            }

            Debug.WriteLine($"Removed Master {e.EndPoint}");
        }

        public static Message.IModbusMessage SlaveRequestApply(ModbusSlave slave, Message.IModbusMessage request)
        {
            return slave.ApplyRequest(request);
        }
        public int GetPortListener() 
        {

            return ((System.Net.IPEndPoint)_server.LocalEndpoint).Port;
        }
    }
}
