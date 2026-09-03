using UnityEngine;

[ExecuteAlways]
public sealed class CameraPinnedBackground : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool followHorizontal = true;
    [SerializeField] private bool followVertical;

    private void OnEnable()
    {
        SnapToCamera();
    }

    private void LateUpdate()
    {
        SnapToCamera();
    }

    public void SnapToCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        Vector3 position = transform.position;
        if (followHorizontal)
        {
            position.x = targetCamera.transform.position.x;
        }

        if (followVertical)
        {
            position.y = targetCamera.transform.position.y;
        }

        transform.position = position;
    }
}
