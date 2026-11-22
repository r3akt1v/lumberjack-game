using UnityEngine;

public interface IInteractable
{
    string Prompt { get; }
    void Interact();
    float Range { get; }
}

public class Interaction : MonoBehaviour
{
    public Transform cameraTransform;
    IInteractable current;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        current = null;
        if (cameraTransform != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 4f))
            {
                var it = hit.collider.GetComponentInParent<IInteractable>();
                if (it != null) current = it;
            }
        }

        UIManager.Instance?.SetPrompt(current != null ? current.Prompt : null);

        if (Input.GetKeyDown(KeyCode.E) && current != null)
            current.Interact();
    }
}
