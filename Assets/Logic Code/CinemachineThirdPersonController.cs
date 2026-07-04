using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineFreeLook))]
public class CinemachineThirdPersonController : MonoBehaviour
{
    public Transform player;
    public float horizontalSpeed = 2f;
    public float verticalSpeed = 1.5f;
    public float mouseSensitivity = 100f;
    public bool invertY = false;

    private CinemachineFreeLook freeLook;

    void Awake()
    {
        freeLook = GetComponent<CinemachineFreeLook>();
        if (freeLook == null)
        {
            Debug.LogError("CinemachineThirdPersonController requires a CinemachineFreeLook component.");
            enabled = false;
            return;
        }

        if (player != null)
        {
            freeLook.Follow = player;
            freeLook.LookAt = player;
        }
    }

    void Update()
    {
        if (freeLook == null || player == null)
            return;

        float mouseX = Input.GetAxis("Mouse X") * horizontalSpeed * mouseSensitivity * Time.deltaTime;
        float rawMouseY = Input.GetAxis("Mouse Y") * verticalSpeed * mouseSensitivity * Time.deltaTime;
        float mouseY = invertY ? rawMouseY : -rawMouseY;

        freeLook.m_XAxis.Value += mouseX;
        // Clamp Y to FreeLook's configured bounds (not hard 0..1)
        float minY = freeLook.m_YAxis.m_MinValue;
        float maxY = freeLook.m_YAxis.m_MaxValue;
        freeLook.m_YAxis.Value = Mathf.Clamp(freeLook.m_YAxis.Value + mouseY, minY, maxY);
    }
}
