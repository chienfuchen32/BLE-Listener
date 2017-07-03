using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Linq;

namespace ble
{
    class Program
    {
        static List<List<string>> recvData = new List<List<string>>();
        static List<string> outputData = new List<string>();
        static List<List<string>> outputDataN = new List<List<string>>();
        static textLogger logger = new textLogger();
        static Dictionary<string, deviceData.deviceInfo> deviceList = new Dictionary<string, deviceData.deviceInfo>();
        static string localBDAddr;
        static void Main(string[] args)
        {
            Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " System Ready To Run......0.5");

            string appPath = Directory.GetCurrentDirectory();
            logger.logFilename = "debug";
            logger.logPathname = appPath;
            logger.logMode = "D";
            logger.expireDays = 7;
            logger.initLogger();


            Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " Log File Path......" + logger.logPathname + "\\" + logger.logFilename);

            Thread.Sleep(1000);
            Thread socketServerThread = new Thread(socketServer);
            socketServerThread.IsBackground = true;
            //socketServerThread.Start();//****

            Thread.Sleep(1000);
            Thread socketClientThread = new Thread(connectToRemoteBT);
            socketClientThread.IsBackground = true;
            //socketClientThread.Start();

            Thread.Sleep(1000);
            Thread getLocalBDAddrThread = new Thread(getLocalBDAddr);
            getLocalBDAddrThread.IsBackground = true;
            getLocalBDAddrThread.Start();

            Thread.Sleep(1000);
            Thread bleScanThread = new Thread(runLEScanCommand);
            bleScanThread.IsBackground = true;
            bleScanThread.Start();

            Thread.Sleep(1000);
            Thread btMonThread = new Thread(btMon);
            btMonThread.IsBackground = true;
            btMonThread.Start();

            Thread.Sleep(1000);
            Thread parseThread = new Thread(parseData);
            parseThread.IsBackground = true;
            parseThread.Start();

            Thread.Sleep(1000);
            Thread sendBleStatusThread = new Thread(sendBleStatus);
            sendBleStatusThread.IsBackground = true;
            sendBleStatusThread.Start();

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        private static void socketServer()
        {
            //serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " Socket Server Started  ");
            try
            {
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, 10001));

                serverSocket.Listen(5);

