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

            TcpListener listener2 = new TcpListener(IPAddress.Parse("127.0.0.1"), 503);//for test
            var slaveserTCP = ModbusTcpSlave.CreateTcp(1, listener2);
            slaveserTCP.ModbusSlaveRequestReceived += Slave_ModbusSlaveRequestReceived;

            //var slvRTU = ModbusSerialSlave.CreateRtu(1, listener2);
            Console.ReadLine();
        }
        private static async void Slave_ModbusSlaveRequestReceived(object sender, ModbusSlaveRequestEventArgs e)
        {
            ModbusSerialSlaveTcp slave = sender as ModbusSerialSlaveTcp;
            slave.DataStore.HoldingRegisters[1] = 1;
            slave.DataStore.HoldingRegisters[2] = 2;
        }
        private static async void Listen(ModbusSerialSlaveTcp slaveTCP) 
        {
           await slaveTCP.ListenAsync();
        }
        private static async void Listen(ModbusTcpSlave slaveTCP)
        {
            await slaveTCP.ListenAsync();
        }
    }
}
