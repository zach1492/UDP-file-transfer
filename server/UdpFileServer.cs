using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UdpFileServer
{
    public const string FileDirectory = "files";

    private static readonly object controlSendLock = new object();
    private static UdpClient listener;

    public static string FilePath(string filename)
    {
        return Path.Combine(FileDirectory, filename);
    }

    public static int PublicPort(UdpClient socket)
    {
        return ((IPEndPoint)socket.Client.LocalEndPoint).Port;
    }

    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: mono UdpFileServer.exe <port>");
            return;
        }

        int listenPort;
        if (!Int32.TryParse(args[0], out listenPort))
        {
            Console.Error.WriteLine("Invalid port: " + args[0]);
            return;
        }

        listener = new UdpClient(new IPEndPoint(IPAddress.Any, listenPort));
        Console.WriteLine("Server listening on UDP port " + listenPort);

        while (true)
        {
            IPEndPoint client = new IPEndPoint(IPAddress.Any, 0);
            string request = Encoding.ASCII.GetString(listener.Receive(ref client)).Trim();

            if (!request.StartsWith("DOWNLOAD "))
            {
                continue;
            }

            string filename = request.Substring("DOWNLOAD ".Length).Trim();
            if (filename.Length == 0)
            {
                continue;
            }

            Console.WriteLine("Accepted DOWNLOAD for " + filename + " from " + client);
            UdpClient transferSocket = new UdpClient(0); 
            Thread worker = new Thread(FileTransferWorker.Run);
            worker.IsBackground = true;
            worker.Start(new FileTransferWorker.Job(filename, client, transferSocket));
        }
    }

    public static void SendControlReply(IPEndPoint client, string message)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(message);
        lock (controlSendLock)
        {
            listener.Send(bytes, bytes.Length, client);
        }
    }
}