                Socket oneSocket = serverSocket.Accept();
                Thread sThread = new Thread(new ParameterizedThreadStart(ProcessConnectClient));
                sThread.IsBackground = true;
                sThread.Start(oneSocket);


            }
            catch (Exception wb)
            {
                serverSocket.Close();


            }
        }

        private static void ProcessConnectClient(object x)
        {
            Socket oneSocket = (Socket)x;
            byte[] buf = new byte[1024];
            string[] rData = new string[3];
            byte[] TrimedRecvData;
            byte[] sData = new byte[2];
            sData[0] = 0x2a;

            try
            {
                String remoteEndIPAdress = oneSocket.RemoteEndPoint.ToString();
                Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " " + remoteEndIPAdress + " Connected");
                Thread.Sleep(5);

                while (true)
                {
                    if (outputDataN.Count > 0)
                    {
                        Thread.Sleep(1);
                        Monitor.Enter(outputDataN);

                        oneSocket.Send(System.Text.Encoding.Default.GetBytes(JsonConvert.SerializeObject(outputDataN[0])));

                        outputDataN.RemoveAt(0);

                        Monitor.Exit(outputDataN);
                    }
                }

            }
            catch (Exception pc)
            {
                oneSocket.Close();
                Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " Socket Error ");
            }

        }

        private static void connectToRemoteBT()
        {
            byte[] RecvBuffer = new byte[1024];
            byte[] trimedRecvBuffer;
            int nBytes;
            string tmpString;

            //Socket oneSocket = (Socket)X;
            Socket oneSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint remoteServer = new IPEndPoint(IPAddress.Parse("10.100.82.191"), 10001);

            // initial socket            
            try
            {
                // connect to server
                Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " Try To Connect Remote");

                oneSocket.Connect(remoteServer);

                nBytes = oneSocket.Receive(RecvBuffer, 10240, SocketFlags.None);

                // Waiting data in block mode

                while (oneSocket.Connected == true)
                {
                    Thread.Sleep(50);
                    trimedRecvBuffer = new byte[nBytes];
                    Buffer.BlockCopy(RecvBuffer, 0, trimedRecvBuffer, 0, nBytes);

                    //List<byte[]> tokenedData = new List<byte[]>();
                    string str = System.Text.Encoding.Default.GetString(trimedRecvBuffer);

                    Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " Recv = " + str);

                }

            }
            catch (Exception ex)
            {

            }
            finally
            {

            }

        }

        private static void runBtmon()
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = "btmon",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            proc.Start();

            new Thread(() => ReadOutputThread(proc.StandardOutput)).Start();
            //new Thread(() => ReadOutputThread(proc.StandardError)).Start();

            while (true)
            {
                Console.Write(" btmon >> ");
                //var line = Console.ReadLine();
                string line = proc.StandardOutput.ReadToEnd();
                proc.StandardInput.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " " + line);
            }
        }

        static void runLEScanCommand()
        {
           
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = "hcitool lescan",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            proc.Start();

            new Thread(() => ReadOutputThread(proc.StandardOutput)).Start();
            new Thread(() => ReadOutputThread(proc.StandardError)).Start();

            while (true)
            {
                //Console.Write(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " lescan >>> ");
                string line = Console.ReadLine();
                //string line = proc.StandardOutput.ReadToEnd();
                if (line.Length > 0)
                {
                    proc.StandardInput.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " -> " + line);
                }
                Thread.Sleep(1);
            }
        }
        private static void ReadOutputThread(StreamReader streamReader)
        {
            string line = "";
            while (true)
            {
                line = streamReader.ReadLine();
                if(line !=null)
                {
                    //Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " -> " + line);
                }
                Thread.Sleep(1);
            }
        }

        private static void btMon()
        {
            string line = "";
            List<string> deviceData = new List<string>();
            Process proc = new Process();
            proc.StartInfo.FileName = "sudo";
            proc.StartInfo.Arguments = "btmon";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;

            Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " try to run btmon");
            Thread.Sleep(1000);
            proc.Start();
            Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " btmon running");
            line = proc.StandardOutput.ReadLine();
            Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " " + line);
            try
            {
                while (!proc.StandardOutput.EndOfStream)
                {
                    line = proc.StandardOutput.ReadLine();
                    //// do something with line
                    if (!String.IsNullOrEmpty(line))
                    {
                        if ((line.Trim().Substring(0, 1) != "=") && (line.Trim().Substring(0, 4) != "Blue"))//20170105從 || 改成 &&
                        {
                            //Console.Out.WriteLine("line: " +line);
                            //logger.write_txt_log(line);
                            string[] strindData = line.Trim().Split(' ');
                            //Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " strindData[0] = " + strindData[0]);
                            switch (strindData[0].Trim())
                            {
                                case ">":
                                    List<string> copy = new List<string>();
                                    for (int i_d = 0; i_d < deviceData.Count; i_d++) {
                                        copy.Add(deviceData[i_d]);
                                        //Console.Out.WriteLine(deviceData[i_d]);
                                    }
                                    Monitor.Enter(recvData);
                                    recvData.Add(copy);
                                    Monitor.Exit(recvData);
                                    //Thread.Sleep(10);
                                    deviceData.Clear();
                                    //deviceData.Add(line.Trim().Replace('>',' '));
                                    break;
                                default:
                                    deviceData.Add(line.Trim());
                                    break;
                            }
                        }
                        else
                        {
                            logger.write_txt_log("error log " + line);
                        }
                    }
                    Thread.Sleep(1);
                }
            }
            catch(Exception err)
            {
                Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " BT Mon Error ");
                Console.Out.WriteLine(err.ToString());
            }
        }

        private static void parseData()
        {

            while (true)
            {
                if (recvData.Count > 0)
                {
                    //Console.Out.WriteLine(recvData.Count.ToString());
                    Monitor.Enter(recvData);
                    //Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " data = " + JsonConvert.SerializeObject(recvData[0]) + " , Count = " + recvData[0].Count.ToString());
                    if (recvData[0].Count > 0)
                    {
                        //Monitor.Enter(outputDataN);
                        //try
                        //{
                        //    outputDataN.Add(recvData[0]);
                        //    //Console.Out.WriteLine("outputDataN Add  " + JsonConvert.SerializeObject(recvData[0]));
                        //}
                        //catch (Exception err)
                        //{
                        //    //Console.Out.WriteLine("outputDataN Add Error " + err.ToString());

                        //}
                        //Monitor.Exit(outputDataN);
                        deviceData.deviceInfo theDevice = new deviceData.deviceInfo();

                        try
                        {
                            for (int d = 0; d < recvData[0].Count; d++)
                            {
                                string header = null;
                                try
                                {
                                    if (!String.IsNullOrEmpty(recvData[0][d]))
                                    {
                                        if (!String.IsNullOrEmpty(recvData[0][d].Trim().Split(' ')[0]))
                                        {
                                            header = recvData[0][d].Trim().Split(' ')[0].Trim();
                                            switch (header)
                                            {
                                                case "Address:":
                                                    if (String.IsNullOrEmpty(recvData[0][d].Trim().Split(':')[1]))
                                                    {
                                                        Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " Address");
                                                    }
                                                    //theDevice.address = recvData[0][d].Trim().Split(' ')[1].Split(' ')[0].Trim();
                                                    theDevice.address = recvData[0][d].Trim().Split(' ')[1].Trim().Split(' ')[0];
                                                    break;
                                                case "Address":
                                                    if (String.IsNullOrEmpty(recvData[0][d].Trim().Split(':')[1]))
                                                    {
                                                        Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " Address Type");
                                                    }
                                                    //theDevice.addressType = recvData[0][d].Trim().Split(':')[1].Split(' ')[0];
                                                    theDevice.addressType = recvData[0][d].Trim().Split(':')[1].Trim().Split(' ')[0];
                                                    break;
                                                case "Type:":
                                                    if (String.IsNullOrEmpty(recvData[0][d].Trim().Split(':')[1]))
                                                    {
                                                        Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " Type");
                                                    }
                                                    //theDevice.type = recvData[0][d].Trim().Split(':')[1].Trim();
                                                    theDevice.type = recvData[0][d].Trim().Split(':')[1].Trim().Split(' ')[0];
                                                    break;
                                                case "Company:":
                                                    if (String.IsNullOrEmpty(recvData[0][d].Trim().Split(':')[1]))
                                                    {
                                                        Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " Company");
                                                    }
                                                    //theDevice.company = recvData[0][d].Trim().Split(':')[1].Trim();
                                                    theDevice.company = recvData[0][d].Trim().Split(':')[1].Trim().Split(' ')[0];
                                                    break;
                                                case "Name":
                                                    if (String.IsNullOrEmpty(recvData[0][d].Trim().Split(':')[1]))
                                                    {
                                                        Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " Name");
                                                    }
                                                    theDevice.name = recvData[0][d].Trim().Split(':')[1].Trim();
                                                    break;
                                                case "TX":
                                                    if (String.IsNullOrEmpty(recvData[0][d].Trim().Split(':')[1]))
                                                    {
                                                        Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " Tx");
                                                    }
                                                    theDevice.txPower = recvData[0][d].Trim().Split(':')[1].Trim().Split(' ')[0];
                                                    break;
                                                case "RSSI:":
                                                    if (String.IsNullOrEmpty(recvData[0][d].Trim().Split(':')[1]))
                                                    {
                                                        Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " RSSI");
                                                    }
                                                    theDevice.rssi = recvData[0][d].Trim().Split(':')[1].Trim().Split(' ')[0];
                                                    break;
                                                case "UUID:":
                                                    break;
                                                case "Version:":
                                                    break;
                                                default:
                                                    //Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " header = " + header + " , Count = " + recvData[0].Count.ToString());
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            Console.Out.WriteLine("NULL type2");
                                            string responseData = JsonConvert.SerializeObject(recvData);
                                            Console.Out.WriteLine(responseData);
                                        }
                                    }
                                    else
                                    {
                                        Console.Out.WriteLine("NULL type1");
                                        string responseData = JsonConvert.SerializeObject(recvData);
                                        Console.Out.WriteLine(responseData);
                                    }
                                }
                                catch (Exception err)
                                {
                                    Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " err handle = " + recvData[0][d].Trim() + " , Count = " + recvData[0].Count.ToString());
                                    Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " err handle = " + err.ToString()  + "|| " + recvData[0][d]);

                                    string responseData = JsonConvert.SerializeObject(recvData);
                                    Console.Out.WriteLine(responseData);
                                    //break;//****
                                }
                            }
                        }
                        catch(Exception err)
                        {
                            Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " P = " + err.ToString());
                        }
                        //if (theDevice.name != null)
                        //{
                        //    Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ", address type = " + theDevice.addressType + ", address = " + theDevice.address + ", company = " + theDevice.company + ", name = \"" + theDevice.name + "\", type = " + theDevice.type + ", rssi = " + theDevice.rssi + ", tx power" + theDevice.txPower);
                        //}
                        //Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ", address type = " + theDevice.addressType + ", address = " + theDevice.address + ", company = " + theDevice.company + ", name = " + theDevice.name + ", type = " + theDevice.type + ", rssi = " + theDevice.rssi + ", tx power" + theDevice.txPower);

                        theDevice.datetime_nearby = DateTime.Now;//****
                        deviceData.deviceInfo nDevice;
                        try
                        {
                            if (theDevice.address != null)
                            {
                                if (deviceList.TryGetValue(theDevice.address, out nDevice) == true)
                                {
                                    //if (theDevice.deviceName != null && theDevice.deviceName != "")
                                    //{
                                    //    nDevice.deviceName = theDevice.deviceName;
                                    //}
                                    if (theDevice.address != null && theDevice.address != "")
                                    {
                                        nDevice.address = theDevice.address;
                                    }
                                    if (theDevice.addressType != null && theDevice.addressType != "")
                                    {
                                        nDevice.addressType = theDevice.addressType;
                                    }
                                    if (theDevice.type != null && theDevice.type != "")
                                    {
                                        nDevice.type = theDevice.type;
                                    }
                                    if (theDevice.company != null && theDevice.company != "")
                                    {
                                        nDevice.company = theDevice.company;
                                    }
                                    if (theDevice.name != null && theDevice.name != "")
                                    {
                                        nDevice.name = theDevice.name;
                                    }
                                    if (theDevice.txPower != null && theDevice.txPower != "")
                                    {
                                        nDevice.txPower = theDevice.txPower;
                                    }
                                    if (theDevice.rssi != null && theDevice.rssi != "")
                                    {
                                        nDevice.rssi = theDevice.rssi;
                                    }
                                    nDevice.datetime_nearby = theDevice.datetime_nearby;//****
                                }
                                else
                                {
                                    try
                                    {
                                        deviceList.Add(theDevice.address, theDevice);
                                        //Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " new device : " + theDevice.address + " , Count = " + deviceList.Count.ToString());
                                    }
                                    catch (Exception err)
                                    {
                                        Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " error : " + err.ToString() + " , Count = " + deviceList.Count.ToString());
                                    }
                                }
                            }
                        }
                        catch(Exception err)
                        {
                            Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " Parse Phase 2 error : " + err.ToString());
                        }
                        //foreach (KeyValuePair<string, deviceData.deviceInfo> item in deviceList)
                        //{
                        //    Console.Out.WriteLine(string.Format("{0} , {1}, {2}", item.Key, item.Value.name, item.Value.datetime_nearby.ToString("hh:mm:ss")));
                        //    Console.Out.WriteLine(string.Format("{0}", deviceList.Count.ToString()));
                        //}
                        //recvData.RemoveAt(0);//****
                    }
                    recvData.RemoveAt(0);//****
                    //recvData.RemoveAt(0);
                    //break;
                    Monitor.Exit(recvData);
                }
                if (recvData.Count >= 10)
                {
                    Console.Out.WriteLine("recvData count: " + recvData.Count.ToString());
                }
                Thread.Sleep(1);
            }
        }

        private static void getLocalBDAddr()
        {
            //localBDAddr
            string line = "";
            Process proc = new Process();
            proc.StartInfo.FileName = "sudo";
            proc.StartInfo.Arguments = "hcitool dev";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            Thread.Sleep(1000);
            proc.Start();
            try
            {
                while (!proc.StandardOutput.EndOfStream)
                {
                    line = proc.StandardOutput.ReadLine();
                    //// do something with line
                    if (!String.IsNullOrEmpty(line))
                    {
                        string header = null;
                        try
                        {
                            if (!String.IsNullOrEmpty(line.Trim().Split(' ')[0]))
                            {
                                header = line.Trim().Split(' ')[0].Trim().Split('\t')[0] ;
                                if (header.IndexOf("hci0") != -1)
                                {
                                    localBDAddr = line.Trim().Split(' ')[0].Trim().Split('\t')[1];
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " hci0 err handle = " + err.ToString() + "|| " + line);
                        }
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception err)
            {
                Console.Out.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " getLocalBDAddr Error ");
                Console.Out.WriteLine(err.ToString());
            }
        }

        private static void sendBleStatus()
        {
            while (true)
            {
                try
                {
                    Monitor.Exit(deviceList);
                    DateTime DateTime_Now = DateTime.Now;
                    foreach (var item in deviceList.Where(device => DateTime_Now.Subtract(device.Value.datetime_nearby).TotalMinutes > 1).ToList())
                    {
                        deviceList.Remove(item.Key);//http://stackoverflow.com/questions/1636885/remove-item-in-dictionary-based-on-value
                    }
                    Console.Out.WriteLine("deviceList.Count: " + deviceList.Count.ToString());
                    if (deviceList.Count != 0)
                    {
                        object[] ble_list = new object[deviceList.Count];
                        int count = 0;
                        foreach (KeyValuePair<string, deviceData.deviceInfo> item in deviceList)
                        {
                            //Console.Out.WriteLine(string.Format("{0} , {1}, {2}", item.Key, item.Value.name, item.Value.datetime_nearby.ToString("hh:mm:ss")));
                            //Console.Out.WriteLine(string.Format("{0}", deviceList.Count.ToString()));
                            ble_list[count] = new
                            {
                                addr_type = item.Value.addressType,
                                bd_addr = item.Value.address,
                                type = item.Value.type,
                                company = item.Value.company,
                                name = item.Value.name,
                                tx_power = item.Value.txPower,
                                rssi = item.Value.rssi,
                                datetime = item.Value.datetime_nearby.ToString("yyyy-MM-dd hh:mm:ss")
                            };
                            count++;
                        }
                        object obj = new { s_bd_addr = localBDAddr, ble_list = ble_list };
                        string json = JsonConvert.SerializeObject(obj);
                        //Console.Out.WriteLine(json);
                        string url = "http://10.100.82.52:3000/ble/update_info";
                        string result = "";
                        HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                        request.Method = "POST";
                        request.ContentType = "application/json;charset=UTF-8";
                        request.Accept = "Accept=application/json";
                        request.SendChunked = false;
                        byte[] bs = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
                        request.ContentLength = bs.Length;
                        request.Timeout = 5000;
                        using (Stream st = request.GetRequestStream())
                        {
                            st.Write(bs, 0, bs.Length);
                        }
                        //theResponseSetUserLevel response = new theResponseSetUserLevel();
                        using (WebResponse response1 = request.GetResponse())
                        {
                            StreamReader sr1 = new StreamReader(response1.GetResponseStream());
                            result = sr1.ReadToEnd();
                            Console.Out.WriteLine(result);
                            sr1.Close();
                            //response = JsonConvert.DeserializeObject<theResponseSetUserLevel>(result);
                        }
                        ////responseData = JsonConvert.SerializeObject(response);
                    }
                    Monitor.Enter(deviceList);
                }
                catch (Exception err)
                {
                    Console.Out.WriteLine("http failed: " + err.ToString());
                }
                Thread.Sleep(5000);//10 sec
            }
        }
    }
}
