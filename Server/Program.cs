using System.Net;
using System.Net.Sockets;
using System.Text;
class ChatServer
{
    private TcpListener server;
    public List<ClientHandler> clients = new List<ClientHandler>();

    public void Start()
    {
        server = new TcpListener(IPAddress.Any,3302);
        server.Start();

        Console.WriteLine("On.");

        while(true)
        {
            TcpClient client = server.AcceptTcpClient();
            string usernameMessage = WaitForUsername(client);
            string username = ParseUsername(usernameMessage); 
            Console.WriteLine("[Info] Connected: " + username);

            ClientHandler clientHandler = new ClientHandler(client,this,username);
            clients.Add(clientHandler);

            BroadcastMessage("<i>[Connected] " + username + "</i>");

            Thread clientThread = new Thread(clientHandler.HandleClient);
            clientThread.Start();

            Console.WriteLine("[Info] UserCount: " + clients.Count);
            BroadcastMessage("{clientscount:"+ clients.Count +"}");
        }
    }

    private string WaitForUsername(TcpClient client)
    {
        byte[] usernameBuffer = new byte[4096];
        int bytesRead = client.GetStream().Read(usernameBuffer,0,4096);
        return Encoding.UTF8.GetString(usernameBuffer,0,bytesRead);
    }

    private string ParseUsername(string usernameMessage)
    {
        if(usernameMessage.StartsWith("{") && usernameMessage.EndsWith("}"))
        {
            string[] parts = usernameMessage.Trim('{','}').Split(':');
            if(parts.Length == 2 && parts[0].Trim().ToLower() == "username")
            {
                return parts[1].Trim();
            }
        }
        return "Unkown";
    }

    public void BroadcastMessage(string message,ClientHandler sender = null)
    {
        foreach(var client in clients)
        {
            if(client != sender)
            {
                client.SendMessage(message);
            }
        }
    }

    public void RemoveClient(ClientHandler client)
    {
        Console.WriteLine("[Info] Disconnected: " + client.Username);
        BroadcastMessage("<i>[Disconnected]: " + client.Username + "</i>");
        clients.Remove(client);

        Console.WriteLine("[Info] UserCount: " + clients.Count);
        BroadcastMessage("{clientscount:" + clients.Count + "}");
    }
}

class ClientHandler
{
    private TcpClient client;
    private ChatServer server;
    private NetworkStream stream;
    private Thread receiveThread;
    private string username;

    public string Username
    {
        get { return username; }
    }

    public ClientHandler(TcpClient tcpClient,ChatServer chatServer,string initialUsername)
    {
        client = tcpClient;
        server = chatServer;
        stream = client.GetStream();
        username = initialUsername;
    }

    public void HandleClient()
    {
        receiveThread = new Thread(ReceiveMessages);
        receiveThread.Start();
    }

    private void ReceiveMessages()
    {
        byte[] message = new byte[4096];
        int bytesRead;

        while(true)
        {
            try
            {
                bytesRead = stream.Read(message,0,4096);

                if(bytesRead == 0)
                    break;

                string receivedMessage = Encoding.UTF8.GetString(message,0,bytesRead);
                Console.WriteLine(username + ": " + receivedMessage);
                if(receivedMessage.StartsWith("{") && receivedMessage.EndsWith("}"))
                {
                    string[] parts = receivedMessage.Trim('{','}').Split(':');
                    if(parts.Length == 2 && parts[0].Trim().ToLower() == "username")
                    {
                        username = parts[1].Trim();
                        continue;
                    }
                    else
                    {
                        server.BroadcastMessage(username + ": " + receivedMessage,this);
                    }
                }
                else
                {
                    server.BroadcastMessage(username + ": " + receivedMessage,this);
                }
            }
            catch
            {
                break;
            }
        }

        server.RemoveClient(this);
        client.Close();
    }

    public void SendMessage(string message)
    {
        if(client != null && client.Connected)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data,0,data.Length);
            stream.Flush();
        }
    }
}
class Program
{
    static void Main()
    {
        ChatServer chatServer = new ChatServer();
        chatServer.Start();
    }
}