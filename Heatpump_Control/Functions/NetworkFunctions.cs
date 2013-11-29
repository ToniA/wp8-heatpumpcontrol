using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using Microsoft.Phone.Net.NetworkInformation;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Networking;
using Windows.Storage.Streams;
using Renci.SshNet;
using System.IO;

namespace Heatpump_Control
{
    public class NetworkFunctions
    {
        //
        // SSID of the current WiFi connection
        // See http://stackoverflow.com/questions/13043423/how-to-get-the-name-of-wifi-connection-in-wp7
        //
        public static string FindWIFISSID()
        {
            foreach (var network in new NetworkInterfaceList())
            {
                if ((network.InterfaceType == NetworkInterfaceType.Wireless80211) && (network.InterfaceState == ConnectState.Connected))
                {
                    System.Diagnostics.Debug.WriteLine("Connected to " + network.InterfaceName);
                    return network.InterfaceName;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Not Wifi " + network.InterfaceName);
                }
            }

            return null;
        }

        //
        // Find my IP address
        //
        public static string FindIPAddress()
        {
            List<string> ipAddresses = new List<string>();
            var hostnames = NetworkInformation.GetHostNames();
            foreach (var hn in hostnames)
            {
                //IanaInterfaceType == 71 => Wifi
                //IanaInterfaceType == 6 => Ethernet (Emulator)
                if (hn.IPInformation != null &&
                    (hn.IPInformation.NetworkAdapter.IanaInterfaceType == 71
                    || hn.IPInformation.NetworkAdapter.IanaInterfaceType == 6))
                {
                    string ipAddress = hn.DisplayName;
                    ipAddresses.Add(ipAddress);
                }
            }

            if (ipAddresses.Count < 1)
            {
                return null;
            }
            else if (ipAddresses.Count == 1)
            {
                return ipAddresses[0];
            }
            else
            {
                //if multiple suitable address were found use the last one
                //(regularly the external interface of an emulated device)
                return ipAddresses[ipAddresses.Count - 1];
            }
        }

        //
        // Send the JSON using either UDP or SSH
        // * Use UDP if in the home network
        //
        public static void SendJson(string message)
        {
            BackgroundWorker SSHbackgroundWorker = new BackgroundWorker();

            if (FindWIFISSID() != null &&
                FindWIFISSID().Equals(App.ViewModel.settings.SSIDSetting))
            {
                SendUDP(message);
            }
            else
            {
                SSHbackgroundWorker.DoWork += new DoWorkEventHandler(SendSSH);
                SSHbackgroundWorker.WorkerReportsProgress = false;
                SSHbackgroundWorker.WorkerSupportsCancellation = false; 
                SSHbackgroundWorker.RunWorkerAsync(message);
            }
        }

        //
        // Send a command over UDP
        //
        public static void SendUDP(string message)
        {
            int port = 49722;

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetNetworkPreference(NetworkSelectionCharacteristics.NonCellular);

            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            socketEventArg.RemoteEndPoint = new IPEndPoint(IPAddress.Broadcast, port);
            socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
            {
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Connect:
                        System.Diagnostics.Debug.WriteLine("send complete");
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine("invalid op");
                        break;
                }

                var response = e.SocketError.ToString();
                System.Diagnostics.Debug.WriteLine("response: " + response);
            });

            // Add the data to be sent into the buffer
            byte[] payload = Encoding.UTF8.GetBytes(message);
            socketEventArg.SetBuffer(payload, 0, payload.Length);

            // Send the UDP broadcast
            socket.ConnectAsync(socketEventArg);
        }

        //
        // Send a command over SSH
        //
        public static void SendSSH(object sender, DoWorkEventArgs e)
        {
            SSHFunctions.SSHConnect();
            SSHFunctions.SSHExecute(String.Format(App.ViewModel.settings.UDPBroadcastSetting, (string)e.Argument));
            SSHFunctions.SSHDisconnect();
        }
    }
}