using UnityEngine.EventSystems;
using UnityEngine;

public class UIElementContext : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string contextText = "Data";

    public void SetContextText(string text)
    {
        contextText = text;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ContextPanel.Instance.ShowContext(contextText);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ContextPanel.Instance.HideContext();
    }
}