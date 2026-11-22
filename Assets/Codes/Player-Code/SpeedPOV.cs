using UnityEngine;
using Unity.Cinemachine;

// SPEED FOV. Put this on the Player.
// The camera's field of view gets wider the faster you go, so speed FEELS fast.
// It works on the Cinemachine cameras (first and third person). Slow = normal view.
public class SpeedFOV : MonoBehaviour
{
    [Header("References (leave empty to find them by itself)")]
    public PlayerMovement player;
    [Tooltip("The CinemachineCamera objects to widen. Empty = it finds every CinemachineCamera in the scene.")]
    public CinemachineCamera[] cameras;

    [Header("The effect")]
    [Tooltip("Below this speed (m/s) the view stays normal.")]
    public float startSpeed = 7f;
    [Tooltip("At this speed (m/s) the view is at its widest.")]
    public float fullSpeed = 18f;
    [Tooltip("How many degrees wider the view gets at full speed.")]
    public float extraFOV = 18f;
    [Tooltip("How fast it widens (higher = snappier).")]
    public float widenSpeed = 6f;
    [Tooltip("How fast it goes back to normal.")]
    public float narrowSpeed = 4f;

    private float[] baseFOV;
    private float extra;

    void Start()
    {
        if (player == null) player = GetComponent<PlayerMovement>();
        if (cameras == null || cameras.Length == 0)
            cameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // Remember each camera's normal field of view
        baseFOV = new float[cameras.Length];
        for (int i = 0; i < cameras.Length; i++)
            if (cameras[i] != null) baseFOV[i] = cameras[i].Lens.FieldOfView;
    }

    void LateUpdate()
    {
        if (player == null || cameras == null) return;

        float t = Mathf.InverseLerp(startSpeed, fullSpeed, player.HorizontalSpeed);
        float target = t * extraFOV;

        // Widen quickly, narrow a bit slower
        float rate = target > extra ? widenSpeed : narrowSpeed;
        extra = Mathf.Lerp(extra, target, rate * Time.deltaTime);

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == null) continue;
            cameras[i].Lens.FieldOfView = baseFOV[i] + extra;
        }
    }

    void OnDisable()
    {
        // Put the normal view back
        if (cameras == null || baseFOV == null) return;
        for (int i = 0; i < cameras.Length && i < baseFOV.Length; i++)
            if (cameras[i] != null) cameras[i].Lens.FieldOfView = baseFOV[i];
    }
}