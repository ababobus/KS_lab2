using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server {
    class Program {
        static void Main() {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1994));
            serverSocket.Listen(10);

            Console.WriteLine("Появление соединения");
            while (true) {
                Socket clientSocket = serverSocket.Accept();
                Console.WriteLine("подключился клиент: " + clientSocket.RemoteEndPoint);

                Thread clientThread = new Thread(() => HandleClient(clientSocket));
                clientThread.Start();
            }
        }

        static void HandleClient(Socket clientSocket) {
            try {
                byte[] buffer = new byte[1024];
                int receivedBytes = clientSocket.Receive(buffer);
                string receivedMessage = Encoding.Default.GetString(buffer, 0, receivedBytes);
                Console.WriteLine("получено от клиента " + clientSocket.RemoteEndPoint + ": " + receivedMessage);

                string responseMessage = "HEllo client";
                byte[] responseBuffer = Encoding.Default.GetBytes(responseMessage);
                clientSocket.Send(responseBuffer);
            } catch (Exception ex) {
                Console.WriteLine("ошибка подключения клиента: " + ex.Message);
            } finally {
                clientSocket.Close();
            }
        }
    }
}
