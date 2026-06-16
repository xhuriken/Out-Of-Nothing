using UnityEngine;
using TMPro;
using System.Collections;

// UNCOMMENT this line once you have imported Text Animator in your project
// using Febucci.UI;

public class MonologueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject monologuePanel;
    [SerializeField] private TextMeshProUGUI textMeshPro;

    [Header("Text Animator Reference (Optional)")]
    // UNCOMMENT this field once you have imported Text Animator in your project
    // [SerializeField] private TypewriterByCharacter typewriter;

    private Coroutine _displayCoroutine;
    
#pragma warning disable 0414
    private bool _isTypingFinished = false;
#pragma warning restore 0414

    private void Start()
    {
        // Hide panel at start
        //if (monologuePanel != null)
        //{
        //    monologuePanel.SetActive(false);
        //}

        // Auto-detect components if not assigned
        if (textMeshPro == null)
        {
            textMeshPro = GetComponentInChildren<TextMeshProUGUI>();
        }

        // UNCOMMENT this block once you have imported Text Animator in your project
        /*
        if (typewriter == null)
        {
            typewriter = GetComponentInChildren<TypewriterByCharacter>();
        }

        if (typewriter != null)
        {
            typewriter.onTextShowed.AddListener(OnTypingFinished);
        }
        */
    }

    private void OnDestroy()
    {
        // UNCOMMENT this block once you have imported Text Animator in your project
        /*
        if (typewriter != null)
        {
            typewriter.onTextShowed.RemoveListener(OnTypingFinished);
        }
        */
    }

    /// <summary>
    /// Displays a monologue line.
    /// </summary>
    public void ShowLine(string text, float exposureTime)
    {
        if (monologuePanel == null)
        {
            Debug.LogWarning("MonologueUI: Monologue Panel reference is missing!");
            return;
        }

        // Stop any running monologue display
        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
        }

        _displayCoroutine = StartCoroutine(Co_ShowLine(text, exposureTime));
    }

    private IEnumerator Co_ShowLine(string text, float exposureTime)
    {
        monologuePanel.SetActive(true);
        _isTypingFinished = false;

        // UNCOMMENT this block once you have imported Text Animator in your project
        /*
        if (typewriter != null)
        {
            // Use Text Animator typewriter
            typewriter.ShowText(text);

            // Wait until the typewriter finishes typing
            while (!_isTypingFinished)
            {
                yield return null;
            }
        }
        else
        */
        {
            // Fallback: Show text immediately using standard TextMeshPro
            if (textMeshPro != null)
            {
                textMeshPro.text = text;
            }
            _isTypingFinished = true;
        }

        // Wait for the exposure time after typing is complete
        yield return new WaitForSeconds(exposureTime);

        // Hide panel
        monologuePanel.SetActive(false);
        _displayCoroutine = null;
    }

    private void OnTypingFinished()
    {
        _isTypingFinished = true;
    }
}
