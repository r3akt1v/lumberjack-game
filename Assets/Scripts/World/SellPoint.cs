using UnityEngine;

public class SellPoint : MonoBehaviour
{
    public void Sell()
    {
        GameManager.Instance?.SellResources();
        AudioManager.Instance?.Play("sell");
    }
}
