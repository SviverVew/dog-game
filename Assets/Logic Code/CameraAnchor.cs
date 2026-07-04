using UnityEngine;

/// <summary>
/// CameraAnchor follows the player's position but does not copy rotation.
/// Assign this object's Transform to Cinemachine FreeLook's Follow and LookAt.
/// </summary>
public class CameraAnchor : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0f, 1.6f, 0f);
    public float smoothTime = 0.05f;

    private Vector3 velocity;

    void LateUpdate()
    {
        if (player == null) return;
        Vector3 targetPos = player.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
        transform.rotation = Quaternion.identity; // keep world-aligned, don't inherit player rotation
    }
}
