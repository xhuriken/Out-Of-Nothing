using UnityEngine;
using TMPro;
using System.Collections;


using Febucci.UI;

public class MonologueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject monologuePanel;
    [SerializeField] private TextMeshProUGUI textMeshPro;

    [Header("Text Animator Reference (Optional)")]

    [SerializeField] private TypewriterByCharacter typewriter;

    private Coroutine _displayCoroutine;
    
#pragma warning disable 0414
    private bool _isTypingFinished = false;
#pragma warning restore 0414

    private void Start()
    {
        // Hide panel at start
        if (monologuePanel != null)
        {
            monologuePanel.SetActive(false);
        }

        // Auto-detect components if not assigned
        if (textMeshPro == null)
        {
            textMeshPro = GetComponentInChildren<TextMeshProUGUI>();
        }


        if (typewriter == null)
        {
            typewriter = GetComponentInChildren<TypewriterByCharacter>();
        }

        if (typewriter != null)
        {
            typewriter.onTextShowed.AddListener(OnTypingFinished);
        }
        
    }

    private void OnDestroy()
    {

        if (typewriter != null)
        {
            typewriter.onTextShowed.RemoveListener(OnTypingFinished);
        }
        
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

        // Disappearance phase
        
        if (typewriter != null)
        {
            typewriter.StartDisappearingText();

            bool isDisappearingFinished = false;
            UnityEngine.Events.UnityAction onDisappearedAction = null;
            onDisappearedAction = () => { isDisappearingFinished = true; };
            
            typewriter.onTextDisappeared.AddListener(onDisappearedAction);

            while (!isDisappearingFinished)
            {
                yield return null;
            }

            typewriter.onTextDisappeared.RemoveListener(onDisappearedAction);
        }
        

        // Hide panel
        if (monologuePanel != null)
        {
            monologuePanel.SetActive(false);
        }
        _displayCoroutine = null;
    }

    private void OnTypingFinished()
    {
        _isTypingFinished = true;
    }
}
