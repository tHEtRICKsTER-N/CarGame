using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ArcadeRaceSetupMenu : MonoBehaviour
{
    private readonly RaceAIDifficulty[] difficulties =
    {
        RaceAIDifficulty.Easy,
        RaceAIDifficulty.Medium,
        RaceAIDifficulty.Hard,
        RaceAIDifficulty.EMPRESS
    };

    private ArcadeRaceManager raceManager;
    private Font font;
    private Text lapsValueText;
    private Text difficultyValueText;
    private Text carValueText;
    private Image carSwatchImage;
    private Button[] difficultyButtons;
    private int selectedLaps;
    private int selectedCarIndex;
    private RaceAIDifficulty selectedDifficulty;

    public void Initialize(ArcadeRaceManager manager)
    {
        raceManager = manager;
        selectedLaps = raceManager != null ? raceManager.LapsToWin : 2;
        selectedDifficulty = raceManager != null ? raceManager.AIDifficulty : RaceAIDifficulty.Medium;
        selectedCarIndex = raceManager != null ? raceManager.SelectedCarIndex : 0;

        EnsureEventSystem();
        BuildMenu();
        RefreshValues();
    }

    private void BuildMenu()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        Image shade = gameObject.AddComponent<Image>();
        shade.color = new Color(0.02f, 0.025f, 0.03f, 0.58f);

        GameObject panelObject = new GameObject("Race Setup Panel");
        panelObject.transform.SetParent(transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(720f, 700f);

        Image panel = panelObject.AddComponent<Image>();
        panel.color = new Color(0.04f, 0.045f, 0.055f, 0.94f);

        string trackName = raceManager != null ? raceManager.TrackDisplayName : "Race";
        CreateText("Title", panelObject.transform, trackName, 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(640f, 70f));
        CreateText("Subtitle", panelObject.transform, "Race setup", 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(640f, 44f));

        CreateText("Laps Label", panelObject.transform, "Laps", 30, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(-250f, -180f), new Vector2(180f, 52f));
        CreateButton("Laps Minus", panelObject.transform, "-", 34, new Vector2(0.5f, 1f), new Vector2(50f, -180f), new Vector2(68f, 52f), DecreaseLaps);
        lapsValueText = CreateText("Laps Value", panelObject.transform, selectedLaps.ToString(), 32, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(132f, -180f), new Vector2(92f, 52f));
        CreateButton("Laps Plus", panelObject.transform, "+", 34, new Vector2(0.5f, 1f), new Vector2(230f, -180f), new Vector2(68f, 52f), IncreaseLaps);

        CreateText("Difficulty Label", panelObject.transform, "Difficulty", 30, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(-250f, -270f), new Vector2(220f, 52f));
        difficultyValueText = CreateText("Difficulty Value", panelObject.transform, selectedDifficulty.ToString(), 28, TextAnchor.MiddleRight, new Vector2(0.5f, 1f), new Vector2(60f, -270f), new Vector2(260f, 52f));

        difficultyButtons = new Button[difficulties.Length];
        float startX = -249f;
        for (int i = 0; i < difficulties.Length; i++)
        {
            RaceAIDifficulty difficulty = difficulties[i];
            float width = difficulty == RaceAIDifficulty.EMPRESS ? 150f : 126f;
            Vector2 position = new Vector2(startX + i * 152f, -350f);
            difficultyButtons[i] = CreateButton("Difficulty " + difficulty, panelObject.transform, difficulty.ToString(), 23, new Vector2(0.5f, 1f), position, new Vector2(width, 56f), () => SelectDifficulty(difficulty));
        }

        CreateText("Car Label", panelObject.transform, "Car", 30, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(-250f, -455f), new Vector2(140f, 52f));
        CreateButton("Car Previous", panelObject.transform, "<", 34, new Vector2(0.5f, 1f), new Vector2(-150f, -455f), new Vector2(64f, 52f), PreviousCar);
        carValueText = CreateText("Car Value", panelObject.transform, "Car", 27, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -455f), new Vector2(220f, 52f));
        CreateButton("Car Next", panelObject.transform, ">", 34, new Vector2(0.5f, 1f), new Vector2(150f, -455f), new Vector2(64f, 52f), NextCar);

        GameObject swatchObject = new GameObject("Car Color Swatch");
        swatchObject.transform.SetParent(panelObject.transform, false);
        RectTransform swatchRect = swatchObject.AddComponent<RectTransform>();
        swatchRect.anchorMin = new Vector2(0.5f, 1f);
        swatchRect.anchorMax = new Vector2(0.5f, 1f);
        swatchRect.pivot = new Vector2(0.5f, 1f);
        swatchRect.anchoredPosition = new Vector2(222f, -459f);
        swatchRect.sizeDelta = new Vector2(34f, 44f);
        carSwatchImage = swatchObject.AddComponent<Image>();

        CreateButton("Start Race", panelObject.transform, "Start Race", 34, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(360f, 72f), StartRace);
    }

    private Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = anchor;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.72f);
        outline.effectDistance = new Vector2(2f, -2f);

        return text;
    }

    private Button CreateButton(string name, Transform parent, string label, int fontSize, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction clickAction)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = anchor;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.13f, 0.18f, 0.24f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(clickAction);

        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.23f, 0.32f, 0.42f, 1f);
        colors.pressedColor = new Color(0.08f, 0.12f, 0.18f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateText("Label", buttonObject.transform, label, fontSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        return button;
    }

    private void DecreaseLaps()
    {
        selectedLaps = Mathf.Max(1, selectedLaps - 1);
        RefreshValues();
    }

    private void IncreaseLaps()
    {
        selectedLaps = Mathf.Min(5, selectedLaps + 1);
        RefreshValues();
    }

    private void SelectDifficulty(RaceAIDifficulty difficulty)
    {
        selectedDifficulty = difficulty;
        RefreshValues();
    }

    private void PreviousCar()
    {
        int count = raceManager != null ? Mathf.Max(1, raceManager.CarOptionCount) : 1;
        selectedCarIndex = (selectedCarIndex - 1 + count) % count;
        if (raceManager != null)
        {
            raceManager.PreviewCarSelection(selectedCarIndex);
        }

        RefreshValues();
    }

    private void NextCar()
    {
        int count = raceManager != null ? Mathf.Max(1, raceManager.CarOptionCount) : 1;
        selectedCarIndex = (selectedCarIndex + 1) % count;
        if (raceManager != null)
        {
            raceManager.PreviewCarSelection(selectedCarIndex);
        }

        RefreshValues();
    }

    private void StartRace()
    {
        if (raceManager == null)
        {
            return;
        }

        raceManager.SetRaceSetup(selectedLaps, selectedDifficulty, selectedCarIndex);
        raceManager.BeginCountdown();
    }

    private void RefreshValues()
    {
        if (lapsValueText != null)
        {
            lapsValueText.text = selectedLaps.ToString();
        }

        if (difficultyValueText != null)
        {
            difficultyValueText.text = selectedDifficulty.ToString();
        }

        if (carValueText != null)
        {
            carValueText.text = raceManager != null ? raceManager.GetCarDisplayName(selectedCarIndex) : "Default Car";
        }

        if (carSwatchImage != null)
        {
            carSwatchImage.color = raceManager != null ? raceManager.GetCarUIColor(selectedCarIndex) : Color.white;
        }

        if (difficultyButtons == null)
        {
            return;
        }

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            if (difficultyButtons[i] == null)
            {
                continue;
            }

            bool selected = difficulties[i] == selectedDifficulty;
            Image image = difficultyButtons[i].GetComponent<Image>();
            Color buttonColor = selected ? new Color(0.95f, 0.62f, 0.12f, 1f) : new Color(0.13f, 0.18f, 0.24f, 0.95f);
            if (image != null)
            {
                image.color = buttonColor;
            }

            ColorBlock colors = difficultyButtons[i].colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = selected ? new Color(1f, 0.76f, 0.2f, 1f) : new Color(0.23f, 0.32f, 0.42f, 1f);
            colors.selectedColor = colors.highlightedColor;
            difficultyButtons[i].colors = colors;
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }
}
