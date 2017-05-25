using System;
using Microsoft.SPOT;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Collections;

namespace SmartAccess
{
    class MyFTP
    {
        String server;
        int port;
        String username;
        String password;
        int timeout;
        byte[] buffer = new byte[4096];

        public MyFTP(String _server,int _port, String _username, String _password, int _timeout)
        {
            server = _server;
            port = _port;
            username = _username;
            password = _password;
            timeout = _timeout;
        }

        public byte[] get(String filename)
        {
            int startwrite = 0;

            Socket controlSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                    ProtocolType.Tcp);

            Socket dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                    ProtocolType.Tcp);

            controlSocket.SendTimeout = timeout;
            controlSocket.ReceiveTimeout = timeout;

            dataSocket.SendTimeout = timeout;
            dataSocket.ReceiveTimeout = timeout;

            IPHostEntry hostEntry;

            hostEntry = Dns.GetHostEntry(server);
            if (hostEntry.AddressList.Length > 0)
            {
                IPEndPoint endpointControl = new IPEndPoint(hostEntry.AddressList[0], port);

                controlSocket.Connect(endpointControl);

                byte[] response;
                byte[] toSend;
                string statusCode;

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '2')
                {
                    throw new FTPException("Invalid status code: " + statusCode);
                }

                toSend = System.Text.Encoding.UTF8.GetBytes("USER " + username + "\r\n");
                controlSocket.Send(toSend);

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '3')
                {
                    throw new FTPException("Invalid status code after USER: " + statusCode);
                }

                toSend = System.Text.Encoding.UTF8.GetBytes("PASS " + password + "\r\n");
                controlSocket.Send(toSend);

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '2')
                {
                    throw new FTPException("Invalid status code after PASS: " + statusCode);
                }

                toSend = System.Text.Encoding.UTF8.GetBytes("TYPE I\r\n");
                controlSocket.Send(toSend);

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '2')
                {
                    throw new FTPException("Invalid status code after TYPE: " + statusCode);
                }

                toSend = System.Text.Encoding.UTF8.GetBytes("MODE S\r\n");
                controlSocket.Send(toSend);

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '2')
                {
                    throw new FTPException("Invalid status code MODE: " + statusCode);
                }

                toSend = System.Text.Encoding.UTF8.GetBytes("PASV\r\n");
                controlSocket.Send(toSend);

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '2')
                {
                    throw new FTPException("Invalid status code PASV: " + statusCode);
                }

                int passPort = passivePort(response);

                IPEndPoint endpointData = new IPEndPoint(hostEntry.AddressList[0], passPort);
                dataSocket.Connect(endpointData);

                toSend = System.Text.Encoding.UTF8.GetBytes("RETR " + filename + "\r\n");
                controlSocket.Send(toSend);

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode != "150")
                {
                    throw new FTPException("Invalid status code for RETR: " + statusCode);
                }

                byte[] ret = new Byte[204800];
                int totalLen = 0;
                int l = 0;
                while (true)
                {
                    l = dataSocket.Receive(buffer);
                    if (l <= 0) break;
                    append(ref ret, totalLen, buffer,l);
                    totalLen += l;
                }
                dataSocket.Close();

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '2')
                {
                    throw new FTPException("Invalid status code for QUIT: " + statusCode);
                }


                toSend = System.Text.Encoding.UTF8.GetBytes("QUIT\r\n");
                controlSocket.Send(toSend);

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '2')
                {
                    throw new FTPException("Invalid status code for QUIT: " + statusCode);
                }
                controlSocket.Close();

                return compact(ret,totalLen);
            }
            else
            {
                throw new FTPException("Unable to solve with DNS: '" + server + "'");
            }
        }

        public void upload(String filename, byte[] b)
        {
            int startwrite = 0;

            Socket controlSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                    ProtocolType.Tcp);

            Socket dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                    ProtocolType.Tcp);

            controlSocket.SendTimeout = timeout;
            controlSocket.ReceiveTimeout = timeout;

            dataSocket.SendTimeout = timeout;
            dataSocket.ReceiveTimeout = timeout;

            IPHostEntry hostEntry;

            hostEntry = Dns.GetHostEntry(server);
            if (hostEntry.AddressList.Length > 0)
            {
                IPEndPoint endpointControl = new IPEndPoint(hostEntry.AddressList[0], 21);

                controlSocket.Connect(endpointControl);

                byte[] response;
                byte[] toSend;
                string statusCode;

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '2')
                {
                    throw new FTPException("Invalid status code: " +statusCode);
                }

                toSend = System.Text.Encoding.UTF8.GetBytes("USER " + username + "\r\n");
                controlSocket.Send(toSend);

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '3')
                {
                    throw new FTPException("Invalid status code after USER: " + statusCode);
                }

                toSend = System.Text.Encoding.UTF8.GetBytes("PASS " + password + "\r\n");
                controlSocket.Send(toSend);

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '2')
                {
                    throw new FTPException("Invalid status code after PASS: " + statusCode);
                }

                toSend = System.Text.Encoding.UTF8.GetBytes("TYPE I\r\n");
                controlSocket.Send(toSend);

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '2')
                {
                    throw new FTPException("Invalid status code after TYPE: " + statusCode);
                }

                toSend = System.Text.Encoding.UTF8.GetBytes("MODE S\r\n");
                controlSocket.Send(toSend);

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '2')
                {
                    throw new FTPException("Invalid status code MODE: " + statusCode);
                }

                toSend = System.Text.Encoding.UTF8.GetBytes("PASV\r\n");
                controlSocket.Send(toSend);

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '2')
                {
                    throw new FTPException("Invalid status code PASV: " + statusCode);
                }

                int passPort = passivePort(response);

                IPEndPoint endpointData = new IPEndPoint(hostEntry.AddressList[0], passPort);
                dataSocket.Connect(endpointData);

                toSend = System.Text.Encoding.UTF8.GetBytes("STOR " + filename + "\r\n");
                controlSocket.Send(toSend);

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode != "150")
                {
                    throw new FTPException("Invalid status code for STOR: " + statusCode);
                }

                //TODO
                dataSocket.Send(b);
                dataSocket.Close();

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '2')
                {
                    throw new FTPException("Invalid status code for QUIT: " + statusCode);
                }


                toSend = System.Text.Encoding.UTF8.GetBytes("QUIT\r\n");
                controlSocket.Send(toSend);

                System.Threading.Thread.Sleep(1);
                response = readUntil(controlSocket, buffer, ref startwrite);
                statusCode = responseCode(response);

                if (statusCode[0] != '2')
                {
                    throw new FTPException("Invalid status code for QUIT: " + statusCode);
                }
                controlSocket.Close();
            }
            else
            {
                throw new FTPException("Unable to solve with DNS: '" + server + "'");
            }

        }

        private int passivePort(byte[] response)
        {
            try
            {
                string str = new String(System.Text.Encoding.UTF8.GetChars(response));

                int index = str.IndexOf("(");

                if (index >= 0)
                {
                    str = str.Substring(index + 1);

                    string[] arr = str.Split(',');

                    return int.Parse(arr[4]) * 256 + int.Parse(arr[5].Split(')')[0]);
                }
            }catch(Exception e)
            {
            }

            return 0;
        }

        private string responseCode(byte[] response)
        {
            string str = new String(System.Text.Encoding.UTF8.GetChars(response));
            if (str == null) return "999";

            return str.Substring(0, 3);
        }

        private void append(ref byte[] dst, int from,  byte[] add , int to)
        {

            if (dst.Length - from < to)
            {
                Debug.Print("Allargamento del buffer FTP");
                byte[] tmp = new Byte[dst.Length * 2];

                for (int i = 0; i < from; i++) tmp[i] = dst[i];

                dst = tmp;
            }

            for (int i = from, j=0 ; j < to; j++,i++) dst[i] = add[j];

        }

        private byte[] compact (byte[] orig , int len)
        {
            byte[] ret = new Byte[len];

            //for (int i = 0; i < len; i++) ret[i] = orig[i];

            Array.Copy(orig, ret, len);

            return ret;
        }

        private byte[] readUntil(Socket sock , byte[] buffer, ref int startwrite)
        {
            int i = 0;
            while ( true )
            {
                int len = sock.Receive(buffer,startwrite, buffer.Length-startwrite,SocketFlags.None);
                startwrite += len;

                int last_found = -1;

                for (int j = i; j < startwrite-1; j++)
                {
                    if (buffer[j] == '\r' && buffer[j + 1] == '\n')
                    {
                        last_found = j;
                    }
                }

                if (last_found == -1) continue;

                for (i = last_found ; i < startwrite-1; i++)
                {
                    if (buffer[i] == '\r'  && buffer[i+1] == '\n' )
                    {
                        byte[] ret = new byte[i+2];

                        for(int j = 0; j < i+2; j++)
                        {
                            ret[j] = buffer[j];
                        }

                        int k = i + 2;

                        for ( int j = 0; k < startwrite - 1; j++,k++ )
                        {
                            buffer[j] = buffer[k];
                        }

                        startwrite = (startwrite - i - 2);
                        return ret;
                    }
                }

            }
        }
    }
}
