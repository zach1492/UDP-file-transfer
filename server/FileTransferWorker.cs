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
            string message = "ERR " + filename + " NOT_FOUND";
            UdpFileServer.SendControlReply(job.ClientEndpoint, message); 
            job.TransferSocket.Close();
            return;
        }
        else
        {
            byte[] fileBytes = File.ReadAllBytes(filepath);
            int p = UdpFileServer.PublicPort(job.TransferSocket);
            string okMsg = "OK " + filename + " SIZE " + fileBytes.Length + " PORT " + p;

            UdpFileServer.SendControlReply(job.ClientEndpoint, okMsg);

            job.TransferSocket.Client.ReceiveTimeout = 30000;
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                byte[] bytes = job.TransferSocket.Receive(ref remoteEP);
                string message = Encoding.ASCII.GetString(bytes).Trim();

                if (message == "FILE " + filename + " CLOSE")
                {
                    string response = "FILE " + filename + " CLOSE_OK";
                    byte[] closeBytes = Encoding.ASCII.GetBytes(response);
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

                string reply = "FILE " + filename + " OK START " + startIndex + " END " + endIndex + " DATA " + encodedData;
                byte[] replyBytes = Encoding.ASCII.GetBytes(reply);

                job.TransferSocket.Send(replyBytes, replyBytes.Length, remoteEP);
            }
        }
    }
}