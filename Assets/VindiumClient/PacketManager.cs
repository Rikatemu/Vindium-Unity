using UnityEngine;

public static class PacketManager
{
    [System.Serializable]
    public enum PacketDataType {
        Accept,
        Transform,
        Spawn,
        Disconnect,
        Ping,
        Chat
    }

    [System.Serializable]
    public struct AcceptData {
        public bool accepted;
        public string entity_id;
        public string err_message;
        public SpawnData spawn_data;
    }

    [System.Serializable]
    public struct SpawnData {
        public string entity_id;
        public Vector3 position;
        public Quaternion rotation;
    }

    [System.Serializable]
    public struct DisconnectData {
        public string entity_id;
    }

    [System.Serializable]
    public struct Packet {
        public string sender;
        public string ptype;
        public string data;
        public bool send_back;
        public bool owner_only;
    }

    [System.Serializable]
    public struct TransformData {
        public string entity_id;
        public Position position;
        public Rotation rotation;
    }

    [System.Serializable]
    public struct Position {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public struct Rotation {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    /// <summary>Prepare Transform (position, rotation) data for sending to the server.</summary>
    public static void SendTransform(string entityId, Vector3 position, Quaternion rotation)
    {
        TransformData networkTransform = new TransformData();
        networkTransform.entity_id = entityId;

        Position pos = new Position();
        pos.x = position.x;
        pos.y = position.y;
        pos.z = position.z;

        Rotation rot = new Rotation();
        rot.x = rotation.x;
        rot.y = rotation.y;
        rot.z = rotation.z;
        rot.w = rotation.w;

        networkTransform.position = pos;
        networkTransform.rotation = rot;

        Packet packet = new Packet();
        packet.sender = NetworkManager.instance.ip.ToString();
        packet.ptype = PacketDataType.Transform.ToString();
        packet.data = JsonUtility.ToJson(networkTransform);
        packet.send_back = false;
        packet.owner_only = false;

        string message = JsonUtility.ToJson(packet);

        NetworkManager.instance.tcp.SendMessage(PacketDataType.Transform, message);
    }
}
