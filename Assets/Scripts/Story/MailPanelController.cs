using UnityEngine;
using UnityEngine.UI;

public class MailPanelController : MonoBehaviour
{
    [SerializeField] private GameObject mailPanel;
    [SerializeField] private Button backButton;

    public Button BackButton => backButton;

    public void ShowPanel(StoryRoundData[] rounds, int currentRoundIndex)
    {
        HideAllContents(rounds);
        RefreshMailButtons(rounds, currentRoundIndex);
        SetActive(mailPanel, true);
    }

    public void ShowContent(StoryRoundData[] rounds, int roundIndex)
    {
        HideAllContents(rounds);

        if (!IsValidRound(rounds, roundIndex))
        {
            return;
        }

        SetActive(rounds[roundIndex].mail?.contentImage, true);
    }

    public void HidePanel(StoryRoundData[] rounds)
    {
        SetActive(mailPanel, false);
        HideAllContents(rounds);
    }

    public void RefreshMailButtons(StoryRoundData[] rounds, int currentRoundIndex)
    {
        if (rounds == null)
        {
            return;
        }

        for (int roundIndex = 0; roundIndex < rounds.Length; roundIndex++)
        {
            MailData mail = rounds[roundIndex]?.mail;
            SetActive(mail?.button?.gameObject, roundIndex <= currentRoundIndex);
        }
    }

    private void HideAllContents(StoryRoundData[] rounds)
    {
        if (rounds == null)
        {
            return;
        }

        foreach (StoryRoundData round in rounds)
        {
            SetActive(round?.mail?.contentImage, false);
        }
    }

    private static bool IsValidRound(StoryRoundData[] rounds, int roundIndex)
    {
        return rounds != null
            && roundIndex >= 0
            && roundIndex < rounds.Length
            && rounds[roundIndex] != null;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
