using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections.Generic;

public class MenuController : MonoBehaviour
{
    [Header("Main Menu Settings")]
    public RectTransform menuPanel;
    public List<RectTransform> buttons;
    public List<CanvasGroup> buttonTexts;
    public float animationDuration = 0.5f;

    [Header("Main Menu Visuals")]
    public float pointScale = 0.1f;

    [Header("Settings Panel")]
    public RectTransform settingsPanel;
    public List<RectTransform> settingsElements;
    public float elementDelay = 0.08f;

    [Header("Journal Panel")]
    public RectTransform journalPanel;
    public List<RectTransform> journalElements;

    private bool isOpen = false;
    private bool isSettingsOpen = false;
    private bool isJournalOpen = false;

    // --- SÉCURITÉS ANTI-SPAM ---
    private bool isAnimatingMain = false;
    private bool isAnimatingSettings = false;
    private bool isAnimatingJournal = false;

    // Positions des panneaux
    private float mainMenuClosedX;
    private float mainMenuOpenX;
    private float settingsClosedX;
    private float settingsOpenX;
    private float journalClosedX;
    private float journalOpenX;

    // --- SAUVEGARDE DES POSITIONS D'ORIGINE DES ÉLÉMENTS ---
    private List<Vector2> originalSettingsPositions = new List<Vector2>();
    private List<Vector2> originalJournalPositions = new List<Vector2>();

