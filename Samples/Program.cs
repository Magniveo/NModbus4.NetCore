using Modbus.Device;
using System;
using System.Net;
using System.Net.Sockets;

namespace Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 502);//for test
            var slaveTCP = ModbusSerialSlaveTcp.CreateTcp(1, listener);
            slaveTCP.ModbusSlaveRequestReceived += Slave_ModbusSlaveRequestReceived;
            Listen(slaveTCP);

            var slaveserTCP = ModbusTcpSlave.CreateTcp(1, listener);
            slaveserTCP.ModbusSlaveRequestReceived += Slave_ModbusSlaveRequestReceived;
            Console.ReadLine();
        }
        private static async void Slave_ModbusSlaveRequestReceived(object sender, ModbusSlaveRequestEventArgs e)
        { 
        }
        private static async void Listen(ModbusSerialSlaveTcp slaveTCP) 
        {
           await slaveTCP.ListenAsync();
        }
    }
}
