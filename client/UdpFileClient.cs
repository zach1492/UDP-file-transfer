using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UdpFileClient
{
    public static void Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: mono UdpFileClient.exe <hostname> <port> <file-list>");
            return;
        }

        string host = args[0];
        int port = int.Parse(args[1]);
        string[] files = File.ReadAllLines(args[2]);

        UdpClient controlSocket = new UdpClient();
        IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(host), port);

        foreach (string filename in files)
        {
            if (filename.Trim().Length == 0) continue;
            DownloadFile(controlSocket, serverEP, host, filename.Trim());
        }

        controlSocket.Close();
    }

    static void DownloadFile(UdpClient controlSocket, IPEndPoint serverEP, string host, string filename)
    {
        Console.WriteLine(filename);

        byte[] msg = Encoding.ASCII.GetBytes($"DOWNLOAD {filename}");
        controlSocket.Send(msg, msg.Length, serverEP);

        controlSocket.Client.ReceiveTimeout = 5000;
        IPEndPoint fromEP = new IPEndPoint(IPAddress.Any, 0);
        byte[] resp = controlSocket.Receive(ref fromEP);
        string reply = Encoding.ASCII.GetString(resp).Trim();

        if (reply == $"ERR {filename} NOT_FOUND")
        {
            Console.WriteLine(reply);
            return;
        }

        int sizeStart = reply.IndexOf("SIZE ") + 5;
        int sizeEnd = reply.IndexOf(" PORT");
        int fileSize = int.Parse(reply.Substring(sizeStart, sizeEnd - sizeStart));

        int portStart = reply.IndexOf("PORT ") + 5;
        int transferPort = int.Parse(reply.Substring(portStart));


        UdpClient dataSocket = new UdpClient();
        IPEndPoint dataEP = new IPEndPoint(IPAddress.Parse(host), transferPort);
        dataSocket.Client.ReceiveTimeout = 5000;

        byte[] buffer = new byte[fileSize];
        int position = 0;
        int lastPercent = -1;

        Console.Write($"\r{filename} 0%");

        while (position < fileSize)
        {
            int end = Math.Min(position + 999, fileSize - 1);
            string getMsg = $"FILE {filename} GET START {position} END {end}";

            string chunkReply = null;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                byte[] getBytes = Encoding.ASCII.GetBytes(getMsg);
                dataSocket.Send(getBytes, getBytes.Length, dataEP);
                dataSocket.Client.ReceiveTimeout = (attempt + 1) * 1000;

                try
                {
                    IPEndPoint replyEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] chunkBytes = dataSocket.Receive(ref replyEP);
                    dataEP = replyEP;
                    chunkReply = Encoding.ASCII.GetString(chunkBytes).Trim();
                    break;
                }
                catch (SocketException)
                {
                    DrainSocket(dataSocket);
                }
            }

            if (chunkReply == null)
            {
                Console.WriteLine($"\nERROR {filename} timeout");
                dataSocket.Close();
                return;
            }

            int dataStart = chunkReply.IndexOf(" DATA ") + 6;
            string encoded = chunkReply.Substring(dataStart);
            byte[] chunk = Convert.FromBase64String(encoded);
            Array.Copy(chunk, 0, buffer, position, chunk.Length);

            position = end + 1;

            int percent = (int)((float)position / fileSize * 100);
            if (percent != lastPercent)
            {
                Console.Write($"\r{filename} {percent}%");
                lastPercent = percent;
            }
        }

        Console.Write($"\r{filename} 100%");
        Console.WriteLine();

        byte[] closeMsg = Encoding.ASCII.GetBytes($"FILE {filename} CLOSE");
        dataSocket.Send(closeMsg, closeMsg.Length, dataEP);

        try
        {
            IPEndPoint replyEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] closeResp = dataSocket.Receive(ref replyEP);
        }
        catch (SocketException){}

        File.WriteAllBytes(filename, buffer);
        Console.WriteLine($"OK {filename}");
        dataSocket.Close();
    }

    static void DrainSocket(UdpClient socket)
    {
        int saved = socket.Client.ReceiveTimeout;
        socket.Client.ReceiveTimeout = 1;
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            while (true) socket.Receive(ref ep);
        }
        catch (SocketException) { }
        socket.Client.ReceiveTimeout = saved;
    }
}
