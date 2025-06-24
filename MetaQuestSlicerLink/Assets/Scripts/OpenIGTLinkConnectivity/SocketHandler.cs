// Code retrieved from: https://github.com/BIIG-UC3M/IGT-UltrARsound
// Code created by Marius Krusen
// Modified by Niklas Kompe, Johann Engster, Phillip Overloeper (adapted for Meta Quest)

using System;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using System.Net.Sockets;

public class SocketHandler
{
    private TcpClient tcpClient;
    private NetworkStream clientStream;

    public SocketHandler()
    {
        tcpClient = new TcpClient();
    }

    public bool Connect(string ip, int port)
    {
        try
        {
            tcpClient = new TcpClient(ip, port);
            clientStream = tcpClient.GetStream();
            Debug.Log("[SocketHandler] Connected to " + ip + ":" + port);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[SocketHandler] Connection failed: " + e);
            return false;
        }
    }

    public void Send(string msg)
    {
        byte[] msgAsByteArray = Encoding.ASCII.GetBytes(msg);
        Send(msgAsByteArray);
    }

    public void Send(byte[] msg)
    {
        if (clientStream != null && clientStream.CanWrite)
        {
            try
            {
                clientStream.Write(msg, 0, msg.Length);
            }
            catch (Exception e)
            {
                Debug.LogError("[SocketHandler] Send failed: " + e);
            }
        }
    }

    public byte[] Listen(uint msgSize)
    {
        byte[] buffer = new byte[msgSize];
        List<byte> byteList = new List<byte>();
        int readBytes = 0;

        try
        {
            while (clientStream != null && clientStream.CanRead && clientStream.DataAvailable)
            {
                readBytes = clientStream.Read(buffer, 0, buffer.Length);
                byteList.AddRange(new ArraySegment<byte>(buffer, 0, readBytes));
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[SocketHandler] Read failed: " + e);
        }

        return byteList.ToArray();
    }

    public void Disconnect()
    {
        try
        {
            if (clientStream != null) clientStream.Close();
            if (tcpClient != null) tcpClient.Close();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SocketHandler] Disconnect warning: " + e);
        }
    }
}

// Optional shader compatibility fix for Meta Quest (replace MRTK shader)
#if UNITY_ANDROID
[ExecuteAlways]
public class QuestShaderFix : MonoBehaviour
{
    void OnEnable()
    {
        var renderers = FindObjectsOfType<Renderer>();
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat != null && mat.shader.name.Contains("Mixed Reality Toolkit"))
                {
                    mat.shader = Shader.Find("Mobile/Diffuse");
                    Debug.Log("[QuestShaderFix] Replaced MRTK shader with Mobile/Diffuse on: " + renderer.name);
                }
            }
        }
    }
}
#endif
