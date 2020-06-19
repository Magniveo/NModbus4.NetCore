using Modbus.Device;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            List<ModbusSlave> lstDevice = new List<ModbusSlave>();
            for (int i = 0; i < 1000; i++) 
            {
                TcpListener listener = new TcpListener(IPAddress.Any, 40022+i);//for test
                var slaveTCP = ModbusSerialSlaveTcp.CreateTcp(1, listener);
                slaveTCP.ModbusSlaveRequestReceived += Slave_ModbusSlaveRequestReceived;
                lstDevice.Add(slaveTCP);
            }
            StartListListen(lstDevice);
            Console.ReadLine();
        }
        private static async void Slave_ModbusSlaveRequestReceived(object sender, ModbusSlaveRequestEventArgs e)
        {
            ModbusSerialSlaveTcp slave = sender as ModbusSerialSlaveTcp;
            for (int i = 1; i < 43; i++) 
            {
                Random rnd = new Random();
                slave.DataStore.HoldingRegisters[i] =(ushort) rnd.Next(1,42);
            }
            GC.Collect();
        }
        private static void StartListListen(List<ModbusSlave> lstDevice) 
        {
            foreach (var slv in lstDevice)
                Listen((ModbusSerialSlaveTcp)slv);
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