    private void Start()
    {
        CalculatePositions();

        // Initialisation Main Menu
        menuPanel.anchoredPosition = new Vector2(mainMenuClosedX, 0);
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].localRotation = Quaternion.Euler(0, 0, -90f);
            buttons[i].localScale = Vector3.one * pointScale;
            if (buttonTexts[i] != null) buttonTexts[i].alpha = 0f;
        }

        // Initialisation & Sauvegarde Settings Panel
        settingsPanel.anchoredPosition = new Vector2(settingsClosedX, 0);
        foreach (var element in settingsElements)
        {
            originalSettingsPositions.Add(element.anchoredPosition); // On stocke la vraie position
            CanvasGroup cg = element.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;
        }

        // Initialisation & Sauvegarde Journal Panel
        journalPanel.anchoredPosition = new Vector2(journalClosedX, 0);
        foreach (var element in journalElements)
        {
            originalJournalPositions.Add(element.anchoredPosition); // On stocke la vraie position
            CanvasGroup cg = element.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;
        }
    }

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame && !isAnimatingMain && !isAnimatingSettings && !isAnimatingJournal)
        {
            if (!isOpen)
            {
                OpenMenu();
            }
            else
            {
                // Fermeture propre en chaîne si un sous-menu est ouvert
                if (isSettingsOpen)
                {
                    CloseSettings(() => CloseMenu());
                }
                else if (isJournalOpen)
                {
                    CloseJournal(() => CloseMenu());
                }
                else
                {
                    CloseMenu();
                }
            }
        }
    }

    private void CalculatePositions()
    {
        RectTransform canvasRect = menuPanel.parent as RectTransform;
        float canvasWidth = canvasRect != null ? canvasRect.rect.width : Screen.width;
        float halfCanvasWidth = canvasWidth / 2f;

        // --- MAIN MENU (Ancre à gauche, glisse vers la droite) ---
        mainMenuClosedX = 0f;
        mainMenuOpenX = halfCanvasWidth;

        // --- SETTINGS (Ancre à droite, glisse vers la gauche) ---
        settingsClosedX = 0f;
        settingsOpenX = -halfCanvasWidth; // S'arrête au milieu

        // --- JOURNAL (Ancre à droite, glisse AUSSI vers la gauche) ---
        journalClosedX = 0f; // Caché à droite de l'écran

        // Comme il est plus grand, il doit s'avancer plus loin vers la gauche.
        // On lui dit de glisser de la valeur de sa propre largeur !
        journalOpenX = -journalPanel.rect.width;
    }

    void OpenMenu()
    {
        isOpen = true;
        isAnimatingMain = true;
        menuPanel.DOKill();
        CalculatePositions();

        Sequence openSequence = DOTween.Sequence();
        openSequence.Append(menuPanel.DOAnchorPosX(mainMenuOpenX, animationDuration).SetEase(Ease.OutBack));

        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i;
            openSequence.Append(buttons[index].DORotate(Vector3.zero, 0.4f).SetEase(Ease.OutCubic));
            openSequence.Join(buttons[index].DOScale(1f, 0.4f).SetEase(Ease.OutBack));
            openSequence.Join(buttonTexts[index].DOFade(1f, 0.3f));
        }

        openSequence.OnComplete(() => isAnimatingMain = false);
    }

    void CloseMenu()
    {
        isOpen = false;
        isAnimatingMain = true;
        menuPanel.DOKill();
        Sequence closeSequence = DOTween.Sequence();

        for (int i = 0; i < buttons.Count; i++)
        {
            closeSequence.Join(buttonTexts[i].DOFade(0f, 0.2f));
            closeSequence.Join(buttons[i].DORotate(new Vector3(0, 0, -90f), 0.2f));
            closeSequence.Join(buttons[i].DOScale(pointScale, 0.2f));
        }

        closeSequence.Append(menuPanel.DOAnchorPosX(mainMenuClosedX, animationDuration).SetEase(Ease.InBack));
        closeSequence.OnComplete(() => isAnimatingMain = false);
    }

    // --- PANNEAU PARAMÈTRES (SETTINGS) ---

    public void ToggleSettings()
    {
        if (isAnimatingMain || isAnimatingSettings || isAnimatingJournal) return;

        if (isJournalOpen)
        {
            // Si le journal est ouvert, on le ferme d'abord, puis on ouvre les settings
            CloseJournal(() => OpenSettings());
        }
        else
        {
            if (!isSettingsOpen) OpenSettings();
            else CloseSettings(null);
        }
    }

    void OpenSettings()
    {
        isSettingsOpen = true;
        isAnimatingSettings = true;
        settingsPanel.DOKill();
        CalculatePositions();

        Sequence settingsSeq = DOTween.Sequence();
        settingsSeq.Append(settingsPanel.DOAnchorPosX(settingsOpenX, animationDuration).SetEase(Ease.OutBack));

        for (int i = 0; i < settingsElements.Count; i++)
        {
            RectTransform element = settingsElements[i];
            CanvasGroup cg = element.GetComponent<CanvasGroup>();

            if (cg != null)
            {
                // On repart de la position de départ décalée proprement
                element.anchoredPosition = originalSettingsPositions[i] + new Vector2(30f, -10f);
                cg.alpha = 0f;

                settingsSeq.Insert(animationDuration * 0.5f + (i * elementDelay),
                    element.DOAnchorPos(originalSettingsPositions[i], 0.4f).SetEase(Ease.OutCubic));

                settingsSeq.Insert(animationDuration * 0.5f + (i * elementDelay),
                    cg.DOFade(1f, 0.3f));
            }
        }

        settingsSeq.OnComplete(() => isAnimatingSettings = false);
    }

    void CloseSettings(System.Action onCompleteCallback)
    {
        isSettingsOpen = false;
        isAnimatingSettings = true;
        settingsPanel.DOKill();

        Sequence settingsSeq = DOTween.Sequence();

        for (int i = settingsElements.Count - 1; i >= 0; i--)
        {
            RectTransform element = settingsElements[i];
            CanvasGroup cg = element.GetComponent<CanvasGroup>();

            if (cg != null)
            {
                Vector2 targetPos = originalSettingsPositions[i] + new Vector2(30f, -10f);
                settingsSeq.Join(cg.DOFade(0f, 0.15f));
                settingsSeq.Join(element.DOAnchorPos(targetPos, 0.15f));
            }
        }

        settingsSeq.Append(settingsPanel.DOAnchorPosX(settingsClosedX, animationDuration).SetEase(Ease.InBack));
        settingsSeq.OnComplete(() => {
            isAnimatingSettings = false;
            onCompleteCallback?.Invoke();
        });
    }

    // --- PANNEAU JOURNAL (CORRIGÉ) ---

    public void ToggleJournal()
    {
        if (isAnimatingMain || isAnimatingSettings || isAnimatingJournal) return;

        if (isSettingsOpen)
        {
            // Si les paramètres sont ouverts, on les ferme d'abord, puis on ouvre le journal
            CloseSettings(() => OpenJournal());
        }
        else
        {
            if (!isJournalOpen) OpenJournal();
            else CloseJournal(null);
        }
    }

    void OpenJournal()
    {
        isJournalOpen = true;
        isAnimatingJournal = true;
        journalPanel.DOKill();
        CalculatePositions();

        Sequence journalSeq = DOTween.Sequence();
        // Glissement vers la GAUCHE (valeur négative)
        journalSeq.Append(journalPanel.DOAnchorPosX(journalOpenX, animationDuration).SetEase(Ease.OutBack));

        // Cascade des éléments internes
        for (int i = 0; i < journalElements.Count; i++)
        {
            RectTransform element = journalElements[i];
            CanvasGroup cg = element.GetComponent<CanvasGroup>();

            if (cg != null)
            {
                // Comme le panneau vient de la droite, on fait venir les éléments depuis la droite aussi (+50f)
                element.anchoredPosition = originalJournalPositions[i] + new Vector2(50f, -10f);
                element.localScale = Vector3.one * 0.8f;
                cg.alpha = 0f;

                journalSeq.Insert(animationDuration * 0.4f + (i * 0.1f),
                    element.DOAnchorPos(originalJournalPositions[i], 0.4f).SetEase(Ease.OutCubic));

                journalSeq.Insert(animationDuration * 0.4f + (i * 0.1f),
                    element.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));

                journalSeq.Insert(animationDuration * 0.4f + (i * 0.1f),
                    cg.DOFade(1f, 0.3f));
            }
        }

        journalSeq.OnComplete(() => isAnimatingJournal = false);
    }

    void CloseJournal(System.Action onCompleteCallback)
    {
        isJournalOpen = false;
        isAnimatingJournal = true;
        journalPanel.DOKill();

        Sequence journalSeq = DOTween.Sequence();

        // On range les éléments vers la droite
        for (int i = journalElements.Count - 1; i >= 0; i--)
        {
            RectTransform element = journalElements[i];
            CanvasGroup cg = element.GetComponent<CanvasGroup>();

            if (cg != null)
            {
                Vector2 targetPos = originalJournalPositions[i] + new Vector2(50f, -10f);
                journalSeq.Join(cg.DOFade(0f, 0.15f));
                journalSeq.Join(element.DOAnchorPos(targetPos, 0.15f));
                journalSeq.Join(element.DOScale(0.8f, 0.15f));
            }
        }

        // Le panneau repart vers la DROITE pour se cacher (retour à 0)
        journalSeq.Append(journalPanel.DOAnchorPosX(journalClosedX, animationDuration).SetEase(Ease.InBack));

        journalSeq.OnComplete(() => {
            isAnimatingJournal = false;
            onCompleteCallback?.Invoke();
        });
    }

    // --- BOUTONS ACTIONS ---

    public void PlayGame()
    {
        if (isAnimatingMain || isAnimatingSettings || isAnimatingJournal) return;

        if (isOpen)
        {
            if (isSettingsOpen) CloseSettings(() => CloseMenu());
            else if (isJournalOpen) CloseJournal(() => CloseMenu());
            else CloseMenu();
        }
    }

    public void QuitGame()
    {
        if (isAnimatingMain || isAnimatingSettings || isAnimatingJournal) return;

        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}