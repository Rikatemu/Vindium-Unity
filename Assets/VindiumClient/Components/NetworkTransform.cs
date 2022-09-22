using System.Collections;
using UnityEngine;

/// <summary>NetworkTransform synces position to or from the server based on the local object authority.</summary>
public class NetworkTransform : NetworkBehaviour
{
    public float syncRateSeconds = 0.1f;
    public float lerpRate = 15;
    public float positionThreshold = 0.5f;
    public float rotationThreshold = 0.5f;
    public Vector3 newPosition = Vector3.zero;
    public Quaternion newRotation = Quaternion.identity;

    private Vector3 lastPosition;
    private Quaternion lastRotation;

    private void Start()
    {
        newPosition = transform.position;
        newRotation = transform.rotation;

        if (networkIdentity.isLocalPlayer)
        {
            StartCoroutine(SyncTransform());
        }
    }

    private void Update()
    {
        // If is not local player, lerp to the new position
        if (!networkIdentity.isLocalPlayer) {
            if (transform.position != newPosition)
            {
                transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * lerpRate);
            }

            if (transform.rotation != newRotation)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * lerpRate);
            }
        }
    }

    private IEnumerator SyncTransform()
    {
        while (true)
        {
            // Sync only if change is detected
            if (Vector3.Distance(transform.position, lastPosition) > positionThreshold || Quaternion.Angle(transform.rotation, lastRotation) > rotationThreshold)
            {
                lastPosition = transform.position;
                lastRotation = transform.rotation;
                PacketManager.SendTransform(networkIdentity.id, transform.position, transform.rotation);
            }

            yield return new WaitForSeconds(syncRateSeconds);
        }
    }
}
