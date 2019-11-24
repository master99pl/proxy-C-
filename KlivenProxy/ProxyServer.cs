using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KlivenProxy {
    class ProxyServer {

        static bool cont = true;

        static void Main (string[] args) {
            IPAddress hostIp = IPAddress.Loopback;
            IPEndPoint endPoint = new IPEndPoint(hostIp, 9999);
            Socket connectionListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            connectionListener.Bind(endPoint);
            //new Thread(() => Listen(connectionListener)).Start();
            connectionListener.Listen((int)SocketOptionName.MaxConnections);

            Listen(connectionListener);
        }

        private static void Listen (Socket listener) {
            while (!cont) { }
            while (true) {
                Console.WriteLine("Waiting for a connection...");
                Socket clientSocket = listener.Accept();
                cont = false;
                string request = "";
                using (NetworkStream netStream = new NetworkStream(clientSocket)) {
                    StreamReader sr = new StreamReader(netStream);
                    while (true) {
                        string line = sr.ReadLine();
                        request += line + "\r\n";
                        Console.WriteLine("RECIVIENG FROM CLIENT\t" + line);
                        if (string.IsNullOrWhiteSpace(line))
                            break;
                    }
                }

                Console.WriteLine("Request: {0}", request);

                var split = request.Split(new string[] { "Host: " }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length < 2) {
                    byte[] wypierdalaj = Encoding.ASCII.GetBytes("Z HTTPS to wypierdalaj");
                    clientSocket.Send(wypierdalaj);
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    break;
                }
                var ip = split[1].Substring(0, split[1].IndexOf("\r\n"));


                int port = 80;
                if (ip.Contains(":443")) {
                    port = 443;
                    ip=ip.Substring(0, ip.IndexOf(':'));
                }

                Console.WriteLine("Docelowe IP: " + ip);


                new Thread(() => {
                    IPAddress remoteHost = Dns.Resolve(ip).AddressList[0]; // mowilem ze serwer DNS jet potrzebny
                    IPEndPoint endPoint = new IPEndPoint(remoteHost, 80);
                    Socket remoteSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    remoteSocket.Connect(endPoint);
                    //var remSocResp = listener.Accept();
                    var validRequest = Encoding.ASCII.GetBytes(request.Trim() + "\r\n");
                    int sent = 0;
                    while (sent != validRequest.Length) {
                        sent += remoteSocket.Send(validRequest, sent, Math.Min(1024, validRequest.Length - sent), SocketFlags.None);
                        Console.WriteLine("Sent: " + sent + " / " + validRequest.Length);
                    }
                   // remoteSocket.Listen(100);
                   // var remSocResp = remoteSocket.Accept();

                    string response = "";
                    //teraz sluchamy odpowiedzi od serwera zdalnego
                    byte[] buffer = new byte[1024];

                    int count = 0;
                    while ((count = listener.Receive(buffer)) != -1) {

                        Console.WriteLine("ODPWOEIDZ ZE ZDALNEGO: " + Encoding.ASCII.GetString(buffer));
                        clientSocket.Send(buffer, count, SocketFlags.None);
                    }
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    cont = true;
                }).Start();

                // remoteSocket.Send(Encoding.ASCII.GetBytes("\n"));
            }

        }
    }
}
