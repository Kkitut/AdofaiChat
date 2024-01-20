using UnityEngine;
using UnityModManagerNet;
using System.Text;
using System.Threading;
using System;
using static UnityModManagerNet.UnityModManager.ModEntry;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class Main
{
    public static ModLogger Logger;
    private static bool Modon = false;

    private static TcpClient client;
    private static bool isRunning = false;

    private static string sendMessage = "";
    private static string eipAddress = "";
    private static string AllMessages = "";
    private static string infostr = "<color=#FF0000>Disconnected</color>";
    private static string formattedTime = "<color=#808080>" + DateTime.Now.ToString("HH:mm") + "</color>";
    private static string username = "";
    private static string usernametemp = "";
    private static int clientsCount = 0;
    public static bool Start(UnityModManager.ModEntry modEntry)
    {
        Logger = modEntry.Logger;
        username = "Guest-" + RandomStringGenerator.GenerateRandomString(8);
        usernametemp = username;
        modEntry.OnToggle = (entry,value) =>
        {
            Modon = value;

            if(value)
            {
                
            }
            else
            {
                if(isRunning)
                {
                    try
                    {
                        client?.Close();
                        isRunning = false;
                        clientsCount = 0;
                        infostr = "<color=#FF0000>Disconnected</color>";
                        AllMessages = "\n" + formattedTime + " <i>[Info] " + infostr + "</i>" + AllMessages;
                    }
                    catch(Exception e)
                    {
                        infostr = "Error disconnecting: " + e.Message;
                    }
                }
            }

            return true;
        };

        modEntry.OnGUI = (entry) =>
        {
            formattedTime = "<color=#808080>" + DateTime.Now.ToString("HH:mm") + "</color>";
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Send",GUILayout.Width(80)))
            {
                SendMessage(client,sendMessage);
                if(sendMessage != "")
                {
                    AllMessages = "\n" + formattedTime + " <b>" + username + "(Me):</b> " + sendMessage + AllMessages;
                    sendMessage = "";
                }
            }
            GUI.SetNextControlName("MessageField");
            sendMessage = GUILayout.TextField(sendMessage);
            if(sendMessage != "" && isRunning && Event.current.isKey && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "MessageField")
            {
                SendMessage(client,sendMessage);
                AllMessages = "\n" + formattedTime + " <b>" + username + "(Me):</b> " + sendMessage + AllMessages;
                sendMessage = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:",GUILayout.Width(0));
            if(GUILayout.Button("Apply",GUILayout.Width(120)))
            {
                username = usernametemp;
                SendMessage(client,"{username:"+username+"}");
            }

            usernametemp = GUILayout.TextField(usernametemp);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if(!isRunning && GUILayout.Button("Connect",GUILayout.Width(180)))
            {
                try
                {
                    TryConnect();
                    isRunning = true;
                    infostr = "<color=#00FF00>Connected</color>";
                    SendMessage(client,"{username:" + username + "}");
                    AllMessages = "\n" + formattedTime + " <i>[Info] " + infostr + "</i>" + AllMessages;
                }
                catch(Exception e)
                {
                    infostr = "Error connecting: " + e.Message;
                }
            }
            if(isRunning && GUILayout.Button("Disconnect",GUILayout.Width(180)))
            {
                try
                {
                    client?.Close();
                    isRunning = false;
                    clientsCount = 0;
                    infostr = "<color=#FF0000>Disconnected</color>";
                    AllMessages = "\n" + formattedTime + " <i>[Info] " + infostr + "</i>" + AllMessages;
                }
                catch(Exception e)
                {
                    infostr = "Error disconnecting: " + e.Message;
                }
                
            }
            GUILayout.Label("Address:",GUILayout.Width(0));
            eipAddress = GUILayout.TextField(eipAddress);
            GUILayout.EndHorizontal();
            GUILayout.Label("Info: " + infostr);
            GUILayout.Label("UserCount: " + clientsCount);
            if(GUILayout.Button("Clear",GUILayout.Width(100)))
            {
                AllMessages = "";
            }
            GUILayout.Label("Message:");
            GUILayout.Label(AllMessages);
        };

        return true;
    }

    static void TryConnect()
    {
        if(client == null || !client.Connected)
        {
            try
            {
                client = new TcpClient(eipAddress,3302);
                infostr = "Connected";
                Thread receiveThread = new Thread(new ParameterizedThreadStart(ReceiveMessages));
                receiveThread.Start(client);
            }
            catch(Exception e)
            {
                infostr = ("Error: " + e.Message);
            }
        }
    }

    static void ReceiveMessages(object clientObj)
    {
        TcpClient tcpClient = (TcpClient)clientObj;
        NetworkStream clientStream = tcpClient.GetStream();

        byte[] message = new byte[4096];
        int bytesRead;

        while(isRunning)
        {
            bytesRead = 0;

            try { bytesRead = clientStream.Read(message,0,4096); }
            catch { break; }

            if(bytesRead == 0)
                break;

            string receivedMessage = Encoding.UTF8.GetString(message,0,bytesRead);

            if(receivedMessage.StartsWith("{") && receivedMessage.EndsWith("}"))
            {
                string[] parts = receivedMessage.Trim('{','}').Split(':');
                if(parts.Length == 2 && parts[0].Trim().ToLower() == "clientscount")
                {
                    int count;
                    if(int.TryParse(parts[1].Trim(),out count))
                    {
                        clientsCount = count;
                        continue;
                    }
                }
            }
            AllMessages = "\n" + formattedTime + " " + receivedMessage + AllMessages;
        }
    }

    static void SendMessage(TcpClient client,string message)
    {
        if(client != null && client.Connected)
        {
            NetworkStream stream = client.GetStream();
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data,0,data.Length);
            stream.Flush();
        }
    }

}

public class RandomStringGenerator
{
    private static readonly System.Random random = new System.Random();
    private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string GenerateRandomString(int length)
    {
        char[] randomString = new char[length];

        for(int i = 0;i < length;i++)
        {
            randomString[i] = chars[random.Next(chars.Length)];
        }

        return new string(randomString);
    }
}