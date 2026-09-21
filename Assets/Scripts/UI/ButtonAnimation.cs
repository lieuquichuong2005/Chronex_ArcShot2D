using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimation : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;

    [Header("Normal")]
    [SerializeField] private Sprite backgroundNormal;
    [SerializeField] private Sprite iconNormal;

    [Header("Pressed")]
    [SerializeField] private Sprite backgroundPressed;
    [SerializeField] private Sprite iconPressed;

    public void OnPointerDown(PointerEventData eventData)
    {
        background.sprite = backgroundPressed;

        if (icon != null)
            icon.sprite = iconPressed;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        background.sprite = backgroundNormal;

        if (icon != null)
            icon.sprite = iconNormal;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        background.sprite = backgroundNormal;

        if (icon != null)
            icon.sprite = iconNormal;
    }
}