using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
namespace ServerChessGame
{
    internal class Program
    {
        private static TcpListener server = null;
        private static TcpClient client = null;
        private static Thread manageClientThread = null;
        private static List<TcpClient> clients = new List<TcpClient>();
        private static Hashtable manageChatThread = new Hashtable();
        private static Thread acceptClientThread = null;
        static void acceptClient()
        {
            while (true)
            {
                client = server.AcceptTcpClient();
                clients.Add(client);
                //khi login vào
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024 * 500];
                int length = stream.Read(buffer, 0, buffer.Length);
                string userName = Encoding.UTF8.GetString(buffer, 0, length);
                manageClientThread = new Thread(new ParameterizedThreadStart(rcvData));
                if (manageChatThread.ContainsKey(userName))
                {
                    Console.WriteLine("Da xoa luong nay: " + userName);
                    manageChatThread.Remove(userName);
                }
                manageChatThread.Add(userName, manageClientThread);
                Thread thread = (Thread)manageChatThread[userName];
                thread.Start(client);
            }
        }
        static void rcvData(object cl)
        {
            TcpClient client = (TcpClient)cl;
            while (true)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024 * 500];
                int length = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.UTF8.GetString(buffer, 0, length);
                //phân loại dữ liệu
                string[] listMsg = message.Split('*');
                switch (int.Parse(listMsg[0]))
                {
                    case 0:
                        break;
                    case 5:
                        //tiến hành gửi dữ liệu về cho những user còn lại
                        foreach (TcpClient client1 in clients)
                        {
                            if (client1 != client)
                            {
                                NetworkStream stream1 = client1.GetStream();
                                byte[] buffer1 = Encoding.UTF8.GetBytes(message);
                                stream1.Write(buffer1, 0, buffer1.Length);
                            }
                        }
                        break;
                    case 7: //xử lý logout
                        foreach (string key in manageChatThread.Keys)
                        {

                            if (key == listMsg[1])
                            {
                                manageChatThread.Remove(key);
                                return;
                            }
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