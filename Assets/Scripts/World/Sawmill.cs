using System.Collections.Generic;
using UnityEngine;

public class Sawmill : MonoBehaviour
{
    public float processTime = 2.5f; // seconds per log->plank
    Queue<int> queue = new Queue<int>();
    float timer;
    Transform blade;
    bool playerInside;

    void Start()
    {
        var b = transform.Find("Blade");
        if (b == null)
        {
            foreach (Transform c in transform)
                if (c.name.ToLower().Contains("blade")) { blade = c; break; }
        }
        else blade = b;
    }

    void Update()
    {
        var gm0 = GameManager.Instance;
        if (queue.Count > 0 && gm0 != null && gm0.HasCarrySpace(1))
        {
            timer += Time.deltaTime;
            if (blade != null) blade.Rotate(0, 0, 720f * Time.deltaTime);
            if (timer >= processTime)
            {
                timer = 0;
                queue.Dequeue();
                gm0.planksCarried += GameManager.LogsPerPlank;
                gm0.Notify();
            }
        }
        else if (blade != null)
        {
            blade.Rotate(0, 0, Mathf.Lerp(blade.localEulerAngles.z, 0f, Time.deltaTime * 2f));
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var p = other.GetComponentInParent<PlayerTag>();
        if (p != null) playerInside = true;
    }
    void OnTriggerExit(Collider other)
    {
        var p = other.GetComponentInParent<PlayerTag>();
        if (p != null) playerInside = false;
    }

    // called by Interaction when player presses E at sawmill
    public void DepositLogs()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.logsCarried <= 0) return;
        int n = gm.logsCarried;
        gm.logsCarried = 0;
        gm.logsInSawmillQueue += n;
        for (int i = 0; i < n; i++) queue.Enqueue(1);
        gm.Notify();
    }
}
