using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    public float walkSpeed = 5.2f;
    public float sprintSpeed = 8.5f;
    public float jumpHeight = 1.1f;
    public float gravity = -22f;
    public float mouseSensitivity = 2.2f;

    CharacterController cc;
    float pitch, yaw;
    Vector3 vel;
    float bobPhase;
    float bobAmp = 0.035f;

    public Camera cam;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        yaw = transform.eulerAngles.y;
        cam = GetComponentInChildren<Camera>();
        if (cam == null)
        {
            var c = new GameObject("PlayerCamera");
            c.transform.SetParent(transform, false);
            c.transform.localPosition = new Vector3(0, 1.62f, 0);
            cam = c.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 70f;
            c.AddComponent<AudioListener>();
        }
    }

    public void SetView(float pitchDeg, float yawDeg)
    {
        pitch = pitchDeg; yaw = yawDeg;
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        if (cam != null) cam.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }

    // cursor locking is managed by UIManager (start screen / pause / shop)

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        // look
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -85f, 85f);
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        cam.transform.localRotation = Quaternion.Euler(pitch, 0, 0);

        // move
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        Vector3 move = transform.rotation * input * speed;

        if (cc.isGrounded)
        {
            vel.y = Mathf.Max(vel.y, -2f);
            if (Input.GetButtonDown("Jump")) vel.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
        }
        vel.y += gravity * Time.deltaTime;
        cc.Move((move + Vector3.up * vel.y) * Time.deltaTime);

        // head bob
        float moving = input.magnitude;
        bobPhase += Time.deltaTime * (moving > 0.1f && cc.isGrounded ? (Input.GetKey(KeyCode.LeftShift) ? 11f : 8f) : 2f);
        float targetAmp = (moving > 0.1f && cc.isGrounded) ? bobAmp : 0f;
        cam.transform.localPosition = new Vector3(0, 1.62f + Mathf.Sin(bobPhase * 2f) * targetAmp, 0);
        cam.transform.localRotation = Quaternion.Euler(pitch, Mathf.Sin(bobPhase) * targetAmp * 20f, 0);
    }
}
