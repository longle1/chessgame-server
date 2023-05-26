using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
namespace ServerChessGame
{
    internal class Program
    {
        private static TcpListener server = null;
        private static TcpClient client = null;
        private static Thread manageClientThread = null;
        private static Hashtable clients = new Hashtable(); //chứa danh sách client
        private static Hashtable manageChatThread = new Hashtable();
        private static Thread acceptClientThread = null;
        private static NetworkStream stream = null;

        private static Hashtable pairOfClients = new Hashtable();
        private static Hashtable managePairOfClientsThread = new Hashtable();
        static void acceptClient()
        {
            while (true)
            {
                client = server.AcceptTcpClient();
                //khi login vào
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024 * 500];
                int length = stream.Read(buffer, 0, buffer.Length);
                string userName = Encoding.UTF8.GetString(buffer, 0, length);
                if(clients.ContainsKey(userName))
                    clients.Remove(userName);
                clients.Add(userName, client);
                
                //khởi chạy toàn bộ luồng dữ liệu
                manageClientThread = new Thread(new ParameterizedThreadStart(rcvData));
                if (manageChatThread.ContainsKey(userName))
                    manageChatThread.Remove(userName);
                manageChatThread.Add(userName, manageClientThread);

                Thread thread = (Thread)manageChatThread[userName];
                thread.Start(client);

            }
        }
        static void handleRcvDataClientToClient(object obj)
        {
            object[] objs = (object[])obj;
            TcpClient currentClient = (TcpClient)objs[0];
            TcpClient difClient = (TcpClient)objs[1];   

            while(true)
            {
                byte[] rcvData = new byte[1024 * 500];
                NetworkStream stream = currentClient.GetStream();
                int length = stream.Read(rcvData, 0, rcvData.Length);
                //dữ liệu đã nhận được
                string data = Encoding.UTF8.GetString(rcvData, 0, length);
                Console.WriteLine("Chat 1-1 nhan du lieu: " + data);

                byte[] sendData = Encoding.UTF8.GetBytes(data);
                stream = difClient.GetStream();
                stream.Write(sendData, 0, sendData.Length);
            }
        }
        static void rcvData(object cl)
        {
            TcpClient client = (TcpClient)cl;
            while (true)
            {
                stream = client.GetStream();
                byte[] buffer = new byte[1024 * 500];
                int length = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.UTF8.GetString(buffer, 0, length);

                //phân loại dữ liệu
                string[] listMsg = message.Split('*');
                switch (int.Parse(listMsg[0]))
                {
                    case 0: //cập nhật lại danh sách phòng chơi
                        foreach (string key in clients.Keys)
                        {
                            if(key != listMsg[1])
                            {
                                TcpClient client1 = (TcpClient)clients[key];
                                stream = client1.GetStream();
                                byte[] buffer2 = Encoding.UTF8.GetBytes(message);
                                stream.Write(buffer2, 0, buffer2.Length);
                            }
                        }
                        break;
                    case 1:
                    case 2:
                    case 3:
                        Console.WriteLine(listMsg[1] + ": da tao phong");
                        TcpClient clientRcv = (TcpClient)clients[listMsg[1]];
                        if (clientRcv != null)
                        {
                            stream = clientRcv.GetStream();
                            byte[] buffer1 =  Encoding.UTF8.GetBytes(message);
                            stream.Write(buffer1, 0, buffer1.Length);
                        }
                        break;
                    case 4: //dùng để tạo ra các luồng chat 1-1
                        string[] lstInfo = listMsg[1].Split(":");
                        Console.WriteLine("user " + lstInfo[0] + " dang thuc hien chat 1-1 voi " + lstInfo[2]);
                        TcpClient currentClient = (TcpClient)clients[lstInfo[2]];
                        if (currentClient != null)
                        {
                            stream = currentClient.GetStream();
                            byte[] buffer1 = Encoding.UTF8.GetBytes(message);
                            stream.Write(buffer1, 0, buffer1.Length);
                        }

                        break;
                    case 5:
                        //tách lấy ra username
                        string[] strs = listMsg[1].Split(":");
                        string userName = strs[0].Substring(0, strs[0].Length - 3);
                        //tiến hành gửi dữ liệu về cho những user còn lại
                        foreach (string key in clients.Keys)
                        {
                            if (key != userName)
                            {
                                TcpClient client1 = (TcpClient)clients[key];
                                stream = client1.GetStream();
                                byte[] buffer2 = Encoding.UTF8.GetBytes(message);
                                stream.Write(buffer2, 0, buffer2.Length);
                            }
                        }
                        break;
                    case 6: //xử lý logout
                        string[] msgs = listMsg[1].Split(",");
                        Console.WriteLine(msgs[0] + ": da roi phong chat");
                        //gửi dữ liệu đến các client còn lại
                        foreach (string key in clients.Keys)
                        {
                            if (key != msgs[0])
                            {
                                Console.WriteLine(msgs[0] + ": da roi phong chat");
                                TcpClient currentCl = (TcpClient)clients[key];
                                if (currentCl != null)
                                {
                                    stream = currentCl.GetStream();
                                    byte[] buffer2 = Encoding.UTF8.GetBytes(message);
                                    stream.Write(buffer2, 0, buffer2.Length);
                                }
                            }
                        }
                        //đóng kết nối của client này
                        TcpClient currentCl1 = (TcpClient)clients[msgs[0]];
                        if(currentCl1 != null)
                        currentCl1.Close();
                        clients.Remove(msgs[0]);
                        foreach (string key in manageChatThread.Keys)
                        {
                            if (key == msgs[0])
                            {
                                manageChatThread.Remove(key);
                                return;
                            }
                        }
                        break;
                    case 7:
                        TcpClient clientRcv1 = (TcpClient)clients[listMsg[1]];
                        if (clientRcv1 != null)
                        {
                            stream = clientRcv1.GetStream();
                            byte[] buffer1 = Encoding.UTF8.GetBytes(message);
                            stream.Write(buffer1, 0, buffer1.Length);
                        }
                        break;
                }
            }
        }
        static void Main(string[] args)
        {
            server = new TcpListener(System.Net.IPAddress.Any, 8081);
            server.Start();


            acceptClientThread = new Thread(new ThreadStart(acceptClient));
            acceptClientThread.Start();

        }
        
    }
}