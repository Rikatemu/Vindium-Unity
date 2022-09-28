using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

/// <summary>Network manager that handles communication with the server.</summary>
public class NetworkManager : MonoBehaviour
{
    // Must precisely match the server tickrate!
    // This is the time between each tick in milliseconds.
    // 30 ticks per 1 second - 33.3333ms per tick (1000 / 30)
    public const float TIME_BETWEEN_TICKS_MS = 33.3333f;
    public const int DATA_BUFFER_SIZE = 2048;

    public static NetworkManager instance;
    public string ipAddress = "127.0.0.1";
    public int port = 8080;
    public bool autoConnect = true;
    public GameObject playerPrefab;

    [HideInInspector] public TCP tcp;
    [HideInInspector] public UDP udp;
    [HideInInspector] public bool connected = false;
    [HideInInspector] public IPAddress ip;
    [HideInInspector] public Dictionary<string, GameObject> entities = new Dictionary<string, GameObject>();
    [HideInInspector] public Dictionary<Entity,EntityUpdateEvent> entityUpdateQueue = new Dictionary<Entity, EntityUpdateEvent>();
    [HideInInspector] public List<string> entitySpawnQueue = new List<string>();
    [HideInInspector] public string id = "";
    [HideInInspector] public bool firstRead = true;

    public struct Entity {
        public string id;
        public string type;
        public bool isLocalPlayer;
        public Vector3 position;
        public Quaternion rotation;
    }

    public enum EntityUpdateEvent {
        UpdateEntityTransform,
        DestroyEntity
    }

    private void Awake()
    {
        ip = IPAddress.Parse(ipAddress);

        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    private void Start()
    {
        tcp = new TCP();
        udp = new UDP();

        if (autoConnect)
        {
            Connect();
        }

        StartCoroutine(EntitySpawnQueue());
        StartCoroutine(EntityUpdateQueue());
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    private IEnumerator EntitySpawnQueue()
    {
        while (true)
        {
            if (entitySpawnQueue.Count > 0)
            {
                List<string> entitySpawnQueueClone = new List<string>(entitySpawnQueue);
                foreach (string entityId in entitySpawnQueueClone)
                {
                    SpawnEntity(entityId);
                }
                entitySpawnQueue.Clear();
            }

            yield return new WaitForSeconds(0.25f);
        }
    }
    
    private IEnumerator EntityUpdateQueue()
    {
        while (true)
        {
            if (entityUpdateQueue.Count > 0)
            {
                Dictionary<Entity,EntityUpdateEvent> entityUpdateQueueClone = new Dictionary<Entity, EntityUpdateEvent>(entityUpdateQueue);
                foreach (KeyValuePair<Entity,EntityUpdateEvent> queueRecord in entityUpdateQueueClone)
                {
                    switch (queueRecord.Value)
                    {
                        case EntityUpdateEvent.UpdateEntityTransform:
                            UpdateTransform(queueRecord.Key);
                            break;
                        case EntityUpdateEvent.DestroyEntity:
                            DestroyEntity(queueRecord.Key);
                            break;
                    }
                }
                entityUpdateQueue.Clear();
            }

            yield return new WaitForSeconds(0);
        }
    }

    private void Connect()
    {
        tcp.ConnectTCP();
    }

    private void Disconnect()
    {
        tcp.DisconnectTCP();
        udp.DisconnectUDP();
    }

    private void SpawnEntity(string entityId)
    {
        if (entities.ContainsKey(entityId)) return;

        GameObject entity = Instantiate(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        entities.Add(entityId, entity);
        NetworkIdentity playerNetworkIdentity = entity.GetComponent<NetworkIdentity>();

        playerNetworkIdentity.networkManager = this;
        playerNetworkIdentity.id = entityId;

        if (entityId == id)
        {
            playerNetworkIdentity.isLocalPlayer = true;
        }
    }

    private void UpdateTransform(Entity entity)
    {
        if (!instance.entities.ContainsKey(entity.id)) return;

        GameObject entity_obj = instance.entities[entity.id];
        NetworkIdentity entityNetworkIdentity = entity_obj.GetComponent<NetworkIdentity>();
        NetworkTransform entityNetworkTransform = entity_obj.GetComponent<NetworkTransform>();

        entityNetworkIdentity.isLocalPlayer = false;
        entityNetworkIdentity.id = entity.id;
        entityNetworkTransform.newPosition = entity.position;
        entityNetworkTransform.newRotation = entity.rotation;
    }

    private void DestroyEntity(Entity entity)
    {
        if (!instance.entities.ContainsKey(entity.id)) return;

        GameObject entity_obj = instance.entities[entity.id];
        instance.entities.Remove(entity.id);
        Destroy(entity_obj);
    }
}
