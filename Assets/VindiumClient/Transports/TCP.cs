using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class TCP
{
    public TcpClient socket;

    // Must precisely match the server tickrate!
    // This is the time between each tick in milliseconds.
    // 30 ticks per 1 second - 33.3333ms per tick (1000 / 30)
    public float timeBetweenTicksMs = 33.3333f;

    private NetworkStream stream;
    private byte[] receiveBuffer;
    private bool canWrite = true;

    private Dictionary<PacketManager.PacketDataType, string> messageQueue = new Dictionary<PacketManager.PacketDataType, string>();

    public void Connect()
    {
        socket = new TcpClient
        {
            ReceiveBufferSize = NetworkManager.dataBufferSize,
            SendBufferSize = NetworkManager.dataBufferSize
        };

        receiveBuffer = new byte[NetworkManager.dataBufferSize];
        var test = socket.BeginConnect(NetworkManager.instance.ip, NetworkManager.instance.port, ConnectCallback, socket);
    }

    public void Disconnect()
    {
        socket.Close();
        stream = null;
        receiveBuffer = null;
        socket = null;
    }

    public void SendMessage(PacketManager.PacketDataType messageType, string message)
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

            yield return new WaitForSeconds((timeBetweenTicksMs / 1000) - (Time.time - tickStartTimeSeconds));
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

        stream.BeginRead(receiveBuffer, 0, NetworkManager.dataBufferSize, ReceiveCallback, null);

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
                PacketManager.PacketDataType packetData = (PacketManager.PacketDataType) System.Enum.Parse(typeof(PacketManager.PacketDataType), packet.ptype);

                switch (packetData)
                {
                    case PacketManager.PacketDataType.Spawn:
                        PacketHandler.HandleSpawn(packet, NetworkManager.instance);
                    break;
                    case PacketManager.PacketDataType.Disconnect:
                        PacketHandler.HandleDisconnect(packet, NetworkManager.instance);
                    break;
                    case PacketManager.PacketDataType.Transform:
                        PacketHandler.HandleTransform(packet, NetworkManager.instance);
                    break;
                }
            }

            receiveBuffer = new byte[NetworkManager.dataBufferSize];

            stream.BeginRead(receiveBuffer, 0, NetworkManager.dataBufferSize, ReceiveCallback, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error receiving TCP data: {ex}");
        }
    }
}