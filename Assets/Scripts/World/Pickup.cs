using UnityEngine;

public class Pickup : MonoBehaviour
{
    public enum Kind { Log, Plank }
    public Kind kind;
    public float value;

    Transform player;
    Vector3 vel;
    bool magnetized;
    float life = 90f;

    public static System.Action<Pickup> OnCollected;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        vel = new Vector3(Random.Range(-1f, 1f), Random.Range(2f, 4f), Random.Range(-1f, 1f));
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (life < 0) { Destroy(gameObject); return; }

        if (player != null)
        {
            Vector3 to = player.position - transform.position;
            float d = to.magnitude;
            if (d < 3f) magnetized = true;
            if (magnetized)
            {
                transform.position += to.normalized * Mathf.Min(10f, 14f / Mathf.Max(d, 0.5f)) * Time.deltaTime;
                transform.Rotate(0, 360f * Time.deltaTime, 0);
                if (d < 1.1f) TryCollect();
            }
            else
            {
                // physics-ish: gravity + ground stick
                vel.y -= 12f * Time.deltaTime;
                transform.position += vel * Time.deltaTime;
                float ground = TerrainMath.Height(transform.position.x, transform.position.z);
                if (transform.position.y < ground + 0.35f)
                {
                    transform.position = new Vector3(transform.position.x, ground + 0.35f, transform.position.z);
                    vel = Vector3.zero;
                    vel.y = 0;
                    // settle: small random drift
                    vel = new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f));
                }
            }
        }
    }

    void TryCollect()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (!gm.HasCarrySpace(1)) return; // stays until space
        if (kind == Kind.Log) gm.logsCarried++;
        else gm.planksCarried++;
        OnCollected?.Invoke(this);
        Destroy(gameObject);
        gm.Notify();
    }
}
