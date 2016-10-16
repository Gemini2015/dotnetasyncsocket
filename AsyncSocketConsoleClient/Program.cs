using Deusty.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncSocketConsoleClient
{
    class Program
    {
        AsyncSocket asyncSocket = null;
        int readCounter = 0;
        
        public Program()
        {
            asyncSocket = new AsyncSocket();
            asyncSocket.DidConnect += AsyncSocket_DidConnect;
            asyncSocket.DidClose += AsyncSocket_DidClose;
            asyncSocket.DidRead += AsyncSocket_DidRead;

            asyncSocket.Connect("127.0.0.1", 15035);
        }

        public void Run()
        {
            var timeStamp = DateTime.Now;
            while(true)
            {
                asyncSocket.ProcessOneEvent();

                if (readCounter >= 10)
                    break;

                var now = DateTime.Now;
                if((now - timeStamp).TotalSeconds > 5)
                {
                    SendMsg();
                    timeStamp = now;
                }
            }
        }

        private void SendMsg()
        {
            string msg = string.Format("Msg[{0}]", readCounter);
            byte[] msgData = UTF8Encoding.UTF8.GetBytes(msg);
            asyncSocket.Write(msgData, -1, readCounter);
        }


        private void AsyncSocket_DidRead(AsyncSocket sender, byte[] data, long tag)
        {
            var text = UTF8Encoding.UTF8.GetString(data);
            System.Console.WriteLine("DidRead:" + "[" + tag + "]" + text);
            asyncSocket.Read(-1, ++readCounter);
        }

        private void AsyncSocket_DidClose(AsyncSocket sender)
        {
            System.Console.WriteLine("DidClose");
        }

        private void AsyncSocket_DidConnect(AsyncSocket sender, System.Net.IPAddress address, ushort port)
        {
            System.Console.WriteLine("DidConnect " + address.ToString() + " at " + port);
            readCounter = 0;
            asyncSocket.Read(-1, ++readCounter);

            SendMsg();
        }

        static void Main(string[] args)
        {
            var program = new Program();

            program.Run();

            var key = System.Console.ReadKey();
        }
    }
}
