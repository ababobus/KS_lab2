
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

class MyServer
{
    private static Hashtable clients = Hashtable.Synchronized(new Hashtable());
    private static Hashtable users = Hashtable.Synchronized(new Hashtable());

    private static Hashtable TCPusers = Hashtable.Synchronized(new Hashtable());
    static void Main()
    {
        StartServer();
    }
    public static void StartServer()
    {
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        int port = 1994;
        TcpListener serverSocket = new(ipAddress, port);//.any
        try
        {
            serverSocket.Start();
            Console.WriteLine("starting server");

            while (true)
            {
                TcpClient clientSocket = serverSocket.AcceptTcpClient();
                Console.WriteLine("the client connected" +
                $"{((IPEndPoint)clientSocket.Client.RemoteEndPoint).Address}" +
                $":{((IPEndPoint)clientSocket.Client.RemoteEndPoint).Port}");

                clients.Add(clientSocket, clientSocket);

                Thread clientThread = new(() => { HandleClient(clientSocket); });
                clientThread.Start();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("error starting the server: " + ex.Message);
        }
        finally
        {
            serverSocket.Stop();
            Console.WriteLine("server finished");
        }
    }
    private static void RegisterUser(string username, string password, TcpClient clientSocket)
    {
        if (!users.ContainsKey(username))
        {
            users[username] = password;
            SendMessage("user registered successfully now L", clientSocket);
            Console.WriteLine($"new user '{username}' registered");
        }
        else
        {
            SendMessage("user with this username already exists, get another username.", clientSocket);

            byte[] data = new byte[1024];
            int bytesRead = clientSocket.GetStream().Read(data, 0, data.Length);
            string clientInfo = Encoding.UTF8.GetString(data, 0, bytesRead);
            string[] credentials = clientInfo.Split(' ');

            if (credentials.Length >= 3 && credentials[0] == "регистрация")
            {
                username = credentials[1];
                password = credentials[2];
            }

        }
    }
    private static void AuthenticateUser(string login, string password, TcpClient clientSocket, ref bool auth, ref string username)
    {

        if (users.ContainsKey(login) && users[login].ToString() == password)
        {
            auth = true;
            username = login;
            SendMessage($"Hello, {username}", clientSocket);
            Console.WriteLine($"the client {username} logged in");
            Broadcast($"{username} joined the chat", clientSocket);

            users[username] = password;
            TCPusers[username] = clientSocket;

        }
        if (!users.ContainsKey(username))
        {
            SendMessage("incorrect username or password", clientSocket);
        }

    }
    private static void HandleClient(TcpClient clientSocket)
    {
        NetworkStream networkStream = clientSocket.GetStream();
        bool isAuth = false;
        string username = "user";
        try
        {
            byte[] bytesFrom = new byte[1024];
            int bytesRead;
            if (isAuth == false)
            {
                SendMessage("R or L username password", clientSocket);

                while ((bytesRead = networkStream.Read(bytesFrom, 0, bytesFrom.Length)) != 0)
                {
                    string authData = Encoding.ASCII.GetString(bytesFrom, 0, bytesRead);
                    string[] clientData = authData.Split(' ');
                    if (clientData[0] == "R")
                    {
                        if (clientData.Length == 3)
                        {
                            RegisterUser(clientData[1], clientData[2], clientSocket);
                        }
                        else
                        {
                            SendMessage("wrong 'register' command use, try again", clientSocket);
                        }
                    }
                    else if (clientData[0] == "L")
                    {
                        if (clientData.Length == 3)
                        {
                            AuthenticateUser(clientData[1], clientData[2], clientSocket, ref isAuth, ref username);
                        }
                        else
                        {
                            SendMessage("wrong 'login' command, try again", clientSocket);
                        }
                    }
                    else
                    {
                        SendMessage($"no '{clientData[0]}' command. Try again",
                        clientSocket);
                    }
                    if (isAuth)
                    {
                        break;
                    }
                }

                while (isAuth == true && (bytesRead = networkStream.Read(bytesFrom, 0, bytesFrom.Length)) != 0)
                {
                    string dataFromClient = Encoding.ASCII.GetString(bytesFrom, 0,
                    bytesRead);
                    Console.WriteLine($"received from {username}: " + dataFromClient);
                    Broadcast($"@{username}:" + dataFromClient, clientSocket);
                }
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"the client {username} forced shutdown " + ex.Message);
        }
        finally
        {
            Console.WriteLine($"the client {username} disconnect");
            clients.Remove(clientSocket);
            clientSocket.Close();
        }
    }
    public static void SendMessage(string message, TcpClient tcpClient)
    {
        NetworkStream broadcastStream = tcpClient.GetStream();
        byte[] broadcastBytes = Encoding.ASCII.GetBytes(message);
        broadcastStream.Write(broadcastBytes, 0, broadcastBytes.Length);
        broadcastStream.Flush();
    }
    public static void Broadcast(string message, TcpClient senderClient)
    {
        foreach (DictionaryEntry item in TCPusers)
        {
            TcpClient broadcastSocket = (TcpClient)item.Value;
            if (broadcastSocket != senderClient)
            {
                SendMessage(message, broadcastSocket);
            }
        }
    }
}
