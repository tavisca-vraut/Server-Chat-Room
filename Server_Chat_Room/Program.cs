using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server_Chat_Room
{
    class Program
    {
        static readonly object _lock = new object();
        static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>();
        static readonly Dictionary<int, string> client_names = new Dictionary<int, string>();

        static void Main(string[] args)
        {
            int count = 1;

            TcpListener ServerSocket = new TcpListener(IPAddress.Any, 5000);
            ServerSocket.Start();

            Console.WriteLine("Now serving at 5000...");

            while (true)
            {
                TcpClient client = ServerSocket.AcceptTcpClient();
                lock (_lock) list_clients.Add(count, client);

                Task.Factory.StartNew(() => handle_clients(count++));
            }
        }

        public static void handle_clients(int id)
        {
            TcpClient client;
            bool nameSet = false;

            lock (_lock) client = list_clients[id];

            while (true)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int byte_count;
                try
                {
                    byte_count = stream.Read(buffer, 0, buffer.Length);
                    string data = Encoding.ASCII.GetString(buffer, 0, byte_count);

                    if (!nameSet)
                    {
                        lock (_lock) client_names[id] = data;
                        nameSet = true;
                        Console.WriteLine($"{client_names[id]} connected!!");
                        broadcast($"{client_names[id]} connected!!");
                    }
                    else
                    {
                        if (data == "exit") break;
                        broadcast(client_names[id].ToString() + ": " + data + Environment.NewLine);
                        Console.WriteLine(client_names[id].ToString() + ": " + data + Environment.NewLine);
                    }
                }
                catch (System.IO.IOException)
                {
                    break;
                }
            }

            lock (_lock) list_clients.Remove(id);
            broadcast($"{client_names[id]} disconnected...");
            Console.WriteLine($"{client_names[id]} disconnected...");
            lock (_lock) client_names.Remove(id);

            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        public static void broadcast(string data)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(data + Environment.NewLine);

            lock (_lock)
            {
                foreach (var _client in list_clients.Values)
                {
                    NetworkStream stream = _client.GetStream();

                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}


