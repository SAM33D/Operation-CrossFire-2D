using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD, player control panels, the Quantum Flux banner and the start/end screens.
/// </summary>
public class UIManager : MonoBehaviour
{
    [System.Serializable]
    private class PlayerPanel
    {
        public Image frame;
        public TMP_Text roleLabel;
        public GameObject pilotControls;
        public GameObject gunnerControls;
        public Image boostCooldownFill;
        public Image shieldCooldownFill;
    }

    [Header("HUD")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private Image[] hullIcons;

    [Header("Player Panels")]
    [SerializeField] private PlayerPanel player1Panel;
    [SerializeField] private PlayerPanel player2Panel;
    [SerializeField] private Color pilotColor = new Color(0f, 1f, 1f);
    [SerializeField] private Color gunnerColor = new Color(1f, 0.25f, 0.25f);

    [Header("Quantum Flux Banner")]
    [SerializeField] private GameObject bannerRoot;
    [SerializeField] private TMP_Text bannerText;
    [SerializeField] private float bannerDuration = 1.5f;

    [Header("Screens")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TMP_Text endResultText;

    private int shownSeconds = -1;
    private int shownCountdown = -1;
    private float bannerTimer;

    public void Tick()
    {
        RefreshTimer();
        UpdateBanner();
    }

    #region HUD
    public void SetScore(int score)
    {
        scoreText.SetText("SCORE {0}", score);
    }

    public void SetPhase(string phaseName)
    {
        phaseText.SetText(phaseName);
    }

    public void SetHull(int hull)
    {
        for (int i = 0; i < hullIcons.Length; i++)
        {
            hullIcons[i].enabled = i < hull;
        }
    }

    private void RefreshTimer()
    {
        int seconds = Mathf.CeilToInt(GameManager.Instance.RemainingTime);
        if (seconds == shownSeconds) return;

        shownSeconds = seconds;
        timerText.SetText("{0}", seconds);
    }
    #endregion

    #region Role Panels
    public void RefreshRoles(int pilotPlayer)
    {
        bool player1IsPilot = pilotPlayer == 1;

        ApplyRole(player1Panel, player1IsPilot);
        ApplyRole(player2Panel, !player1IsPilot);

        player1Panel.roleLabel.SetText(player1IsPilot ? "PLAYER 1 — PILOT" : "PLAYER 1 — GUNNER");
        player2Panel.roleLabel.SetText(player1IsPilot ? "PLAYER 2 — GUNNER" : "PLAYER 2 — PILOT");
    }

    // Both panels show both fills; only the visible layout's fill is seen
    public void SetCooldowns(float boostReady01, float shieldReady01)
    {
        player1Panel.boostCooldownFill.fillAmount = boostReady01;
        player2Panel.boostCooldownFill.fillAmount = boostReady01;
        player1Panel.shieldCooldownFill.fillAmount = shieldReady01;
        player2Panel.shieldCooldownFill.fillAmount = shieldReady01;
    }

    private void ApplyRole(PlayerPanel panel, bool isPilot)
    {
        panel.pilotControls.SetActive(isPilot);
        panel.gunnerControls.SetActive(!isPilot);

        Color roleColor = isPilot ? pilotColor : gunnerColor;
        panel.frame.color = roleColor;
        panel.roleLabel.color = roleColor;
    }
    #endregion

    #region Quantum Flux Banner
    public void ShowFluxCountdown(int secondsLeft)
    {
        if (secondsLeft == shownCountdown) return;

        shownCountdown = secondsLeft;
        bannerText.SetText("QUANTUM FLUX IN {0}...", secondsLeft);
        bannerTimer = 0f; // stays up until the flux itself
        bannerRoot.SetActive(true);
    }

    public void ShowFluxBanner()
    {
        shownCountdown = -1;
        bannerText.SetText("QUANTUM FLUX — ROLES REVERSED");
        bannerTimer = bannerDuration;
        bannerRoot.SetActive(true);
    }

    private void UpdateBanner()
    {
        if (bannerTimer <= 0f) return;

        bannerTimer -= Time.deltaTime;
        if (bannerTimer <= 0f) bannerRoot.SetActive(false);
    }
    #endregion

    #region Start / End Screens
    public void ShowStartScreen()
    {
        startPanel.SetActive(true);
        endPanel.SetActive(false);
        bannerRoot.SetActive(false);
    }

    public void HideStartScreen()
    {
        startPanel.SetActive(false);
    }

    public void ShowEndScreen(bool won, string reason)
    {
        bannerRoot.SetActive(false);
        endResultText.text = (won ? "MISSION COMPLETE\n" : "MISSION FAILED\n") + reason;
        endPanel.SetActive(true);
    }
    #endregion
}
