using UnityEngine;
using UnityModManagerNet;
using System.Text;
using System.Threading;
using System;
using static UnityModManagerNet.UnityModManager.ModEntry;
using System.Net.Sockets;

public class Main
{
    public static ModLogger Logger;
    private static bool Modon = false;

    private static TcpClient client;
    private static bool isRunning = false;

    private static string sendMessage = "";
    private static string eipAddress = "";
    private static string AllMessages = "";
    private static string infostr = "<color=#FF0000>해제됨</color>";
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
                        infostr = "<color=#FF0000>해제됨</color>";
                        AllMessages = "\n" + formattedTime + " <i>[상태] " + infostr + "</i>" + AllMessages;
                    }
                    catch(Exception e)
                    {
                        infostr = "해제중 오류: " + e.Message;
                    }
                }
            }

            return true;
        };

        modEntry.OnGUI = (entry) =>
        {
            formattedTime = "<color=#808080>" + DateTime.Now.ToString("HH:mm") + "</color>";
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("전송",GUILayout.Width(60)))
            {
                SendMessage(client,sendMessage);
                if(sendMessage != "")
                {
                    AllMessages = "\n" + formattedTime + " <b>" + username + "(나):</b> " + sendMessage + AllMessages;
                    sendMessage = "";
                }
            }
            GUI.SetNextControlName("MessageField");
            sendMessage = GUILayout.TextField(sendMessage);
            if(sendMessage != "" && isRunning && Event.current.isKey && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "MessageField")
            {
                SendMessage(client,sendMessage);
                AllMessages = "\n" + formattedTime + " <b>" + username + "(나):</b> " + sendMessage + AllMessages;
                sendMessage = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("이름:",GUILayout.Width(0));
            if(GUILayout.Button("적용",GUILayout.Width(60)))
            {
                username = usernametemp;
                SendMessage(client,"{username:"+username+"}");
            }

            usernametemp = GUILayout.TextField(usernametemp);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if(!isRunning && GUILayout.Button("접속",GUILayout.Width(80)))
            {
                try
                {
                    TryConnect();
                    isRunning = true;
                    infostr = "<color=#00FF00>접속됨</color>";
                    SendMessage(client,"{username:" + username + "}");
                    AllMessages = "\n" + formattedTime + " <i>[상태] " + infostr + "</i>" + AllMessages;
                }
                catch(Exception e)
                {
                    infostr = "접속중 오류: " + e.Message;
                }
            }
            if(isRunning && GUILayout.Button("해제",GUILayout.Width(80)))
            {
                try
                {
                    client?.Close();
                    isRunning = false;
                    clientsCount = 0;
                    infostr = "<color=#FF0000>해제됨</color>";
                    AllMessages = "\n" + formattedTime + " <i>[상태] " + infostr + "</i>" + AllMessages;
                }
                catch(Exception e)
                {
                    infostr = "해제중 오류: " + e.Message;
                }
                
            }
            GUILayout.Label("서버주소:",GUILayout.Width(0));
            eipAddress = GUILayout.TextField(eipAddress);
            GUILayout.EndHorizontal();
            GUILayout.Label("상태: " + infostr);
            GUILayout.Label("접속자 수: " + clientsCount);
            // 메시지 비우기 버튼
            if(GUILayout.Button("비우기",GUILayout.Width(80)))
            {
                AllMessages = "";
            }
            // 수신된 메시지를 표시
            GUILayout.Label("메시지:");
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
                infostr = "연결됨";

                // 클라이언트에서 메시지 수신을 담당하는 스레드 시작
                Thread receiveThread = new Thread(new ParameterizedThreadStart(ReceiveMessages));
                receiveThread.Start(client);
            }
            catch(Exception e)
            {
                infostr = ("오류: " + e.Message + "\n 접속시도 IP:" + eipAddress);
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

            try
            {
                bytesRead = clientStream.Read(message,0,4096);
            }
            catch
            {
                break;
            }

            if(bytesRead == 0)
                break;

            string receivedMessage = Encoding.UTF8.GetString(message,0,bytesRead);

            // 서버에서 클라이언트 수를 저장
            if(receivedMessage.StartsWith("{") && receivedMessage.EndsWith("}"))
            {
                // 파싱 예시: {clientscount:4}
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

            // 파싱 실패한 경우 메시지 기록
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