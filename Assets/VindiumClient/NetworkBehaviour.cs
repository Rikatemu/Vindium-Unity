using UnityEngine;

/// <summary>Base class for networked components.</summary>
[RequireComponent(typeof(NetworkIdentity))]
public abstract class NetworkBehaviour : MonoBehaviour
{
    [HideInInspector] public NetworkIdentity networkIdentity;
    protected NetworkManager networkManager;

    private void Reset()
    {
        networkIdentity = GetComponent<NetworkIdentity>();
    }

    private void Awake()
    {
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
    }
}
