namespace Modbus.Device
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    using IO;
    using Message;

    /// <summary>
    /// Represents an incoming connection from a Modbus master. Contains the slave's logic to process the connection.
    /// </summary>
    internal class ModbusMasterSerialTcpConnection : ModbusDevice, IDisposable
    {
        private readonly TcpClient _client;
        private readonly string _endPoint;
        private readonly Stream _stream;
        private readonly ModbusSerialSlaveTcp _slave;
        private readonly Task _requestHandlerTask;

        private readonly byte[] _mbapHeader = new byte[6];
        private byte[] _messageFrame;
        private byte[] _crc;
        public ModbusMasterSerialTcpConnection(TcpClient client, ModbusSerialSlaveTcp slave)
            : base(new ModbusRtuTransport(new TcpClientAdapter(client)))
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (slave == null)
            {
                throw new ArgumentNullException(nameof(slave));
            }

            _client = client;
            _endPoint = client.Client.RemoteEndPoint.ToString();
            _stream = client.GetStream();
            _slave = slave;
            _requestHandlerTask = Task.Run((Func<Task>)HandleRequestAsync);
        }
        /// <summary>
        ///     Occurs when a Modbus master TCP connection is closed.
        /// </summary>
        public event EventHandler<TcpConnectionEventArgs> ModbusMasterTcpConnectionClosed;

        public string EndPoint
        {
            get { return _endPoint; }
        }

        public Stream Stream
        {
            get { return _stream; }
        }

        public TcpClient TcpClient
        {
            get { return _client; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }

            base.Dispose(disposing);
        }
        private ModbusSerialTransport SerialTransport
        {
            get
            {
                var transport = Transport as ModbusSerialTransport;

                if (transport == null)
                {
                    throw new ObjectDisposedException("SerialTransport");
                }

                return transport;
            }
        }
        private async Task HandleRequestAsync()
        {
            while (true)
            {
                byte[] frameStartRTU = new byte[7];// await Read(7);
                int readBytes = await Stream.ReadAsync(frameStartRTU, 0, 7).ConfigureAwait(false);
                if (readBytes == 0)
                {
                    Debug.WriteLine($"0 bytes read, Master at {EndPoint} has closed Socket connection.");
                    ModbusMasterTcpConnectionClosed?.Invoke(this, new TcpConnectionEventArgs(EndPoint));
                    return;
                }
                var countEndRTU = ModbusRtuTransport.RequestBytesToRead(frameStartRTU);
                byte[] frameEndRTU = new byte[countEndRTU];
                int endBytes = await Stream.ReadAsync(frameEndRTU, 0, countEndRTU).ConfigureAwait(false);// await Read(ModbusRtuTransport.RequestBytesToRead(frameStartRTU));
                if (endBytes == 0)
                {
                    Debug.WriteLine($"0 bytes read, Master at {EndPoint} has closed Socket connection.");
                    ModbusMasterTcpConnectionClosed?.Invoke(this, new TcpConnectionEventArgs(EndPoint));
                    return;
                }
                byte[] frameRTU = Enumerable.Concat(frameStartRTU, frameEndRTU).ToArray();

                Debug.WriteLine($"Begin reading header from Master at IP: {EndPoint}");

                Debug.WriteLine($"Master at {EndPoint} sent header: \"{string.Join(", ", frameStartRTU)}\" with {endBytes} bytes in PDU");

                Debug.WriteLine($"Read frame from Master at {EndPoint} completed {readBytes} bytes");
                Debug.WriteLine($"RX from Master at {EndPoint}: {string.Join(", ", frameRTU)}");
                
                var request = ModbusMessageFactory.CreateModbusRequest(frameRTU);
                request.TransactionId = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(frameRTU, 0));

                // perform action and build response
                IModbusMessage response = _slave.ApplyRequest(request);
                response.TransactionId = request.TransactionId;
                // write response
                byte[] responseFrame = Transport.BuildMessageFrame(response);

                Debug.WriteLine($"TX to Master at {EndPoint}: {string.Join(", ", responseFrame)}");
                await Stream.WriteAsync(responseFrame, 0, responseFrame.Length).ConfigureAwait(false);
            }
        }
        public async Task<byte[]> Read(int count)
        {
            byte[] frameBytes = new byte[count];
            int numBytesRead = 0;

            while (numBytesRead != count)
            {
                numBytesRead += await Stream.ReadAsync(frameBytes, numBytesRead, count - numBytesRead);
            }

            return frameBytes;
        }
    }
}
