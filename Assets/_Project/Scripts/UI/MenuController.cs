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

    [Header("Journal Panel 1 (Du Bas)")]
    public RectTransform journalPanel;
    public List<RectTransform> journalElements;
    public float gapBetweenMenus = 40f;

    [Header("Journal Panel 2 (Du Haut)")]
    public RectTransform journalPanel2;
    public List<RectTransform> journal2Elements;

    [Header("Journal Layout")]
    [Tooltip("Hauteur d'arrivée du Journal 1 en pourcentage de l'écran (0.55 = 55% de la hauteur)")]
    public float journal1TargetHeightPercent = 0.55f;

    private bool isOpen = false;
    private bool isSettingsOpen = false;
    private bool isJournalOpen = false;

    // --- SÉCURITÉS ANTI-SPAM ---
    private bool isAnimatingMain = false;
    private bool isAnimatingSettings = false;
    private bool isAnimatingJournal = false;

    // Positions calculées pour les transitions X
    private float mainMenuClosedX;
    private float mainMenuOpenX;
    private float settingsClosedX;
    private float settingsOpenX;

    // Positions calculées pour le Journal (Vecteurs X,Y)
    private Vector2 journalClosedPosition;
    private Vector2 journalOpenPosition;
    private Vector2 journal2ClosedPosition;
    private Vector2 journal2OpenPosition;

    // Listes pour sauvegarder les positions d'origine locales de tes éléments d'UI
    private List<Vector2> originalSettingsPositions = new List<Vector2>();
    private List<Vector2> originalJournalPositions = new List<Vector2>();
    private List<Vector2> originalJournal2Positions = new List<Vector2>();

    private void Start()
    {
        CalculatePositions();

        // 1. Initialisation Main Menu
        menuPanel.anchoredPosition = new Vector2(mainMenuClosedX, 0);
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].localRotation = Quaternion.Euler(0, 0, -90f);
            buttons[i].localScale = Vector3.one * pointScale;
            if (buttonTexts[i] != null) buttonTexts[i].alpha = 0f;
        }

        // 2. Initialisation Settings
        settingsPanel.anchoredPosition = new Vector2(settingsClosedX, 0);
        foreach (var element in settingsElements)
        {
            originalSettingsPositions.Add(element.anchoredPosition);
            CanvasGroup cg = element.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;
        }

        // 3. Initialisation Journal 1 (Bas)
        if (journalPanel != null)
        {
            journalPanel.anchoredPosition = journalClosedPosition;
            foreach (var element in journalElements)
            {
                originalJournalPositions.Add(element.anchoredPosition);
                CanvasGroup cg = element.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0f;
            }
        }

        // 4. Initialisation Journal 2 (Haut)
        if (journalPanel2 != null)
        {
            journalPanel2.anchoredPosition = journal2ClosedPosition;
            foreach (var element in journal2Elements)
            {
                originalJournal2Positions.Add(element.anchoredPosition);
                CanvasGroup cg = element.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0f;
            }
        }
    }

    private void Update()
    {
        // Raccourci Clavier TAB avec vérification des verrous d'animation
        if (Keyboard.current.tabKey.wasPressedThisFrame && !isAnimatingMain && !isAnimatingSettings && !isAnimatingJournal)
        {
            if (!isOpen)
            {
                OpenMenu();
            }
            else
            {
                // Fermeture en chaîne logique si un panneau est ouvert
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
        float canvasHeight = canvasRect != null ? canvasRect.rect.height : Screen.height;
        float halfCanvasWidth = canvasWidth / 2f;

        // --- MAIN MENU ---
        mainMenuClosedX = 0f;
        mainMenuOpenX = halfCanvasWidth;

        // --- SETTINGS ---
        settingsClosedX = 0f;
        settingsOpenX = -halfCanvasWidth;

        // --- JOURNAL RESPONSIVE (Ton code d'origine) ---
        // 1. Calcul de la hauteur du Journal 1
        float targetY = canvasHeight * 0.02f;

        // 2. Calcul de la largeur/position horizontale :
        float menuPrincipalDroit = halfCanvasWidth + menuPanel.rect.width;
        float journalDemiLargeur = journalPanel.rect.width / 2f;

        // Position X idéale pour que le Journal se cale juste à droite du menu principal
        float targetX = menuPrincipalDroit + gapBetweenMenus + journalDemiLargeur;

        // Sécurité anti-débordement écran à droite :
        if (targetX + journalDemiLargeur > canvasWidth)
        {
            // S'il n'y a pas assez de place à l'écran (ex: petit écran), on force le journal à coller au bord droit de l'écran
            targetX = canvasWidth - journalDemiLargeur - 15f;
        }

        // Définition des vecteurs de position pour le JOURNAL 1 (Bas)
        journalClosedPosition = new Vector2(targetX, -journalPanel.rect.height - 100f); // Caché sous l'écran
        journalOpenPosition = new Vector2(targetX, targetY); // Position ouverte parfaite

        // --- AJOUT AUTOMATIQUE DU JOURNAL 2 (Haut) ---
        if (journalPanel2 != null)
        {
            // Il prend le même X pour être aligné au poil.
            // Sa hauteur d'arrivée (Y) = la hauteur du Journal 1 + la hauteur du Journal 2 pour qu'ils se collent.
            float targetY2 = targetY + journalPanel2.rect.height;

            // Position cachée (au-dessus de l'écran)
            journal2ClosedPosition = new Vector2(targetX, canvasHeight + 100f);
            // Position ouverte parfaite
            journal2OpenPosition = new Vector2(targetX, targetY2);
        }
    }

    // --- GESTION DU MENU PRINCIPAL ---

    // --- GESTION DU MENU PRINCIPAL SIMPLIFIÉE ET RAPIDE ---

    void OpenMenu()
    {
        isOpen = true;
        isAnimatingMain = true;
        menuPanel.DOKill();
        CalculatePositions();

        // On réduit la durée globale (ex: 0.25s au lieu de 0.5s)
        float fastDuration = 0.25f;

        Sequence openSequence = DOTween.Sequence();

        // Le panneau glisse instantanément
        openSequence.Append(menuPanel.DOAnchorPosX(mainMenuOpenX, fastDuration).SetEase(Ease.OutCubic));

        // On anime TOUS les boutons en même temps (Join) pour gagner du temps
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].DOKill();
            openSequence.Join(buttons[i].DORotate(Vector3.zero, fastDuration).SetEase(Ease.OutCubic));
            openSequence.Join(buttons[i].DOScale(1f, fastDuration).SetEase(Ease.OutCubic));
            if (buttonTexts[i] != null) openSequence.Join(buttonTexts[i].DOFade(1f, fastDuration));
        }

        openSequence.OnComplete(() => isAnimatingMain = false);
    }

    void CloseMenu()
    {
        isOpen = false;
        isAnimatingMain = true;
        menuPanel.DOKill();

        float fastDuration = 0.2f; // Encore plus rapide pour la fermeture
        Sequence closeSequence = DOTween.Sequence();

        // On cache tout d'un coup
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].DOKill();
            closeSequence.Join(buttonTexts[i].DOFade(0f, fastDuration));
            closeSequence.Join(buttons[i].DORotate(new Vector3(0, 0, -90f), fastDuration));
            closeSequence.Join(buttons[i].DOScale(pointScale, fastDuration));
        }

        // Le panneau se retire juste après
        closeSequence.Append(menuPanel.DOAnchorPosX(mainMenuClosedX, fastDuration).SetEase(Ease.InCubic));

        closeSequence.OnComplete(() => isAnimatingMain = false);
    }

    // --- GESTION DES PARAMÈTRES (SETTINGS) ---

    public void ToggleSettings()
    {
        if (isAnimatingMain || isAnimatingSettings || isAnimatingJournal) return;

        if (isJournalOpen)
        {
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

    // --- GESTION DU JOURNAL (MÂCHOIRE SYNCHRONISÉE) ---

    public void ToggleJournal()
    {
        if (isAnimatingMain || isAnimatingSettings || isAnimatingJournal) return;

        if (isSettingsOpen)
        {
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
        if (journalPanel2 != null) journalPanel2.DOKill();
        CalculatePositions();

        Sequence journalSeq = DOTween.Sequence();

        // Les deux panneaux s'ouvrent au même moment (Join) avec effet de ressort lourd (OutBack)
        journalSeq.Append(journalPanel.DOAnchorPos(journalOpenPosition, animationDuration).SetEase(Ease.OutBack));
        if (journalPanel2 != null)
        {
            journalSeq.Join(journalPanel2.DOAnchorPos(journal2OpenPosition, animationDuration).SetEase(Ease.OutBack));
        }

        // Cascade - Éléments du Journal 1 (Viennent du bas)
        for (int i = 0; i < journalElements.Count; i++)
        {
            RectTransform element = journalElements[i];
            CanvasGroup cg = element.GetComponent<CanvasGroup>();

            if (cg != null)
            {
                element.anchoredPosition = originalJournalPositions[i] + new Vector2(0f, -40f);
                element.localScale = Vector3.one * 0.8f;
                cg.alpha = 0f;

                journalSeq.Insert(animationDuration * 0.4f + (i * 0.1f), element.DOAnchorPos(originalJournalPositions[i], 0.4f).SetEase(Ease.OutCubic));
                journalSeq.Insert(animationDuration * 0.4f + (i * 0.1f), element.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
                journalSeq.Insert(animationDuration * 0.4f + (i * 0.1f), cg.DOFade(1f, 0.3f));
            }
        }

        // Cascade - Éléments du Journal 2 (Viennent du haut)
        for (int i = 0; i < journal2Elements.Count; i++)
        {
            RectTransform element = journal2Elements[i];
            CanvasGroup cg = element.GetComponent<CanvasGroup>();

            if (cg != null)
            {
                element.anchoredPosition = originalJournal2Positions[i] + new Vector2(0f, 40f);
                cg.alpha = 0f;

                journalSeq.Insert(animationDuration * 0.4f + (i * 0.1f), element.DOAnchorPos(originalJournal2Positions[i], 0.4f).SetEase(Ease.OutCubic));
                journalSeq.Insert(animationDuration * 0.4f + (i * 0.1f), cg.DOFade(1f, 0.3f));
            }
        }

        journalSeq.OnComplete(() => isAnimatingJournal = false);
    }

    void CloseJournal(System.Action onCompleteCallback)
    {
        isJournalOpen = false;
        isAnimatingJournal = true;

        journalPanel.DOKill();
        if (journalPanel2 != null) journalPanel2.DOKill();

        Sequence journalSeq = DOTween.Sequence();

        // Rangement des sous-éléments du bas
        for (int i = journalElements.Count - 1; i >= 0; i--)
        {
            Vector2 targetPos = originalJournalPositions[i] + new Vector2(0f, -40f);
            journalSeq.Join(journalElements[i].GetComponent<CanvasGroup>().DOFade(0f, 0.15f));
            journalSeq.Join(journalElements[i].DOAnchorPos(targetPos, 0.15f));
            journalSeq.Join(journalElements[i].DOScale(0.8f, 0.15f));
        }

        // Rangement des sous-éléments du haut
        for (int i = journal2Elements.Count - 1; i >= 0; i--)
        {
            Vector2 targetPos = originalJournal2Positions[i] + new Vector2(0f, 40f);
            journalSeq.Join(journal2Elements[i].GetComponent<CanvasGroup>().DOFade(0f, 0.15f));
            journalSeq.Join(journal2Elements[i].DOAnchorPos(targetPos, 0.15f));
        }

        // Fermeture physique simultanée des deux blocs de la mâchoire
        journalSeq.Append(journalPanel.DOAnchorPos(journalClosedPosition, animationDuration).SetEase(Ease.InBack));
        if (journalPanel2 != null)
        {
            journalSeq.Join(journalPanel2.DOAnchorPos(journal2ClosedPosition, animationDuration).SetEase(Ease.InBack));
        }

        journalSeq.OnComplete(() => {
            isAnimatingJournal = false;
            onCompleteCallback?.Invoke();
        });
    }

    // --- BOUTONS ACTIONS STANDARDS ---

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