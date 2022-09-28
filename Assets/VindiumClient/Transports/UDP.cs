using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UDP
{
    public UdpClient socket;
    public IPEndPoint endPoint;

    private Dictionary<PacketManager.PacketDataType, string> messageQueue = new Dictionary<PacketManager.PacketDataType, string>();
    private bool canRead = true;

    public UDP()
    {
        endPoint = new IPEndPoint(IPAddress.Parse(NetworkManager.instance.ipAddress), NetworkManager.instance.port);
    }

    public void ConnectUDP(int port)
    {
        socket = new UdpClient(port);

        if (socket != null)
        {
            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);
        }
        else
        {
            Debug.LogError("UDP Socket is null");
        }

        Debug.Log($"UDP Socket created on port {port}");
    }

    public void DisconnectUDP()
    {
        socket.Close();
        socket = null;
    }

    public void SendMessageUDP(PacketManager.PacketDataType messageType, string message)
    {
        try
        {
            if (socket != null)
            {
                byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
                socket.BeginSend(data, data.Length, null, null);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Error sending data to server via UDP: {e}");
        }
    }

    private IEnumerator MessageQueueProcessor()
    {
        while (true)
        {
            float tickStartTimeSeconds = Time.time;

            if (messageQueue.Count > 0)
            {
                foreach (KeyValuePair<PacketManager.PacketDataType, string> message in messageQueue)
                {
                    try
                    {
                        if (message.Value != null && socket != null)
                        {

                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error sending data to server via TCP: {ex}");
                    }
                }

                messageQueue.Clear();
            }

            yield return new WaitForSeconds((NetworkManager.TIME_BETWEEN_TICKS_MS / 1000) - (Time.time - tickStartTimeSeconds));
        }
    } 

    private void ReceiveCallback(IAsyncResult result)
    {
        if (canRead && socket != null)
        {
            canRead = false;
            try
            {
                byte[] data = socket.EndReceive(result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                if (data.Length < 4)
                {
                    return;
                }

                HandleData(data);
            }
            catch (Exception e)
            {
                Debug.Log($"Error receiving UDP data: {e}");
            }
            canRead = true;
        }
    }

    private void HandleData(byte[] data)
    {
        string json = System.Text.Encoding.UTF8.GetString(data);
        PacketManager.Packet packet = JsonUtility.FromJson<PacketManager.Packet>(json);

        PacketHandler.HandlePacket(packet, NetworkManager.instance);
    }
}