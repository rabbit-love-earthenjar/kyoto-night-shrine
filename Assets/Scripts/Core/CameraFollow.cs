using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, -10f);
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 22f;
    [SerializeField] private float minY = -4f;
    [SerializeField] private float maxY = 12f;
    [SerializeField] private bool lockVertical = true;
    [SerializeField] private bool useVerticalRouteBands;
    [SerializeField] private float lowerRouteCameraY;
    [SerializeField] private float upperRouteCameraY;
    [SerializeField] private float routeBandSwitchY;

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
        else if (useVerticalRouteBands)
        {
            desiredPosition.y = target.position.y >= routeBandSwitchY
                ? upperRouteCameraY
                : lowerRouteCameraY;
        }
        else
        {
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }

    public void ConfigureRouteBounds(
        Transform followTarget,
        float horizontalMin,
        float horizontalMax,
        float verticalMin,
        float verticalMax)
    {
        target = followTarget;
        minX = Mathf.Min(horizontalMin, horizontalMax);
        maxX = Mathf.Max(horizontalMin, horizontalMax);
        minY = Mathf.Min(verticalMin, verticalMax);
        maxY = Mathf.Max(verticalMin, verticalMax);
        lockVertical = false;
        useVerticalRouteBands = false;
    }

    public void ConfigureRouteBands(
        Transform followTarget,
        float horizontalMin,
        float horizontalMax,
        float lowerCameraY,
        float upperCameraY,
        float switchY)
    {
        ConfigureRouteBounds(followTarget, horizontalMin, horizontalMax, lowerCameraY, upperCameraY);
        lowerRouteCameraY = Mathf.Min(lowerCameraY, upperCameraY);
        upperRouteCameraY = Mathf.Max(lowerCameraY, upperCameraY);
        routeBandSwitchY = switchY;
        useVerticalRouteBands = true;
    }
}
