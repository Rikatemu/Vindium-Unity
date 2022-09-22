using System;
using System.Net.Sockets;
using UnityEngine;

public class TCP
{
    public TcpClient socket;

    private NetworkStream stream;
    private byte[] receiveBuffer;
    private bool canWrite = true;

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

    public void SendMessage(string message)
    {
        try
        {
            if (canWrite && message != null && socket != null)
            {
                stream = socket.GetStream();
                if (stream.CanWrite)
                {
                    canWrite = false;
                    byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                    stream.BeginWrite(data, 0, data.Length, WriteFinishedCallback, null);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending data to server via TCP: {ex}");
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