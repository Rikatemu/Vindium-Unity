using UnityEngine;

public static class PacketHandler {
    public static void HandleSpawn(PacketManager.Packet packet, NetworkManager instance) {
        PacketManager.SpawnData spawnData = JsonUtility.FromJson<PacketManager.SpawnData>(packet.data);
        if (!instance.entities.ContainsKey(spawnData.entity_id))
        {
            instance.entitySpawnQueue.Add(spawnData.entity_id);
        }
    }

    public static void HandleDisconnect(PacketManager.Packet packet, NetworkManager instance) {
        PacketManager.DisconnectData disconnectData = JsonUtility.FromJson<PacketManager.DisconnectData>(packet.data);
        if (instance.entities.ContainsKey(disconnectData.entity_id))
        {
            NetworkManager.Entity updateEntity = new NetworkManager.Entity();
            updateEntity.id = disconnectData.entity_id;

            instance.entityUpdateQueue.Add(updateEntity, NetworkManager.EntityUpdateEvent.DestroyEntity);
        }
    }

    public static void HandleTransform(PacketManager.Packet packet, NetworkManager instance)
    {
        PacketManager.TransformData transformData = JsonUtility.FromJson<PacketManager.TransformData>(packet.data);
        if (instance.entities.ContainsKey(transformData.entity_id))
        {
            NetworkManager.Entity updateEntity = new NetworkManager.Entity();
            updateEntity.id = transformData.entity_id;
            
            updateEntity.position = new Vector3(transformData.position.x, transformData.position.y, transformData.position.z);
            updateEntity.rotation = new Quaternion(transformData.rotation.x, transformData.rotation.y, transformData.rotation.z, transformData.rotation.w);
            updateEntity.isLocalPlayer = false;

            instance.entityUpdateQueue.Add(updateEntity, NetworkManager.EntityUpdateEvent.UpdateEntityTransform);
        }
        else
        {
            instance.entitySpawnQueue.Add(transformData.entity_id);
        }
    }
}