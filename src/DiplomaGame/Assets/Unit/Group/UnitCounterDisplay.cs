using TMPro;
using UnityEngine;

public class UnitCounterDisplay : MonoBehaviour
{
    private TextMeshPro _textComponent;
    private GameObject _textObject;
    private int _currentCount = 0;

    private Vector3 _textOffset = new(0.1f, 0.2f, 0);
    private Color _textColor = Color.white;
    private float _textSize = 30.0f;

    private void Awake()
    {
        CreateTextObject();
    }

    private void CreateTextObject()
    {
        _textObject = new GameObject("UnitCounter");
        _textObject.transform.SetParent(transform);
        _textObject.transform.localPosition = _textOffset;

        _textComponent = _textObject.AddComponent<TextMeshPro>();

        _textComponent.alignment = TextAlignmentOptions.Center;
        _textComponent.fontSize = _textSize;
        _textComponent.color = _textColor;
        _textObject.layer = LayerMask.NameToLayer("GameplayUI");

        _textObject.SetActive(false);
    }

    public void UpdateCount(int count)
    {
        _currentCount = count;

        if (count <= 1)
        {
            _textObject.SetActive(false);
            return;
        }

        _textObject.SetActive(true);
        _textComponent.text = count.ToString();
    }

    public void RotateText(bool toLeft)
    {
        var scale = _textObject.transform.localScale;

        var absScaleX = Mathf.Abs(scale.x);

        _textObject.transform.localScale = new Vector3(
            toLeft ? -absScaleX : absScaleX,
            scale.y,
            scale.z
        );
    }

    private void OnDestroy()
    {
        if (_textObject != null)
        {
            Destroy(_textObject);
        }
    }
}
