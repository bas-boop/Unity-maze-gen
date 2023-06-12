using UnityEngine;

public sealed class CameraController : MonoBehaviour
{
    [SerializeField] private Transform tilemap; // Reference to your tilemap object
    [SerializeField] private float padding = 1f; // Padding around the tilemap

    private Camera _mainCamera;

    private void Start() => _mainCamera = GetComponent<Camera>();

    /// <summary>
    /// Sets the camera it's position and look size.
    /// </summary>
    public void SetCameraPosition()
    {
        var tilemapBounds = CalculateTilemapBounds();
        MoveAndZoomCamera(tilemapBounds);
    }

    /// <summary>
    /// Calculates the tilemap size.
    /// </summary>
    /// <returns>The size of the tilemap</returns>
    private Bounds CalculateTilemapBounds()
    {
        var tilemapRenderers = tilemap.GetComponentsInChildren<Renderer>();

        if (tilemapRenderers.Length == 0) return new Bounds(tilemap.position, Vector3.zero);

        var bounds = tilemapRenderers[0].bounds;

        for (int i = 1; i < tilemapRenderers.Length; i++)
        {
            bounds.Encapsulate(tilemapRenderers[i].bounds);
        }

        return bounds;
    }

    /// <summary>
    /// Sets the zoom of the camera at the good distance, with the tilemap it's size.
    /// </summary>
    /// <param name="targetBounds">The tilemap size.</param>
    private void MoveAndZoomCamera(Bounds targetBounds)
    {
        var targetPosition = targetBounds.center;
        var targetSize = Mathf.Max(targetBounds.size.x, targetBounds.size.y) * 0.5f;

        _mainCamera.transform.position = new Vector3(targetPosition.x, targetPosition.y, _mainCamera.transform.position.z);
        _mainCamera.orthographicSize = targetSize + padding;
    }
}
