using UnityEngine;

[AddComponentMenu("Camera/Third Person Camera")]
public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target; // personagem (coloque o "head" ou pivot)
    public Vector3 targetOffset = new Vector3(0, 1.6f, 0);

    [Header("Orbit")]
    public float distance = 3.5f;
    public float minDistance = 1.5f;
    public float maxDistance = 5.5f;
    public float sensitivityX = 120f;
    public float sensitivityY = 90f;
    public float orbitSmoothTime = 0.05f;
    public float pitchMin = -35f;
    public float pitchMax = 60f;

    [Header("Collision")]
    public LayerMask collisionLayers;
    public float cameraRadius = 0.3f;
    public float collisionOffset = 0.2f; // small offset from collision

    private float yaw = 0f;
    private float pitch = 10f;
    private Vector3 currentVel = Vector3.zero;
    private float currentDistance;

    void Start()
    {
        currentDistance = distance;
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Read mouse input
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * sensitivityX * Time.deltaTime;
        pitch -= mouseY * sensitivityY * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // desired camera position
        Vector3 targetCenter = target.position + targetOffset;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);

        // handle collision: raycast from target center towards desired camera pos
        Vector3 desiredCamPos = targetCenter + rot * new Vector3(0, 0, -distance);

        RaycastHit hit;
        Vector3 dir = (desiredCamPos - targetCenter).normalized;
        float desiredDist = distance;

        // spherecast to prevent clipping
        if (Physics.SphereCast(targetCenter, cameraRadius, dir, out hit, distance + cameraRadius, collisionLayers))
        {
            desiredDist = Mathf.Max( minDistance, hit.distance - collisionOffset );
        }

        // allow zoom with mouse wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentDistance = Mathf.Clamp(currentDistance - scroll * 2f, minDistance, maxDistance);
        // pick the smaller of collision-constrained distance and user-chosen distance
        float finalDist = Mathf.Min(desiredDist, currentDistance);

        Vector3 finalPos = Vector3.SmoothDamp(transform.position, targetCenter + rot * new Vector3(0, 0, -finalDist), ref currentVel, orbitSmoothTime);
        transform.position = finalPos;
        transform.rotation = rot;

        // optionally, camera can look at target center (for small offsets)
        transform.LookAt(targetCenter);
    }
}

