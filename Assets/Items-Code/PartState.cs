using UnityEngine;

public class PartState : MonoBehaviour
{
    [Header("Must be different for every part, for example RAM or CoolerFan")]
    public string partId = "RAM";

    [Header("How hard this part's maze is (1 = easiest, 5 = hardest)")]
    [Range(1, 5)] public int difficulty = 1;

    [Header("Look: the part's color in each state")]
    public Color brokenColor = new Color(1f, 0.35f, 0.35f);   // reddish
    public Color fixedColor = new Color(0.45f, 1f, 0.55f);    // greenish

    [Header("Optional: objects only shown in one state (sparks, smoke, a green light...)")]
    public GameObject[] showWhenBroken;
    public GameObject[] showWhenFixed;

    public bool IsFixed { get { return PartRepair.IsFixed(partId); } }

    void Start()
    {
        Apply();
    }

    public void Apply()
    {
        bool fixedNow = IsFixed;
        Color c = fixedNow ? fixedColor : brokenColor;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            Material m = r.material;   // makes a copy, so other objects keep their color
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);   // URP
            else if (m.HasProperty("_Color")) m.SetColor("_Color", c);      // built-in
        }

        foreach (GameObject g in showWhenBroken) if (g != null) g.SetActive(!fixedNow);
        foreach (GameObject g in showWhenFixed) if (g != null) g.SetActive(fixedNow);
    }
}