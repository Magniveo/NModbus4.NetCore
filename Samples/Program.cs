using Modbus.Device;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Samples
{
    class Program
    {
        static Dictionary<ushort, TcpClient> liststationsactive;
        static void Main(string[] args)
        {
            //for (int j = 0; j < 56; j++)
            //{
            //    //Thread.Sleep(27000);
            //    TcpClient client = new TcpClient();
            //    client.Connect("127.0.0.1", 18610);
            //    Thread.Sleep(3000);
            //    var stream = client.GetStream();
            //    byte[] receivedBuffer = new byte[1024];
            //    stream.Read(receivedBuffer, 0, receivedBuffer.Length);
            //    byte[] answer = new byte[56];
            //    answer[0] = 111;
            //    answer[1] = 3;
            //    for (int i = 2; i < 13; i++)
            //    {
            //        Random rnd = new Random();
            //        answer[i] = (byte)rnd.Next(1, 4);
            //    }
            //    answer[35] = (byte)(14 & 0xFF);
            //    answer[36] = (byte)((14 >> 8) & 0xFF);
            //    answer[53] = (byte)((46 >> 8) & 0xFF);
            //    answer[54] = (byte)(46 & 0xFF);
            //    stream.Write(answer);
            //}
            //return;
            //liststationsactive = new Dictionary<ushort, TcpClient>();
            List<ModbusSlave> lstDevice = new List<ModbusSlave>();
            for (int i = 0; i < 10; i++) 
            {
                // here rtutcp
                TcpListener listener = new TcpListener(IPAddress.Any, 40022+i);//for test
                var slaveTCP = ModbusSerialSlaveTcp.CreateTcp(111, listener);
                slaveTCP.ModbusSlaveRequestReceived += Slave_ModbusSlaveRequestReceived;
                lstDevice.Add(slaveTCP);
            
                //here tcp
                var listenertcp = new TcpListener(IPAddress.Any, 50022 + i);//for test
                var slaveRTCP = ModbusTcpSlave.CreateTcp(112, listenertcp);
                slaveRTCP.ModbusSlaveRequestReceived += Slave_ModbusSlaveRequestReceived;
                lstDevice.Add(slaveRTCP);
            }
            StartListListen(lstDevice);
            ////SendRead42((ModbusSerialSlaveTcp)lstDevice[0]);
            Console.ReadLine();
            Thread.Sleep(3000);

        }
        private static async void Slave_ModbusSlaveRequestReceived(object sender, ModbusSlaveRequestEventArgs e)
        {
            if (e.Message.FunctionCode == 3) {
                switch (sender.GetType().ToString())
                {
                    case "Modbus.Device.ModbusSerialSlaveTcp":

                        ModbusSerialSlaveTcp slave = sender as ModbusSerialSlaveTcp;
                        //Random tick = new Random();
                        //await Task.Delay(tick.Next(1, 5));
                        //Thread.Sleep(tick.Next(1, 5));
                        //for (int i = 1; i < 43; i++)
                        //{
                        //    Random rnd = new Random();
                        //    slave.DataStore.HoldingRegisters[i] = (ushort)rnd.Next(1, 4);
                        //}
                        slave.DataStore = GenerateStation();
                        break;
                    case "Modbus.Device.ModbusTcpSlave":
                        ModbusTcpSlave slavetcp = sender as ModbusTcpSlave;
                        slavetcp.DataStore = GenerateStation();
                        //Random ticktcp = new Random();
                        //await Task.Delay(ticktcp.Next(1, 5));
                        //Thread.Sleep(ticktcp.Next(1, 5));
                        //for (int i = 1; i < 43; i++)
                        //{
                        //    Random rnd = new Random();
                        //    slavetcp.DataStore.HoldingRegisters[i] = (ushort)rnd.Next(1, 4);
                        //}
                        break;
                }
            }
            GC.Collect();
        }
        static float NextFloat(float min, float max)
        {
            Random random = new Random();
            double val = (random.NextDouble() * (max - min) + min);
            return (float)val;
        }
        static int NextInt(int min, int max)
        {
            Random random = new Random();
            int val = random.Next(min, max);
            return val;
        }
        private static Modbus.Data.DataStore GenerateStation() {
            var result = new Modbus.Data.DataStore();
            result.HoldingRegisters.Insert(1,(ushort)(NextFloat(5.0f,48.0f)*10));//voltage
            result.HoldingRegisters.Insert(2, (ushort)(NextFloat(5.0f, 30.0f) * 10));//current
            result.HoldingRegisters.Insert(3, (ushort)(NextFloat(-1.8f, -1.0f) ));//usp
            result.HoldingRegisters.Insert(4, (ushort)(NextFloat(215, 240)));//u220
            result.HoldingRegisters.Insert(5, (ushort)(NextFloat(5, 15)));//shim
            result.HoldingRegisters.Insert(6, (ushort)(NextFloat(-1.8f, 48.0f) ));//stabilization settings
            result.HoldingRegisters.Insert(7, (ushort)4238);//electric meter
            result.HoldingRegisters.Insert(8, 3200);//Coefficient
            result.HoldingRegisters.Insert(9, (ushort)5000);//svn1
            result.HoldingRegisters.Insert(10, (ushort)4000);//svn2
            result.HoldingRegisters.Insert(11, (ushort)NextInt(1,5));//faults
            result.HoldingRegisters.Insert(12, (ushort)1);//stationType
            result.HoldingRegisters.Insert(13, (ushort)1);//stationType ??
            result.HoldingRegisters.Insert(14, (ushort)2028);//setDateSoftware
            result.HoldingRegisters.Insert(15, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//uOutMax
            result.HoldingRegisters.Insert(16, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//iOutMax
            result.HoldingRegisters.Insert(17, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//temperature
            result.HoldingRegisters.Insert(18, (ushort)2028);//release date
            result.HoldingRegisters.Insert(19, (ushort)12345);//stationnumber
            result.HoldingRegisters.Insert(20, (ushort)NextFloat(-1.8f, -1.0f));//upp
            result.HoldingRegisters.Insert(21, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//breaker
            result.HoldingRegisters.Insert(22, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//date
            result.HoldingRegisters.Insert(23, (ushort)12345);//softwareversion
            result.HoldingRegisters.Insert(24, (ushort)1);//main
            result.HoldingRegisters.Insert(25, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(26, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(27, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(28, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(29, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(30, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(31, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(32, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(33, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(34, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(35, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(36, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(37, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(38, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(39, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(40, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(41, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(42, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            result.HoldingRegisters.Insert(43, (ushort)(NextFloat(-1.8f, -1.0f) * 10));//u220
            //result.HoldingRegisters[31] = (ushort)(NextFloat(-1.8f, -1.0f) * 10);//u220
            return result;
        }
        private static void StartListListen(List<ModbusSlave> lstDevice) 
        {
            //ModbusIpMaster ipMaster = new ModbusIpMaster();
            //ipMaster.ReadWriteMultipleRegisters
            foreach (var slv in lstDevice)
            {
                var strslv = slv.GetType().ToString();
                switch (slv.GetType().ToString()) 
                {
                    case "Modbus.Device.ModbusSerialSlaveTcp":
                        Listen((ModbusSerialSlaveTcp)slv);
                        break;
                    case "Modbus.Device.ModbusTcpSlave":
                        Listen((ModbusTcpSlave)slv);
                        break;
                }
            }    
        }
        private static async void Listen(ModbusSerialSlaveTcp slaveTCP) //no changing to task
        {
            try
            {
                await slaveTCP.ListenAsync();
            }
            catch (Exception ex) 
            {
                Thread.Sleep(10000);
                Listen(slaveTCP);
            }
        }
        private static async void Listen(ModbusTcpSlave slaveTCP)
        {
            await slaveTCP.ListenAsync();
        }
        private static async void SendRead42(ModbusSerialSlaveTcp slaveTCP) 
        {
            while (true) 
            {
                if (slaveTCP.Masters.Count > 0)
                {
                    foreach (var mstr in slaveTCP.Masters) 
                    {
                        Debug.WriteLine($"{DateTime.Now} conected device-{mstr.Client.RemoteEndPoint.ToString()} isconnected-{mstr.Connected}");
                        var master = ModbusSerialMaster.CreateRtu(mstr);
                        try
                        {
                            if (mstr.Connected)
                            {
                                var newCon = liststationsactive.FirstOrDefault(x => x.Value == mstr);
                                if(true)// (newCon.Key == null || newCon.Key <1) 
                                {


                                    var data = await master.ReadHoldingRegistersAsync(111, 0, 42);
                                    var station_number = data[25];
                                    Debug.WriteLine($"{data[25]} lp {mstr.Client.LocalEndPoint} rp {mstr.Client.RemoteEndPoint} station_number {station_number}");
                                    liststationsactive.Remove(station_number);
                                    liststationsactive.Add(station_number, mstr);
                                }
                            }
                            else 
                            {
                                liststationsactive.Remove(liststationsactive.FirstOrDefault(x=>x.Value == mstr).Key);
                            }
                            Thread.Sleep(12000);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                        //await master.ReadHoldingRegistersAsync();
                    }
                    //continue;
                }
                else 
                {
                
                }
                Thread.Sleep(12000);
                continue;
            }
        }
    }
}