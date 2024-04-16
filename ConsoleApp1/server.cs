using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Threading;

class Server {
    private static readonly Hashtable clients = Hashtable.Synchronized(new Hashtable());
    private static readonly Hashtable users = Hashtable.Synchronized(new Hashtable());

    //private static Dictionary<string, string>

    static void Main() {
        StartServer();
    }

    public static void StartServer() {
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        int port = 1994;
        TcpListener serverSocket = new(ipAddress, port);

        try {
            serverSocket.Start();
            Console.WriteLine("Запуск сервера");

            while (true) {
                TcpClient clientSocket = serverSocket.AcceptTcpClient();
                Console.WriteLine("Клиент подключен." +
                    $"{((IPEndPoint)clientSocket.Client.RemoteEndPoint).Address}" +  $":{((IPEndPoint)clientSocket.Client.RemoteEndPoint).Port}");
                clients.Add(clientSocket, clientSocket);

                Thread clientThread = new(() => {HandleClient(clientSocket);});
                clientThread.Start();
            }
        } catch (Exception ex) {
            Console.WriteLine("Error starting the server: " + ex.Message);
        } finally {
            serverSocket.Stop();
            Console.WriteLine("Server stopped.");
        }
    }
    private static void RegisterUser(string username, string password, TcpClient clientSocket) {
        if (!users.ContainsKey(username)) {
            users[username] = password;

            SendMessage("User registered successfully now LOGIN", clientSocket);
            Console.WriteLine($"new user '{username}' registered");
        } else {
            SendMessage("User with this username already exists. Please choose another username.", clientSocket);
        }
    }
    private static void AuthenticateUser(string login, string password, TcpClient clientSocket, ref bool auth, ref string username) {
        if (users.ContainsKey(login) && users[login].ToString() == password) {
            auth = true;
            username = login;
            SendMessage($"Authentication successful! Hello {username}", clientSocket);
            Console.WriteLine($"Client {username} logged in");
            Broadcast($"{username} joined the chat", clientSocket);
        } else {
            SendMessage("Incorrect username or password. Please try again.", clientSocket);
        }
    }
    private static void HandleClient(TcpClient clientSocket) {
        NetworkStream networkStream = clientSocket.GetStream();
        bool isAuth = false;
        string username = "user";
        try {
            byte[] bytesFrom = new byte[1024];
            int bytesRead;
            if (isAuth == false) {
                SendMessage("LOGIN with L username password" +
                        " or REGISTER with R username password", clientSocket);
                while ((bytesRead = networkStream.Read(bytesFrom, 0, bytesFrom.Length)) != 0) {
                    string authData = Encoding.ASCII.GetString(bytesFrom, 0, bytesRead);
                    string[] clientData = authData.Split(' ');
                    if (clientData[0] == "R") {
                        if (clientData.Length == 3) {
                            RegisterUser(clientData[1], clientData[2], clientSocket);
                        } else {
                            SendMessage("wrong 'register' command use, try again", clientSocket);

                        }

                    } else if (clientData[0] == "L") {
                        if (clientData.Length == 3) {
                            AuthenticateUser(clientData[1], clientData[2], clientSocket, ref isAuth, ref username);
                        } else {
                            SendMessage("wrong 'login' command use, try again", clientSocket);

                        }

                    } else {
                        SendMessage($"there is no '{clientData[0]}' command, try again", clientSocket);
                    }
                    if (isAuth) {
                        break;
                    }
                }
            }
            if (isAuth == true) {
                while ((bytesRead = networkStream.Read(bytesFrom, 0, bytesFrom.Length)) != 0) {

                    string dataFromClient = Encoding.ASCII.GetString(bytesFrom, 0, bytesRead);
                    Console.WriteLine($"Received from {username}: " + dataFromClient);
                    Broadcast($"@{username}:" + dataFromClient, clientSocket);
                }
            }

        } catch (Exception ex) {
            Console.WriteLine($"Client {username} forcibly disconnected: " + ex.Message);
        } finally {
            Console.WriteLine($"Client {username} disconnected.");
            Broadcast($"{username} disconnected.", clientSocket);
            clients.Remove(clientSocket);
            clientSocket.Close();
        }
    }
    public static void SendMessage(string message, TcpClient tcpClient) {
        NetworkStream broadcastStream = tcpClient.GetStream();

        byte[] broadcastBytes = Encoding.ASCII.GetBytes(message);
        broadcastStream.Write(broadcastBytes, 0, broadcastBytes.Length);
        broadcastStream.Flush();
    }
    public static void Broadcast(string message, TcpClient senderClient) {
        foreach (DictionaryEntry item in clients) {
            TcpClient broadcastSocket = (TcpClient)item.Value;

            if (broadcastSocket != senderClient) {
                SendMessage(message, broadcastSocket);
            }
        }
    }

}

