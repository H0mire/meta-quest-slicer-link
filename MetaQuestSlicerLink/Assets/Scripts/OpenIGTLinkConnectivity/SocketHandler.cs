// Code retrieved from: https://github.com/BIIG-UC3M/IGT-UltrARsound
/*
* Code created by Marius Krusen
* Modified by Niklas Kompe, Johann Engster, Phillip Overloeper
* Modified for Meta Quest (Android) compatibility
*/

using System;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;


/// <summary>
/// The class to communicate with the server socket.
/// </summary>
public class SocketHandler
{
    /// <summary>
    /// Tcp client for server communication
    /// </summary>
    private TcpClient tcpClient;

    /// <summary>
    /// Stream to receive and send messages
    /// </summary>
    private NetworkStream clientStream;


    /// <summary>
    /// Constructor to create a socket to communicate.
    /// </summary>
    public SocketHandler()
    {
        // Constructor for Meta Quest (Android) - initialization handled in Connect method
    }

    /// <summary>
    /// Connects socket to server.
    /// </summary>
    /// <param name="ip">Server ip</param>
    /// <param name="port">Server port</param>
    /// <returns>If socket connection was successful.</returns>
    public async Task<bool> Connect(string ip, int port)
    {
        try
        {
            // Create a TcpClient
            tcpClient = new TcpClient();
            
            // Set timeout and connect asynchronously
            var connectTask = tcpClient.ConnectAsync(ip, port);
            var timeoutTask = Task.Delay(5000);
            
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                Debug.Log("Connection timeout");
                tcpClient?.Close();
                return false;
            }
            
            // Create clientStream for further communication
            clientStream = tcpClient.GetStream();
            return true;
        }
        catch (Exception e)
        {
            Debug.Log("Connecting exception: " + e);
            tcpClient?.Close();
        }
        return false;
    }

    /// <summary>
    /// Method to send strings to the server.
    /// </summary>
    /// <param name="msg">Message to be sent.</param>
    public async Task Send(String msg)
    {
        byte[] msgAsByteArray = Encoding.ASCII.GetBytes(msg);
        await Send(msgAsByteArray);
    }

    /// <summary>
    /// Method to send bytes to the server.
    /// </summary>
    /// <param name="msg">Message to be sent.</param>
    public async Task Send(byte[] msg)
    {
        try
        {
            if (clientStream != null && clientStream.CanWrite)
            {
                await clientStream.WriteAsync(msg, 0, msg.Length);
                await clientStream.FlushAsync();
            }
        }
        catch (Exception e)
        {
            Debug.Log("Send exception: " + e);
        }
    }


    /// <summary>
    /// Method to receive a byte array from the server.
    /// </summary>
    /// <returns>Message the server has sent.</returns>
    public async Task<byte[]> Listen(uint msgSize)
    {
        try
        {
            if (clientStream == null || !clientStream.CanRead)
            {
                return new byte[0];
            }

            byte[] buffer = new byte[msgSize];
            int totalBytesRead = 0;
            
            while (totalBytesRead < msgSize)
            {
                int bytesRead = await clientStream.ReadAsync(buffer, totalBytesRead, (int)msgSize - totalBytesRead);
                if (bytesRead == 0)
                {
                    // Connection closed
                    break;
                }
                totalBytesRead += bytesRead;
            }
            
            // Return only the bytes that were actually read
            byte[] result = new byte[totalBytesRead];
            Array.Copy(buffer, 0, result, 0, totalBytesRead);
            return result;
        }
        catch (Exception e)
        {
            Debug.Log("Listen exception: " + e);
            return new byte[0];
        }
    }

    public void Disconnect()
    {
        try
        {
            if (clientStream != null)
            {
                clientStream.Close();
                clientStream = null;
            }
            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient = null;
            }
        }
        catch (Exception e)
        {
            Debug.Log("Disconnect exception: " + e);
        }
    }
}
