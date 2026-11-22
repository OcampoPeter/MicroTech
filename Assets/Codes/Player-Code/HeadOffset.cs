using UnityEngine;

// HEAD OFFSET + HEAD LOOK. Put this on the Player (same as before).
// 1) Lowers the neck so the head sits closer to the shoulders (Neck Lower).
// 2) Fixes a head that tilts up (Head Euler).
// 3) Turns the head toward where the camera looks.
// It runs after the Animator, so it sits on top of the animation.
public class HeadOffset : MonoBehaviour
{
    [Header("References (leave empty, found by itself)")]
    public Animator animator;
    public Transform lookCamera;

    [Header("Neck height")]
    [Tooltip("Moves the neck (and head) DOWN, in meters. Start at 0.05. Too much will squash the shoulders.")]
    public float neckLower = 0.05f;

    [Header("Tilt fix")]
    [Tooltip("Degrees added to the head. Use the number that fixed the tilt.")]
    public Vector3 headEuler = new Vector3(10f, 0f, 0f);

    [Header("Head look")]
    public bool headLook = true;
    [Tooltip("Most the head can turn left or right (degrees).")]
    public float maxYaw = 80f;
    [Tooltip("Most the head can nod up or down (degrees).")]
    public float maxPitch = 30f;
    [Tooltip("How much of the camera's up/down look the head copies. 0 = never nods.")]
    [Range(0f, 1f)] public float pitchAmount = 0.5f;
    [Tooltip("How much of the turn goes to the neck (the rest goes to the head).")]
    [Range(0f, 1f)] public float neckShare = 0.4f;
    [Tooltip("How fast the head follows. Higher = snappier, lower = smoother.")]
    public float followSpeed = 8f;

    private Transform head, neck;
    private float yaw, pitch;

    void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman) return;
        head = animator.GetBoneTransform(HumanBodyBones.Head);
        neck = animator.GetBoneTransform(HumanBodyBones.Neck);

        if (lookCamera == null)
        {
            PlayerMovement m = GetComponent<PlayerMovement>();
            if (m != null) lookCamera = m.playerCamera;
            if (lookCamera == null && Camera.main != null) lookCamera = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (head == null) return;

        // 1) lower the neck (the head is a child of the neck, so it comes down too)
        if (neck != null && neckLower != 0f)
            neck.position += Vector3.down * neckLower;

        // 2) the tilt fix
        head.localRotation *= Quaternion.Euler(headEuler);

        if (!headLook || lookCamera == null) return;

        // 3) where is the camera looking, compared to where the body faces?
        Vector3 body = animator.transform.forward; body.y = 0f; body.Normalize();
        Vector3 look = lookCamera.forward;
        Vector3 lookFlat = new Vector3(look.x, 0f, look.z);
        if (lookFlat.sqrMagnitude < 0.0001f) return;
        lookFlat.Normalize();

        float targetYaw = Vector3.SignedAngle(body, lookFlat, Vector3.up);
        float targetPitch = -Mathf.Asin(Mathf.Clamp(look.y, -1f, 1f)) * Mathf.Rad2Deg * pitchAmount;
        targetYaw = Mathf.Clamp(targetYaw, -maxYaw, maxYaw);
        targetPitch = Mathf.Clamp(targetPitch, -maxPitch, maxPitch);

        float k = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        yaw = Mathf.Lerp(yaw, targetYaw, k);
        pitch = Mathf.Lerp(pitch, targetPitch, k);

        Vector3 right = Vector3.Cross(Vector3.up, body);
        Quaternion turn = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, right);

        Quaternion neckTurn = Quaternion.Slerp(Quaternion.identity, turn, neckShare);
        Quaternion headTurn = Quaternion.Slerp(Quaternion.identity, turn, 1f - neckShare);
        if (neck != null) neck.rotation = neckTurn * neck.rotation;
        head.rotation = headTurn * head.rotation;
    }
}