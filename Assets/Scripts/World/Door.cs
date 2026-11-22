using UnityEngine;

// Hinged door leaf: E to open/close, collider disabled while open.
public class Door : MonoBehaviour, IInteractable
{
    public Transform leaf;
    public BoxCollider leafCol;
    public bool isOpen;
    public float angle;
    const float OpenAngle = 105f;

    public string Prompt => isOpen ? "[E] Закрыть дверь" : "[E] Открыть дверь";
    public float Range => 3.2f;

    public void Interact()
    {
        isOpen = !isOpen;
        AudioManager.Instance?.Play("door");
    }

    void Update()
    {
        float target = isOpen ? OpenAngle : 0f;
        if (Mathf.Approximately(angle, target)) return;
        angle = Mathf.MoveTowards(angle, target, 160f * Time.deltaTime);
        if (leaf != null) leaf.localRotation = Quaternion.Euler(0f, angle, 0f);
        if (leafCol != null) leafCol.enabled = angle < 30f;
    }
}
