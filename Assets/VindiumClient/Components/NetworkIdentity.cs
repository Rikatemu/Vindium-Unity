using UnityEngine;

/// <summary>Handles network identity of the object like ID, if it is owned by a local player, etc...</summary>
public class NetworkIdentity : MonoBehaviour
{
    /// <summary>Network manager that handles communication with the server.</summary>
    [HideInInspector] public NetworkManager networkManager;

    /// <summary>Entity's unique ID.</summary>
    public string id;

    /// <summary>True if this Entity is the the client's own local player.</summary>
    public bool isLocalPlayer = false;
}
