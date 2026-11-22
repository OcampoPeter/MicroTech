// WHERE TO PUT THIS FILE: Assets/Codes/RamButton.cs
// Then drag it onto the HoverZone object of each RAM button.
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class RamButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("The RAM stick that moves up (RamModel)")]
    public Transform moveTarget;
    public float raiseAmount = 0.3f;
    public UnityEvent onClick;

    Vector3 startPos;

    void Awake()
    {
        startPos = moveTarget.position;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        moveTarget.position = startPos + Vector3.up * raiseAmount;
    }

    public void OnPointerExit(PointerEventData e)
    {
        moveTarget.position = startPos;
    }

    public void OnPointerClick(PointerEventData e)
    {
        onClick.Invoke();
    }
}