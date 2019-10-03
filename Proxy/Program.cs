using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;

namespace Proxy
{
    class Program
    {
        static void Main(string[] args)
        {
            ushort port = 9015;
            var server = new Server();
            var serverUdp = new UdpServer();

            server.Listen(port);
            serverUdp.Listen(port);

            if (server.Listening && serverUdp.Listening)
            {
                Console.WriteLine($"Listening on port: {port}");
            }

            Console.ReadLine();
        }
    }
}
