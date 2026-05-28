using System;
using System.Net;
using System.Net.Sockets;
using System.Net.IO

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
        string message = $"ERR {filename} NOT_FOUND";

        if(!file.Exists(filename)){
            SendControlReply(ClientEndpoint, message);
            socket.Shutdown(SocketShutdown.Send)
            transferSocket.close();
            
        }
        else{
            p = PublicPort(job.TransferSocket)
            message = $"OK {filename} SIZE {new FileInfo(path).Length} PORT {p/*IPEndPoint*/} ";

            byte[] bytes = Encoding.ASCII.GetBytes(message);

            job.transferSocket.send(bytes, bytes.Length, 0, SocketFlags.None);

            while(true){
                byte[] bytes = transferSocket.Receive(ref groupEP);

                string message = Encoding.ASCII.GetString(bytes);

                int startWord = message.IndexOf("FILE ") + 5;
                int endWord = message.IndexOf(" GET");

                string fileName = message.Substring(startWord, endWord - startWord);

                startWord = message.IndexOf("START ") + 6;
                endWord = message.IndexOf(" END");

                int startIndex = int.Parse(message.Substring(startWord, endWord - startWord));

                startWord = message.IndexOf(" END") + 4;

                int endIndex = int.Parse(message.Substring(startWord, message.Length - startWord));

                string file = File.ReadAllLines(fileName);

                int length = endIndex - startIndex 

                file = file.Substring(startIndex, length);
            }
        }
    }
}
