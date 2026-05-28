using System;
using System.Net;
using System.Net.Sockets;

public class FileTransferWorker
{
    public class Job
    {
        public string Filename;
        public IPEndPoint ClientEndpoint;
        public UdpClient TransferSocket;

        public Job(string filename, IPEndPoint clientEndpoint, UdpClient transferSocket)
        {
            Filename = filename;
            ClientEndpoint = clientEndpoint;
            TransferSocket = transferSocket;
        }
    }

    // Implement Run — see assignment specification
    // (Job: Filename, ClientEndpoint, TransferSocket).
    public static void Run(object jobObject)
    {
    }
}
