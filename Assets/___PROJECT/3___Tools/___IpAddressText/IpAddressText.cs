using TMPro;
using UnityEngine;

public class IpAddressText : MonoBehaviour
{
    public TextMeshProUGUI text;

    public void SetIpAddressText()
    {
        text.text = GameManager.Inst.GetLocalIPAddress();
    }
}
