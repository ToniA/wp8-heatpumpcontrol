using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
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
        private const int UDPPort = 49722;

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
        public static IPAddress FindMyIPAddress()
        {
            List<string> ipAddresses = new List<string>();
            var hostnames = NetworkInformation.GetHostNames();
            foreach (HostName hn in hostnames)
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
                return IPAddress.Parse(ipAddresses[0]);
            }
            else
            {
                //if multiple suitable address were found use the last one
                //(regularly the external interface of an emulated device)
                return IPAddress.Parse(ipAddresses[ipAddresses.Count - 1]);
            }
        }

        public static Boolean HomeWifiConnected()
        {
            if (FindWIFISSID() != null &&
                FindWIFISSID().Equals(App.ViewModel.settings.SSIDSetting))
            {
                return true;
            }

            return false;
        }

        // Send the 'identify' command as Json over the network
        public static void SendHeatpumpIdentify(HeatPumpIdentifyCommand heatpumpIdentify)
        {
            if (HomeWifiConnected())
            {
                heatpumpIdentify.channel = null;
            }

            SendJson(heatpumpIdentify);
        }

        // Send the 'command' command as Json over the network
        public static void SendHeatpumpCommand(HeatPumpStateCommand heatpumpCommand)
        {
            if (HomeWifiConnected())
            {
                heatpumpCommand.channel = null;
            }

            SendJson(heatpumpCommand);
        }

        //
        // Send the JSON using either UDP or SSH
        // * Use UDP if in the home network
        //
        private static void SendJson(object heatpumpCommand)
        {
            // Serialize the command to JSON
            string Json = JsonFunctions.SerializeToJsonString(heatpumpCommand);
            System.Diagnostics.Debug.WriteLine("JSON to send: " + Json);

            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = false;
            backgroundWorker.WorkerSupportsCancellation = false; 

            if (HomeWifiConnected())
            {
                backgroundWorker.DoWork += new DoWorkEventHandler(SendUDP);
            }
            else
            {
                backgroundWorker.DoWork += new DoWorkEventHandler(SendSSH);
            }
            backgroundWorker.RunWorkerAsync(Json);
        }

        //
        // Send a command over UDP
        //
        public static void SendUDP(object sender, DoWorkEventArgs ea)
        {
            const int MAX_RECVBUFFER_SIZE = 2048;

            // Create a new socket instance
            // Since this socket is only used within the WiFi network, the preference is on the NonCellular side,
            // to not send the UDP broadcast into the Internet, it wouldn't be delivered...
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetNetworkPreference(NetworkSelectionCharacteristics.NonCellular);
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();

            // Let's assume that the local network broadcast address can be created by changing the last digit to 255
            // - Like 192.168.0.xx -> 192.168.0.255
            // WP8 does support sending to IPAddress.Broadcast, i.e. "255.255.255.255", but there's something
            // wrong with the socket handling:
            // - Cannot use 'SendToAsync', but have to use 'ConnectAsync' instead
            // - Cannot receive any data using the same socket (code seems OK, but nothing is received)
            byte[] localBroadcastAddressBytes = FindMyIPAddress().GetAddressBytes();
            localBroadcastAddressBytes[3] = 255;
            IPAddress localBroadcast = new IPAddress(localBroadcastAddressBytes);

            socketEventArg.RemoteEndPoint = new IPEndPoint(localBroadcast, UDPPort);
            socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
            {
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.SendTo: // The broadcast was sent successfully, now start listening on the same socket
                        Socket sock = e.UserToken as Socket;
                        socketEventArg.SetBuffer(new Byte[MAX_RECVBUFFER_SIZE], 0, MAX_RECVBUFFER_SIZE);
                        sock.ReceiveFromAsync(socketEventArg);
                        break;

                    case SocketAsyncOperation.ReceiveFrom: // Received a response to the broadcast
                        if (e.SocketError == System.Net.Sockets.SocketError.Success)
                        {
                            // Retrieve the data from the buffer
                            var response = Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred);
                            response = response.Trim('\0');

                            System.Diagnostics.Debug.WriteLine("Received JSON data: " + response);

                            // Is this is an 'identify' or 'command' response?
                            HeatPumpIdentifyResponse identifyResponse = (HeatPumpIdentifyResponse)JsonFunctions.DeserializeFromStringToJson(response, typeof(HeatPumpIdentifyResponse));
                            System.Diagnostics.Debug.WriteLine("Response to command: " + identifyResponse.command + ": " + response);

                            // Run the notification handler (if found)
                            if (App.ViewModel.notificationHandlers.ContainsKey(identifyResponse.command))
                            {
                                System.Diagnostics.Debug.WriteLine("Found handler for command " + identifyResponse.command);
                                App.ViewModel.notificationHandlers[identifyResponse.command](response);
                            }
                        }
                        break;
                }
            });

            // Add the data to be sent into the send buffer
            byte[] payload = Encoding.UTF8.GetBytes((string)ea.Argument);
            socketEventArg.SetBuffer(payload, 0, payload.Length);

            // Add the socket as the UserToken for use within the event handler
            socketEventArg.UserToken = socket;

            // Send the UDP broadcast
            socket.SendToAsync(socketEventArg);
        }

        //
        // Send a command over SSH
        // - The response is sent over the Windows Phone notification channel, as a RAW notification, i.e. no need to read the response from SSH
        public static void SendSSH(object sender, DoWorkEventArgs ea)
        {
            SSHFunctions.SSHConnect();
            SSHFunctions.SSHExecute(String.Format(App.ViewModel.settings.UDPBroadcastSetting, (string)ea.Argument));
            SSHFunctions.SSHDisconnect();
        }
    }
}