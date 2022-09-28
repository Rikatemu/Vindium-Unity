using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class TCP
{
    public TcpClient socket;

    private NetworkStream stream;
    private byte[] receiveBuffer;
    private bool canWrite = true;

    private Dictionary<PacketManager.PacketDataType, string> messageQueue = new Dictionary<PacketManager.PacketDataType, string>();

    public void ConnectTCP()
    {
        socket = new TcpClient
        {
            ReceiveBufferSize = NetworkManager.DATA_BUFFER_SIZE,
            SendBufferSize = NetworkManager.DATA_BUFFER_SIZE
        };

        receiveBuffer = new byte[NetworkManager.DATA_BUFFER_SIZE];
        var test = socket.BeginConnect(NetworkManager.instance.ip, NetworkManager.instance.port, ConnectCallback, socket);
    }

    public void DisconnectTCP()
    {
        socket.Close();
        stream = null;
        receiveBuffer = null;
        socket = null;
    }

    public void SendMessageTCP(PacketManager.PacketDataType messageType, string message)
    {
        if (messageQueue.ContainsKey(messageType))
        {
            messageQueue[messageType] = message;
        }
        else
        {
            messageQueue.Add(messageType, message);
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
                        if (canWrite && message.Value != null && socket != null)
                        {
                            stream = socket.GetStream();
                            if (stream.CanWrite)
                            {
                                canWrite = false;
                                byte[] data = System.Text.Encoding.ASCII.GetBytes(message.Value);
                                stream.BeginWrite(data, 0, data.Length, WriteFinishedCallback, null);
                            }
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

    private void WriteFinishedCallback(IAsyncResult result) 
    {
        if (result.IsCompleted)
        {
            canWrite = true;
        }
    }

    private void ConnectCallback(IAsyncResult _result)
    {
        socket.EndConnect(_result);

        if (!socket.Connected)
        {
            Debug.LogError("Failed to connect to server!");
            return;
        }

        stream = socket.GetStream();

        stream.BeginRead(receiveBuffer, 0, NetworkManager.DATA_BUFFER_SIZE, ReceiveCallback, null);

        Debug.Log($"TCP Socket created on port {((IPEndPoint)socket.Client.LocalEndPoint).Port}");
        NetworkManager.instance.udp.ConnectUDP(((IPEndPoint)socket.Client.LocalEndPoint).Port);
        NetworkManager.instance.StartCoroutine(MessageQueueProcessor());
    }

    private void ReceiveCallback(IAsyncResult _result)
    {
        try
        {
            if (stream == null) return;

            int byteLength = stream.EndRead(_result);
            if (byteLength <= 0)
            {
                return;
            }

            byte[] data = new byte[byteLength];
            Array.Copy(receiveBuffer, data, byteLength);

            string message = System.Text.Encoding.ASCII.GetString(data);

            if (NetworkManager.instance.firstRead)
            {
                PacketManager.AcceptData acceptData = JsonUtility.FromJson<PacketManager.AcceptData>(message);

                if (acceptData.accepted)
                {
                    NetworkManager.instance.id = acceptData.entity_id;
                    NetworkManager.instance.entitySpawnQueue.Add(acceptData.entity_id);
                    NetworkManager.instance.connected = true;
                    NetworkManager.instance.firstRead = false;
                }
                else
                {
                    Debug.LogError(acceptData.err_message);
                }
            }
            else
            {
                PacketManager.Packet packet = JsonUtility.FromJson<PacketManager.Packet>(message);
                
                PacketHandler.HandlePacket(packet, NetworkManager.instance);
            }

            receiveBuffer = new byte[NetworkManager.DATA_BUFFER_SIZE];

            stream.BeginRead(receiveBuffer, 0, NetworkManager.DATA_BUFFER_SIZE, ReceiveCallback, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error receiving TCP data: {ex}");
        }
    }
}