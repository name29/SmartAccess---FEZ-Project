using System;
using System.Net.Sockets;
using Microsoft.SPOT;
using System.Net;

namespace SmartAccess
{
    class SimpleFileTransfer
    {
        int timeout;
        IPEndPoint endpointControl;
        string server;
        int port;

        public SimpleFileTransfer(String server, int port, int timeout)
        {

            this.timeout = timeout;
            this.server = server;
            this.port = port;
        }

        public void upload(String filename, byte[] data)
        {
            try
            {
                Socket controlSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                        ProtocolType.Tcp);

                if (endpointControl == null)
                {
                    IPHostEntry hostEntry;

                    hostEntry = Dns.GetHostEntry(server);
                    if (hostEntry.AddressList.Length > 0)
                    {
                        endpointControl = new IPEndPoint(hostEntry.AddressList[0], port);
                    }
                    else
                    {
                        throw new Exception("Unable to resolve " + server);
                    }
                }

                controlSocket.SendTimeout = timeout;
                controlSocket.ReceiveTimeout = timeout;

                controlSocket.Connect(endpointControl);

                controlSocket.Send(System.Text.Encoding.UTF8.GetBytes(filename));
                Byte[] b = new Byte[1];
                b[0] = 0;
                controlSocket.Send(b);

                controlSocket.Send(data);

                controlSocket.Close();
            }
            catch(Exception e)
            {
                throw new SFTException("Unable to upload: "+e.Message);
            }
        }
    }
}
