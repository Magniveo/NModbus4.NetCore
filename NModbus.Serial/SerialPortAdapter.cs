using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using NModbus.IO;

namespace NModbus.Serial
{
    /// <summary>
    ///     Concrete Implementor - http://en.wikipedia.org/wiki/Bridge_Pattern
    /// </summary>
    public class SerialPortAdapter : IStreamResource
    {
        private const string NewLine = "\r\n";
        private SerialPort _serialPort;

        public SerialPortAdapter(SerialPort serialPort)
        {
            Debug.Assert(serialPort != null, "Argument serialPort cannot be null.");

            _serialPort = serialPort;
            _serialPort.NewLine = NewLine;
        }

        public int InfiniteTimeout
        {
            get { return SerialPort.InfiniteTimeout; }
        }

        public int ReadTimeout
        {
            get { return _serialPort.ReadTimeout; }
            set { _serialPort.ReadTimeout = value; }
        }

        public int WriteTimeout
        {
            get { return _serialPort.WriteTimeout; }
            set { _serialPort.WriteTimeout = value; }
        }

        public void DiscardInBuffer()
        {
            _serialPort.DiscardInBuffer();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            //var bytesRead =  ReadAsync(buffer, offset, count,_serialPort.ReadTimeout).Result;
            //return bytesRead;
            //Task.Run(() => {Thread.Sleep(100) ;});
            //return _serialPort.BaseStream.Read(buffer, offset, count);
            //return (int)_serialPort.BaseStream?.Read(buffer, offset, count);
            //_serialPort.BaseStream.BeginRead(buffer, offset, count)
            return _serialPort.BaseStream.Read(buffer, offset, count);
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            await Task.Delay(50);
            CancellationTokenSource cts = new CancellationTokenSource(_serialPort.ReadTimeout);
            //var task = await _serialPort.BaseStream.ReadAsync(buffer, offset, count);
            Task<int> readTask = Task.Run(async () =>
            {
                return await _serialPort.BaseStream.ReadAsync(buffer, offset, count, cts.Token);
            }, cts.Token);

            Task completedTask = await Task.WhenAny(readTask, Task.Delay(_serialPort.ReadTimeout, cts.Token));
            if (completedTask == readTask)
            {
                return await readTask;
            }
            else
            {
                cts.Cancel();
                throw new TimeoutException("The read operation has timed out.");
            }

        }
        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, int timeoutMilliseconds = 1000)
        {
            await Task.Delay(18);
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(timeoutMilliseconds); // Set the timeout

            try
            {
                // Start a task to read from the serial port
                var readTask = _serialPort.BaseStream.ReadAsync(buffer, offset, count, cts.Token);

                // Wait for the task to complete or the cancellation token to be signaled
                await Task.WhenAny(readTask, Task.Delay(-1, cts.Token));

                // If cancellation was requested, throw an exception
                cts.Token.ThrowIfCancellationRequested();

                // Otherwise, return the number of bytes read
                var bytesRead  = await readTask;

                //await _serialPort.BaseStream.FlushAsync();
                return bytesRead;
            }
            catch (OperationCanceledException)
            {
                // Handle the timeout
                throw new TimeoutException("Serial port read operation timed out.");
            }
        }
        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, TimeSpan timeout)
        {
            CancellationTokenSource cts = new CancellationTokenSource(timeout);

            try
            {
                Task<int> readTask = Task.Run(async () =>
                {
                    return await _serialPort.BaseStream.ReadAsync(buffer, offset, count, cts.Token);
                }, cts.Token);

                Task completedTask = await Task.WhenAny(readTask, Task.Delay(timeout, cts.Token));

                if (completedTask == readTask)
                {
                    return await readTask;
                }
                else
                {
                    cts.Cancel();
                    throw new TimeoutException("The read operation has timed out.");
                }
            }
            finally
            {
                cts.Dispose();
            }
        }
        public void Write(byte[] buffer, int offset, int count)
        {
            //_serialPort.BaseStream.Write(buffer, offset, count);
            //_serialPort.BaseStream.Flush();
            _serialPort.BaseStream.Write(buffer, offset, count);
            //_serialPort.BaseStream.WriteAsync(buffer, offset, count);
            //await _serialPort.BaseStream.FlushAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _serialPort?.Dispose();
                _serialPort = null;
            }
        }
    }
}
