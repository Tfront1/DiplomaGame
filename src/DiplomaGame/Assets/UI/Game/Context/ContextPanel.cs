using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ContextPanel : MonoBehaviour
{
    private GameObject _contextPrefab;
    private Transform _contextPanel;
    private TextMeshProUGUI _contextText;

    private Canvas _mainCanvas;

    private static ContextPanel _instance;
    public static ContextPanel Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ContextPanel>();
                if (_instance == null)
                {
                    var gameObject = new GameObject("ContextPanel");
                    _instance = gameObject.AddComponent<ContextPanel>();
                }
            }
            return _instance;
        }
    }
    public void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (_contextPrefab == null)
                _contextPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Context/ContextTextPrefab");

            _mainCanvas = MainCanvasUI.MainCanvas;
            InitializeUI();
        }
    }

    private void InitializeUI()
    {
        var contextPrefab = _contextPrefab.transform.Find("Panel");
        _contextPanel = Instantiate(contextPrefab, _mainCanvas.transform);
        _contextPanel.name = "ContextMenu";
        _contextPanel.gameObject.SetActive(false);

        _contextText = _contextPanel.transform.Find("Text").GetComponent<TextMeshProUGUI>();

        var canvasGroup = _contextPanel.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    public void ShowContext(string text)
    {
        if (_contextPanel != null)
        {
            _contextText.text = text;
            var textRect = _contextText.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);

            var textSize = textRect.sizeDelta;
            var panelRect = _contextPanel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(textSize.x, textSize.y);

            var panelLayout = _contextPanel.GetComponent<LayoutElement>();
            panelLayout.preferredWidth = textSize.x;
            panelLayout.preferredHeight = textSize.y;

            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

            Vector2 mousePosition = Input.mousePosition;
            var panelSize = panelRect.sizeDelta;
            var offset = new Vector2(10, 10);
            var panelPosition = mousePosition + offset;

            var rightEdge = panelPosition.x + panelSize.x;
            if (rightEdge > Screen.width)
            {
                panelPosition.x = mousePosition.x - panelSize.x - offset.x;
            }

            var bottomEdge = panelPosition.y - panelSize.y;
            if (bottomEdge < 0)
            {
                panelPosition.y = mousePosition.y + panelSize.y + offset.y;
            }

            _contextPanel.position = panelPosition;
            _contextPanel.gameObject.SetActive(true);
        }
    }

    public void HideContext()
    {
        if (_contextPanel != null)
            _contextPanel.gameObject.SetActive(false);
    }
}