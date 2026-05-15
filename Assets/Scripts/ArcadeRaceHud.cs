using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public sealed class ArcadeRaceHud : MonoBehaviour
{
    private ArcadeRaceManager raceManager;
    private Text statusText;
    private Text raceInfoText;
    private Text standingsText;
    private Text boostText;
    private Image boostFill;
    private GameObject finishPanel;
    private Text finishText;
    private readonly StringBuilder builder = new StringBuilder(256);

    public void Initialize(ArcadeRaceManager manager)
    {
        raceManager = manager;
        BuildHud();
    }

    private void Update()
    {
        if (raceManager == null || statusText == null)
        {
            return;
        }

        UpdateStatus();
        UpdateRaceInfo();
        UpdateStandings();
        UpdateBoostMeter();
        UpdateFinishPanel();
    }

    private void BuildHud()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        statusText = CreateText("Race Status", font, 48, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(650f, 90f));
        raceInfoText = CreateText("Race Info", font, 28, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(430f, 210f));
        standingsText = CreateText("Standings", font, 25, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(460f, 260f));
        CreateBoostMeter(font);
        CreateFinishPanel(font);
    }

    private Text CreateText(string name, Font font, int fontSize, TextAnchor alignment, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        return CreateText(name, transform, font, fontSize, alignment, anchor, anchoredPosition, size);
    }

    private Text CreateText(string name, Transform parent, Font font, int fontSize, TextAnchor alignment, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
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
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        outline.effectDistance = new Vector2(2f, -2f);

        return text;
    }

    private void CreateBoostMeter(Font font)
    {
        GameObject boostRoot = new GameObject("Boost Meter");
        boostRoot.transform.SetParent(transform, false);

        RectTransform rootRect = boostRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(0f, 0f);
        rootRect.pivot = new Vector2(0f, 0f);
        rootRect.anchoredPosition = new Vector2(30f, 34f);
        rootRect.sizeDelta = new Vector2(360f, 72f);

        boostText = CreateText("Boost Label", boostRoot.transform, font, 24, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, -18f), new Vector2(360f, 34f));

        GameObject backgroundObject = new GameObject("Boost Bar Background");
        backgroundObject.transform.SetParent(boostRoot.transform, false);

        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0f);
        backgroundRect.anchorMax = new Vector2(0f, 0f);
        backgroundRect.pivot = new Vector2(0f, 0f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = new Vector2(330f, 24f);

        Image background = backgroundObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject fillObject = new GameObject("Boost Bar Fill");
        fillObject.transform.SetParent(backgroundObject.transform, false);

        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(330f, 0f);

        boostFill = fillObject.AddComponent<Image>();
        boostFill.color = new Color(0.05f, 0.85f, 1f, 0.92f);
        boostFill.type = Image.Type.Filled;
        boostFill.fillMethod = Image.FillMethod.Horizontal;
        boostFill.fillOrigin = 0;
    }

    private void CreateFinishPanel(Font font)
    {
        finishPanel = new GameObject("Finish Result Panel");
        finishPanel.transform.SetParent(transform, false);

        RectTransform rectTransform = finishPanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(720f, 360f);

        Image image = finishPanel.AddComponent<Image>();
        image.color = new Color(0.02f, 0.025f, 0.03f, 0.82f);

        finishText = CreateText("Finish Result Text", finishPanel.transform, font, 32, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660f, 310f));
        finishPanel.SetActive(false);
    }

    private void UpdateStatus()
    {
        switch (raceManager.State)
        {
            case ArcadeRaceState.Countdown:
                statusText.text = Mathf.CeilToInt(raceManager.CountdownRemaining).ToString();
                break;
            case ArcadeRaceState.Racing:
                statusText.text = "GO!";
                break;
            case ArcadeRaceState.Finished:
                statusText.text = raceManager.PlayerParticipant != null && raceManager.PlayerParticipant.HasFinished ? "FINISH!" : "KEEP PUSHING!";
                break;
            default:
                statusText.text = "GET READY";
                break;
        }
    }

    private void UpdateRaceInfo()
    {
        ArcadeRaceParticipant player = raceManager.PlayerParticipant;
        if (player == null)
        {
            raceInfoText.text = string.Empty;
            return;
        }

        int lap = player.HasFinished ? raceManager.LapsToWin : Mathf.Clamp(player.CompletedLaps + 1, 1, raceManager.LapsToWin);
        int rank = raceManager.GetRank(player);

        builder.Length = 0;
        builder.Append("Lap ").Append(lap).Append(" / ").Append(raceManager.LapsToWin).AppendLine();
        builder.Append("Position ").Append(rank).Append(" / ").Append(raceManager.Participants.Count).AppendLine();
        builder.Append("Time ").Append(raceManager.RaceTime.ToString("0.0")).Append("s").AppendLine();
        if (player.CanCompleteLap)
        {
            builder.Append("Finish line ready");
        }
        else
        {
            builder.Append("Next gate ").Append(player.NextCheckpointIndex + 1).Append(" / ").Append(raceManager.Checkpoints.Count);
        }
        raceInfoText.text = builder.ToString();
    }

    private void UpdateStandings()
    {
        List<ArcadeRaceParticipant> standings = raceManager.GetStandings();

        builder.Length = 0;
        builder.AppendLine("Rivals");

        for (int i = 0; i < standings.Count; i++)
        {
            ArcadeRaceParticipant participant = standings[i];
            builder.Append(i + 1).Append(". ").Append(participant.displayName);

            if (participant.HasFinished)
            {
                builder.Append("  ").Append(participant.FinishTime.ToString("0.0")).Append("s");
            }

            builder.AppendLine();
        }

        standingsText.text = builder.ToString();
    }

    private void UpdateBoostMeter()
    {
        ArcadeBoostController boost = raceManager.PlayerBoost;
        if (boostText == null || boostFill == null || boost == null)
        {
            return;
        }

        int boostPercent = Mathf.RoundToInt(boost.NormalizedBoost * 100f);
        boostText.text = boost.IsBoosting ? "BOOST " + boostPercent + "%" : "Boost " + boostPercent + "%";
        boostFill.fillAmount = boost.NormalizedBoost;
        boostFill.color = boost.IsBoosting ? new Color(1f, 0.78f, 0.08f, 0.95f) : new Color(0.05f, 0.85f, 1f, 0.92f);
    }

    private void UpdateFinishPanel()
    {
        if (finishPanel == null || finishText == null)
        {
            return;
        }

        ArcadeRaceParticipant player = raceManager.PlayerParticipant;
        bool showResult = raceManager.State == ArcadeRaceState.Finished && player != null && player.HasFinished;
        finishPanel.SetActive(showResult);

        if (!showResult)
        {
            return;
        }

        int rank = raceManager.GetRank(player);
        builder.Length = 0;
        builder.AppendLine(rank == 1 ? "You won!" : "Race complete");
        builder.AppendLine();
        builder.Append("Position ").Append(rank).Append(" / ").Append(raceManager.Participants.Count).AppendLine();
        builder.Append("Time ").Append(player.FinishTime.ToString("0.0")).Append("s").AppendLine();
        builder.Append("Difficulty ").Append(raceManager.AIDifficulty).AppendLine();
        builder.Append("Laps ").Append(raceManager.LapsToWin);
        finishText.text = builder.ToString();
    }
}
