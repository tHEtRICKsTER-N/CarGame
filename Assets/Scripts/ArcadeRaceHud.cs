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
    }

    private Text CreateText(string name, Font font, int fontSize, TextAnchor alignment, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(transform, false);

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

        int lap = Mathf.Clamp(player.CompletedLaps + 1, 1, raceManager.LapsToWin);
        int rank = raceManager.GetRank(player);

        builder.Length = 0;
        builder.Append("Lap ").Append(lap).Append(" / ").Append(raceManager.LapsToWin).AppendLine();
        builder.Append("Position ").Append(rank).Append(" / ").Append(raceManager.Participants.Count).AppendLine();
        builder.Append("Time ").Append(raceManager.RaceTime.ToString("0.0")).Append("s").AppendLine();
        builder.Append("Next gate ").Append(player.NextCheckpointIndex + 1).Append(" / ").Append(raceManager.Checkpoints.Count);
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
}
