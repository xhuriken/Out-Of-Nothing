using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections.Generic;

public class MenuController : MonoBehaviour
{
    [Header("Settings")]
    public RectTransform menuPanel;
    public List<RectTransform> buttons; // Tes objets "Shape"
    public List<CanvasGroup> buttonTexts;
    public float animationDuration = 0.5f;

    [Header("Visuals")]
    public float pointScale = 0.1f; // Taille de la boule quand elle est un "point"

    private bool isOpen = false;

    private void Start()
    {
        // Position initiale du menu
        menuPanel.anchoredPosition = new Vector2(-menuPanel.rect.width, 0);

        for (int i = 0; i < buttons.Count; i++)
        {
            // 1. On les tourne vers le bas
            buttons[i].localRotation = Quaternion.Euler(0, 0, -90f);
            // 2. On les réduit à l'état de "point"
            buttons[i].localScale = Vector3.one * pointScale;
            // 3. Texte invisible
            if (buttonTexts[i] != null) buttonTexts[i].alpha = 0f;
        }
    }

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (!isOpen) OpenMenu();
            else CloseMenu();
        }
    }

    void OpenMenu()
    {
        isOpen = true;
        menuPanel.DOKill();
        Sequence openSequence = DOTween.Sequence();

        // Glissement du menu
        openSequence.Append(menuPanel.DOAnchorPosX(0, animationDuration).SetEase(Ease.OutBack));

        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i;
            // On enchaîne : Rotation + Agrandissement (devient une boule) + Apparition texte
            openSequence.Append(buttons[index].DORotate(Vector3.zero, 0.4f).SetEase(Ease.OutCubic));
            openSequence.Join(buttons[index].DOScale(1f, 0.4f).SetEase(Ease.OutBack)); // Repasse à taille 1
            openSequence.Join(buttonTexts[index].DOFade(1f, 0.3f));
        }
    }

    void CloseMenu()
    {
        isOpen = false;
        menuPanel.DOKill();
        Sequence closeSequence = DOTween.Sequence();

        // On réduit les boules en points et on cache le texte
        for (int i = 0; i < buttons.Count; i++)
        {
            closeSequence.Join(buttonTexts[i].DOFade(0f, 0.2f));
            closeSequence.Join(buttons[i].DORotate(new Vector3(0, 0, -90f), 0.2f));
            closeSequence.Join(buttons[i].DOScale(pointScale, 0.2f)); // Redevient un point
        }

        closeSequence.Append(menuPanel.DOAnchorPosX(-menuPanel.rect.width, animationDuration).SetEase(Ease.InBack));
    }
}