using System.Collections;
using UnityEngine;

public class AxeController : MonoBehaviour
{
    public Transform axeView;
    public Transform cameraTransform;

    float cooldown;
    bool swinging;
    Vector3 restPos = new Vector3(0.32f, -0.34f, 0.55f);
    Vector3 restRot = new Vector3(15f, -15f, -10f);
    Vector3 swingRot = new Vector3(-70f, 25f, 60f);
    float swingT;

    public static System.Action OnSwing;

    void Start()
    {
        if (axeView == null && cameraTransform != null)
        {
            axeView = cameraTransform.Find("AxeView");
        }
        if (axeView == null)
        {
            var model = Resources.Load<GameObject>("Models/axe");
            if (model != null && cameraTransform != null)
            {
                var go = new GameObject("AxeView");
                go.transform.SetParent(cameraTransform, false);
                var inst = Instantiate(model, go.transform);
                inst.name = "AxeModel";
                inst.transform.localPosition = new Vector3(0, -0.05f, 0);
                inst.transform.localRotation = Quaternion.Euler(-90f, 0, 0); // align handle to view
                inst.transform.localScale = Vector3.one * 0.9f;
                axeView = go.transform;
            }
        }
    }

    void Update()
    {
        if (cooldown > 0) cooldown -= Time.deltaTime;
        if (axeView == null) return;

        if (Input.GetMouseButtonDown(0) && cooldown <= 0 && !swinging && Cursor.lockState == CursorLockMode.Locked)
        {
            var gm = GameManager.Instance;
            float cd = gm != null ? gm.AxeCooldown : 0.8f;
            cooldown = cd;
            swinging = true;
            swingT = 0;
            OnSwing?.Invoke();
            AudioManager.Instance?.Play("chop");
            StartCoroutine(HitCheck());
        }

        // animate swing
        if (swinging)
        {
            swingT += Time.deltaTime / 0.35f;
            float k = Mathf.Clamp01(swingT);
            float arc = Mathf.Sin(k * Mathf.PI); // 0->1->0
            axeView.localPosition = restPos + new Vector3(0.1f * arc, -0.15f * arc, -0.1f * arc);
            axeView.localRotation = Quaternion.Euler(
                Mathf.Lerp(restRot.x, swingRot.x, arc),
                Mathf.Lerp(restRot.y, swingRot.y, arc),
                Mathf.Lerp(restRot.z, swingRot.z, arc));
            if (k >= 1f)
            {
                swinging = false;
                axeView.localPosition = restPos;
                axeView.localRotation = Quaternion.Euler(restRot);
            }
        }
        else
        {
            // idle sway
            float sway = Mathf.Sin(Time.time * 1.5f) * 0.01f;
            axeView.localPosition = restPos + new Vector3(sway, sway * 0.5f, 0);
            axeView.localRotation = Quaternion.Euler(restRot + new Vector3(0, 0, sway * 30f));
        }
    }

    IEnumerator HitCheck()
    {
        yield return new WaitForSeconds(0.18f); // mid-swing
        if (cameraTransform == null) yield break;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 3.6f))
        {
            var tree = hit.collider.GetComponentInParent<Tree>();
            if (tree != null && tree.state == Tree.State.Standing)
            {
                int dmg = GameManager.Instance != null ? GameManager.Instance.AxeDamage : 1;
                tree.TakeDamage(dmg);
                AudioManager.Instance?.Play("hit");
            }
        }
    }
}
