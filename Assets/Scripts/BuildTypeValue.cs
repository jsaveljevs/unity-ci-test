using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class BuildTypeValue : MonoBehaviour
{
    private void Awake()
    {
        var text = GetComponent<Text>();

#if DEV_BUILD
        text.text = "DEV";
#else
        text.text = "PROD";
#endif
    }
}
