using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, -10f);
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 22f;
    [SerializeField] private bool lockVertical = true;

    private float startY;

    private void Awake()
    {
        startY = transform.position.y;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);

        if (lockVertical)
        {
            desiredPosition.y = startY;
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }
}
