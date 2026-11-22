using UnityEngine;

// Makes any object usable via E key with a prompt.
public class Interactable : MonoBehaviour, IInteractable
{
    public string prompt = "[E] Использовать";
    public float range = 4f;
    public System.Action onInteract;

    public string Prompt => prompt;
    public float Range => range;
    public void Interact() => onInteract?.Invoke();
}
