using System.Collections;
using UnityEngine;

public class Tree : MonoBehaviour
{
    public enum State { Standing, Falling, Fallen, Stump, Regrowing }

    public State state = State.Standing;
    public int maxHp = 4;
    public int hp;
    public float respawnTime = 75f;
    public float fallDuration = 1.4f;

    Transform visual;
    Quaternion standRot;
    Vector3 standPos;
    float stateTimer;
    Vector3 fallAxis;
    float fallAngle;
    System.Random rng = new System.Random();

    public static System.Action<Tree> OnChopped;
    public static System.Action<Tree> OnFelled;

    void Awake()
    {
        visual = transform.Find("Visual");
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
        standPos = transform.position;
        standRot = transform.rotation;
        hp = maxHp;
    }

    public void TakeDamage(int dmg)
    {
        if (state != State.Standing) return;
        hp -= dmg;
        // punch animation
        StopAllCoroutines();
        StartCoroutine(Punch());
        if (hp <= 0) StartCoroutine(Fall());
    }

    IEnumerator Punch()
    {
        float t = 0;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            float s = 1f - Mathf.Sin(t / 0.18f * Mathf.PI) * 0.06f;
            if (visual != null) visual.localScale = new Vector3(s, 2f - s, s);
            yield return null;
        }
        if (visual != null) visual.localScale = Vector3.one;
    }

    IEnumerator Fall()
    {
        state = State.Falling;
        GameManager.Instance.treesChopped++;
        OnChopped?.Invoke(this);
        fallAxis = Vector3.Cross(Vector3.up, new Vector3(
            (float)rng.NextDouble() - 0.5f, 0, (float)rng.NextDouble() - 0.5f)).normalized;
        if (fallAxis.sqrMagnitude < 0.01f) fallAxis = Vector3.right;
        Vector3 pivot = standPos + Vector3.up * 0.3f;
        float t = 0;
        Quaternion start = transform.rotation;
        Quaternion end = start * Quaternion.AngleAxis(82f, fallAxis);
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float k = t / fallDuration;
            k = k * k * (3f - 2f * k); // smoothstep
            transform.rotation = Quaternion.Slerp(start, end, k);
            // keep base near ground: rotate around pivot via position offset
            transform.position = pivot - transform.rotation * (pivot - standPos);
            yield return null;
        }
        state = State.Fallen;
        stateTimer = 0;
        OnFelled?.Invoke(this);
    }

    void Update()
    {
        if (state == State.Fallen)
        {
            stateTimer += Time.deltaTime;
            if (stateTimer > 5f)
            {
                // sink into ground and become stump
                transform.position += Vector3.down * Time.deltaTime * 0.8f;
                if (stateTimer > 7.5f)
                {
                    BecomeStump();
                    StartCoroutine(Regrow());
                }
            }
        }
    }

    void BecomeStump()
    {
        var stumpModel = Resources.Load<GameObject>("Models/stump");
        if (stumpModel != null)
        {
            foreach (Transform c in transform) Destroy(c.gameObject);
            var st = Instantiate(stumpModel, transform);
            st.name = "Visual";
            st.transform.localPosition = Vector3.zero;
        }
        state = State.Stump;
    }

    IEnumerator Regrow()
    {
        yield return new WaitForSeconds(respawnTime);
        var treeModel = Resources.Load<GameObject>("Models/tree_pine");
        if (treeModel != null)
        {
            foreach (Transform c in transform) Destroy(c.gameObject);
            var tr = Instantiate(treeModel, transform);
            tr.name = "Visual";
            tr.transform.localPosition = Vector3.zero;
            visual = tr.transform;
        }
        transform.position = standPos;
        transform.rotation = standRot;
        hp = maxHp;
        state = State.Standing;
    }
}
