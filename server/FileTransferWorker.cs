using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

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

    public static void Run(object jobObject)
    {
        Job job = (Job)jobObject;
        string filename = job.Filename;
        string filepath = UdpFileServer.FilePath(filename);

        if (!File.Exists(filepath))
        {
            UdpFileServer.SendControlReply(job.ClientEndpoint, $"ERR {filename} NOT_FOUND");
            job.TransferSocket.Close();
            return;
        }

        byte[] fileBytes = File.ReadAllBytes(filepath);
        int p = UdpFileServer.PublicPort(job.TransferSocket);
        UdpFileServer.SendControlReply(job.ClientEndpoint, $"OK {filename} SIZE {fileBytes.Length} PORT {p}");

        job.TransferSocket.Client.ReceiveTimeout = 30000;
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            byte[] bytes = job.TransferSocket.Receive(ref remoteEP);
            string message = Encoding.ASCII.GetString(bytes).Trim();

            if (message == $"FILE {filename} CLOSE")
            {
                byte[] closeBytes = Encoding.ASCII.GetBytes($"FILE {filename} CLOSE_OK");
                job.TransferSocket.Send(closeBytes, closeBytes.Length, remoteEP);
                job.TransferSocket.Close();
                return;
            }

            int startWord = message.IndexOf("START ") + 6;
            int endWord = message.IndexOf(" END");
            int startIndex = int.Parse(message.Substring(startWord, endWord - startWord));

            startWord = message.IndexOf(" END ") + 5;
            int endIndex = int.Parse(message.Substring(startWord));

            int chunkLen = endIndex - startIndex + 1;
            string encodedData = Convert.ToBase64String(fileBytes, startIndex, chunkLen);

            byte[] replyBytes = Encoding.ASCII.GetBytes($"FILE {filename} OK START {startIndex} END {endIndex} DATA {encodedData}");
            job.TransferSocket.Send(replyBytes, replyBytes.Length, remoteEP);
        }
    }
}
