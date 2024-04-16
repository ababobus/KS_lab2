using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Client {
    private static string serverIp = "127.0.0.1";
    private static int port = 1994;

    static void Main() {
        ConnectToServer();
    }

    public static void ConnectToServer() {
        TcpClient clientSocket = new();
        try {
            clientSocket.Connect(IPAddress.Parse(serverIp), port);

            Console.WriteLine("Выполнено соединение с сервером");

            NetworkStream serverStream = clientSocket.GetStream();

            Thread receiveThread = new(() => {
                ReceiveMessages(serverStream);
            });
            receiveThread.Start();

            Thread sendThread = new(() => {
                while (true) {
                    Console.Write("Ввод: ");
                    string message = Console.ReadLine();
                    if (message != null) {
                        byte[] outStream = Encoding.ASCII.GetBytes(message);
                        serverStream.Write(outStream, 0, outStream.Length);
                        serverStream.Flush();
                    }

                }
            });
            sendThread.Start();
        } catch (Exception ex) {
            Print("Error connecting to the server: " + ex.Message);
        }
    }

    public static void ReceiveMessages(NetworkStream serverStream) {
        try {
            while (true) {
                byte[] inStream = new byte[10025];
                int bytesRead = serverStream.Read(inStream, 0, inStream.Length);

                if (bytesRead == 0) {
                    Print("Server disconnected.");
                    break;
                }

                string returndata = Encoding.ASCII.GetString(inStream, 0, bytesRead);

                Print(returndata);
            }
        } catch (Exception ex) {
            Print("Server terminated. " + ex.Message);
            serverStream.Close();
        }
    }
    public static void Print(string message) {
        if (OperatingSystem.IsWindows()) {
            var (Left, Top) = Console.GetCursorPosition();
            int left = Left;
            int top = Top;

            Console.MoveBufferArea(0, top, left, 1, 0, top + 1);
            Console.SetCursorPosition(0, top);
            Console.WriteLine(message);
            Console.SetCursorPosition(left, top + 1);
        } else Console.WriteLine(message);
    }
}
