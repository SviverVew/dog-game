using UnityEngine;

public class DistanceCulling : MonoBehaviour
{
    public Transform player;
    public float distance = 100f;

    void Update()
    {
        float d = Vector3.Distance(player.position, transform.position);

        gameObject.SetActive(d < distance);
    }
}