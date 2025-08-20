using System.Collections;
using GameUtilities.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI: MonoBehaviour
{
    public static string TownName { get; set; }
    public static int Seed { get; set; }
    public static bool EnableBots { get; set; }
    public static bool EnableFog { get; set; }

    private Canvas _mainCanvas;

    private Transform _fadeTransform;
    private Image _fadeImage;

    private Button _newGameButton;
    private Image _newGameImage;

    private Button _quitButton;
    private Image _quitImage;

    private float _fadeInDuration = 2f;

    private Transform _newGameTransform;

    private TMP_InputField _townNameField;
    private TMP_InputField _seedField;

    private Toggle _botsToggle;
    private Toggle _fogToggle;

    private Button _backButton;
    private Button _startGameButton;

    public void Awake()
    {
        _mainCanvas = FindObjectOfType<Canvas>();

        _fadeTransform = _mainCanvas.transform.Find("FadePanel");
        _fadeImage = _fadeTransform.GetComponent<Image>();

        _newGameButton = _mainCanvas.transform.Find("GamePanel/ButtonsPanel/PlayButton").GetComponent<Button>();
        _newGameImage = _mainCanvas.transform.Find("GamePanel/ButtonsPanel/PlayButton").GetComponent<Image>();

        _quitButton = _mainCanvas.transform.Find("GamePanel/ButtonsPanel/QuitButton").GetComponent<Button>();
        _quitImage = _mainCanvas.transform.Find("GamePanel/ButtonsPanel/QuitButton").GetComponent<Image>();

        _newGameTransform = _mainCanvas.transform.Find("NewGamePanel");

        _townNameField = _newGameTransform.Find("ItemsPanel/TownName").GetComponent<TMP_InputField>();
        _seedField = _newGameTransform.Find("ItemsPanel/Seed").GetComponent<TMP_InputField>();

        _botsToggle = _newGameTransform.Find("ItemsPanel/BotsPanel/BotsCheckbox").GetComponent<Toggle>();
        _fogToggle = _newGameTransform.Find("ItemsPanel/FogPanel/FogCheckbox").GetComponent<Toggle>();

        _backButton = _newGameTransform.Find("BackButton").GetComponent<Button>();
        _startGameButton = _newGameTransform.Find("StartGameButton").GetComponent<Button>();

        _backButton.onClick.AddListener(OnBackButtonClicked);
        _startGameButton.onClick.AddListener(OnStartGameButtonClicked);

        _backButton.interactable = false;
        _startGameButton.interactable = false;

        StartCoroutine(FadeInFromBlack());
    }

    private void StartNewGame()
    {
        _townNameField.text = UtilsClass.GetRandomCityName();
        _seedField.text = new System.Random().Next(100000, 1000000).ToString();
        _newGameTransform.gameObject.SetActive(true);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnBackButtonClicked()
    {
        _newGameTransform.gameObject.SetActive(false);
    }

    private void OnStartGameButtonClicked()
    {
        TownName = _townNameField.text;

        if (!int.TryParse(_seedField.text, out var parsedSeed) || parsedSeed < 1000000 || parsedSeed > 9999999)
        {
            Random.InitState(parsedSeed);
            parsedSeed = Random.Range(1000000, 10000000);
        }

        Seed = parsedSeed;
        EnableBots = _botsToggle.isOn;
        EnableFog = _fogToggle.isOn;
        SceneManager.LoadScene("SampleScene");
    }

    private IEnumerator FadeInFromBlack()
    {
        var startColor = Color.black;
        var endColor = new Color(0f, 0f, 0f, 0f);

        _fadeImage.color = startColor;

        var elapsedTime = 0f;

        while (elapsedTime < _fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            var progress = elapsedTime / _fadeInDuration;

            _fadeImage.color = Color.Lerp(startColor, endColor, progress);

            yield return null;
        }

        _fadeImage.color = endColor;
        _fadeTransform.gameObject.SetActive(false);

        _newGameButton.onClick.AddListener(StartNewGame);
        _quitButton.onClick.AddListener(QuitGame);

        _backButton.interactable = true;
        _startGameButton.interactable = true;
    }
}
