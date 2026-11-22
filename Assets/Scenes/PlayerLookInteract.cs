using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLookInteract : MonoBehaviour
{
    [Header("Look Settings")]
    public float maxDistance = 7f;
    public float sphereRadius = 0.5f;

    [Header("Interaction UI")]
    public GameObject pickupUI;         // Shows "Press F to pickup"
    public GameObject controlsUI;       // Shows bottom-left controls while holding
    public GameObject inspectUI;        // Shows when pressing Q while holding

    [Header("Intro Chat & Side Quest UI")]
    public CanvasGroup bottomChatGroup;
    public TMP_Text bottomChatText;
    [TextArea]
    public string introMessage = "Look around the room and check out the parts on the table.";
    public float typeSpeed = 0.035f;        // Seconds per character
    public float holdAfterTyping = 2.5f;    // How long full text stays before fading out
    public float fadeDuration = 0.5f;

    public CanvasGroup sideQuestGroup;
    public TMP_Text sideQuestText;
    [TextArea]
    public string questObjective = "Quest: Inspect and pick up the PC components.";

    [Header("Inspect Quest & Right-Side Damage UI")]
    [TextArea]
    public string insideComponentQuestText = "Quest: Go inside the PC component.";
    public CanvasGroup damageInfoGroup;
    public TMP_Text damageInfoText;
    [TextArea(4, 8)]
    public string possibleDamageText =
        "Power Supply - Possible Damage:\n" +
        "• Bulging, leaking, or blown capacitors\n" +
        "• Burnt cables, melted connectors, or scorch marks\n" +
        "• Stalled fan, heavy dust buildup, or overheating\n" +
        "• Voltage drops, power surges, or internal short";

    private ItemGrabber itemGrabber;
    private bool isInspecting = false;
    private bool hasInspectedOnce = false;
    private Coroutine damageFadeRoutine;

    void Start()
    {
        itemGrabber = FindAnyObjectByType<ItemGrabber>();

        if (pickupUI != null) pickupUI.SetActive(false);
        if (controlsUI != null) controlsUI.SetActive(false);
        if (inspectUI != null) inspectUI.SetActive(false);

        if (sideQuestText != null) sideQuestText.text = questObjective;
        if (damageInfoText != null) damageInfoText.text = possibleDamageText;

        SetupGroupHidden(sideQuestGroup);
        SetupGroupHidden(damageInfoGroup);

        if (bottomChatGroup != null && bottomChatText != null)
        {
            bottomChatText.text = "";
            SetupGroupHidden(bottomChatGroup);
            StartCoroutine(PlayIntroAndQuestSequence());
        }
        else
        {
            ShowGroupInstant(sideQuestGroup);
        }
    }

    private void SetupGroupHidden(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        group.gameObject.SetActive(false);
    }

    private void ShowGroupInstant(CanvasGroup group)
    {
        if (group == null) return;
        group.gameObject.SetActive(true);
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float fromAlpha, float toAlpha, float duration)
    {
        if (group == null) yield break;

        group.gameObject.SetActive(true);
        group.alpha = fromAlpha;

        if (duration <= 0f)
        {
            group.alpha = toAlpha;
            if (Mathf.Approximately(toAlpha, 0f)) group.gameObject.SetActive(false);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            yield return null;
        }

        group.alpha = toAlpha;
        if (Mathf.Approximately(toAlpha, 0f))
        {
            group.gameObject.SetActive(false);
        }
    }

    private void SetDamagePanelVisible(bool visible)
    {
        if (damageInfoGroup == null) return;

        if (damageFadeRoutine != null)
        {
            StopCoroutine(damageFadeRoutine);
        }

        float targetAlpha = visible ? 1f : 0f;
        damageFadeRoutine = StartCoroutine(FadeCanvasGroup(damageInfoGroup, damageInfoGroup.alpha, targetAlpha, fadeDuration));
    }

    private void UpdateSideQuestObjective()
    {
        if (sideQuestText == null) return;
        sideQuestText.text = (isInspecting || hasInspectedOnce) ? insideComponentQuestText : questObjective;
    }

    private IEnumerator PlayIntroAndQuestSequence()
    {
        yield return StartCoroutine(FadeCanvasGroup(bottomChatGroup, 0f, 1f, fadeDuration));

        bottomChatText.text = "";
        foreach (char letter in introMessage)
        {
            bottomChatText.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }

        yield return new WaitForSeconds(holdAfterTyping);

        yield return StartCoroutine(FadeCanvasGroup(bottomChatGroup, 1f, 0f, fadeDuration));

        yield return StartCoroutine(FadeCanvasGroup(sideQuestGroup, 0f, 1f, fadeDuration));
    }

    void Update()
    {
        bool holding = itemGrabber != null && itemGrabber.IsHolding;

        if (!holding)
        {
            if (isInspecting)
            {
                isInspecting = false;
                SetDamagePanelVisible(false);
            }

            if (controlsUI != null) controlsUI.SetActive(false);
            if (inspectUI != null) inspectUI.SetActive(false);

            if (pickupUI == null) return;

            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, maxDistance))
            {
                if (hit.collider.CompareTag("Pickup"))
                {
                    pickupUI.SetActive(true);
                    return;
                }
            }

            pickupUI.SetActive(false);
            return;
        }

        if (pickupUI != null) pickupUI.SetActive(false);

        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            isInspecting = !isInspecting;
            if (isInspecting)
            {
                hasInspectedOnce = true;
                if (damageInfoText != null) damageInfoText.text = possibleDamageText;
            }

            UpdateSideQuestObjective();
            SetDamagePanelVisible(isInspecting);
        }

        if (controlsUI != null) controlsUI.SetActive(!isInspecting);
        if (inspectUI != null) inspectUI.SetActive(isInspecting);
    }
}