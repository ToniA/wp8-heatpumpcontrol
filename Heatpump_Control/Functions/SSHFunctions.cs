using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Microsoft.Phone.Net.NetworkInformation;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Networking;
using Windows.Storage.Streams;
using Renci.SshNet;
using System.IO;

namespace Heatpump_Control
{
    public class SSHFunctions
    {
        private static SshClient client;

        public static void SSHConnect()
        {
            string host = App.ViewModel.settings.SSHServerSetting;
            int port = Convert.ToInt32(App.ViewModel.settings.SSHPortSetting);
            string username = App.ViewModel.settings.SSHAccountSetting;

            if (App.ViewModel.settings.SSHUseKeySetting)
            {
                string sshkey = App.ViewModel.settings.SSHKeySetting;

                byte[] s = System.Text.Encoding.GetEncoding("iso-8859-1").GetBytes(sshkey);
                MemoryStream m = new MemoryStream(s);

                client = new SshClient(host, port, username, new PrivateKeyFile(m));
            }
            else
            {
                string password = App.ViewModel.settings.SSHPasswordSetting;

                client = new SshClient(host, port, username, password);
            }

            System.Diagnostics.Debug.WriteLine("SSH client done");
            client.Connect();
            System.Diagnostics.Debug.WriteLine("SSH connect done");
        }

        public static string SSHExecute(string sshcommand)
        {
            SshCommand cmd;
            string result;

            System.Diagnostics.Debug.WriteLine("CMD: " + sshcommand);

            cmd = client.CreateCommand(sshcommand);
            cmd.CommandTimeout = TimeSpan.FromSeconds(30);
            result = cmd.Execute();

            System.Diagnostics.Debug.WriteLine("RES: " + result);

            return result;
        }

        public static void SSHDisconnect()
        {
            client.Disconnect();
            System.Diagnostics.Debug.WriteLine("SSH disconnect done");
        }
    }
}
