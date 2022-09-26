using UnityEngine;

public class MovementController : NetworkBehaviour
{
    public MeshRenderer meshRenderer;
    public GameObject cam;
    public MouseLook mouseLook;
    public Rigidbody rb;

    private void Start()
    {
        meshRenderer.material.color = Random.ColorHSV();

        if (networkIdentity.isLocalPlayer)
        {
            cam.SetActive(true);
            mouseLook.enabled = true;
        } else {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    private void Update()
    {
        if (!networkIdentity.isLocalPlayer) return;

        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * Time.deltaTime * 5;
        }

        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= transform.forward * Time.deltaTime * 5;
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= transform.right * Time.deltaTime * 5;
        }

        if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * Time.deltaTime * 5;
        }
    }
}
