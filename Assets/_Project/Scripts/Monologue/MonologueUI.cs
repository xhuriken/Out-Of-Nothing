using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;


using Febucci.UI;

public class MonologueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject monologuePanel;
    [SerializeField] private TextMeshProUGUI textMeshPro;

    [Header("Text Animator Reference (Optional)")]
    [SerializeField] private TypewriterByCharacter typewriter;

    [Header("Mysterious Styling")]
    [SerializeField] private TMP_FontAsset mysteriousFont;
    [SerializeField] private bool applyMysteriousTags = true;
    [SerializeField] [Range(0.01f, 3f)] private float typewriterSpeedMultiplier = 0.6f;

    private Coroutine _displayCoroutine;
    
#pragma warning disable 0414
    private bool _isTypingFinished = false;
#pragma warning restore 0414

    private struct QueueItem
    {
        public string text;
        public float exposureTime;
    }

    private Queue<QueueItem> _dialogueQueue = new Queue<QueueItem>();
    private bool _isDisplaying = false;

    private void OnDisable()
    {
        if (_dialogueQueue != null)
        {
            _dialogueQueue.Clear();
        }
        _isDisplaying = false;
        _displayCoroutine = null;
    }

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

        // Apply mysterious font if provided
        if (textMeshPro != null && mysteriousFont != null)
        {
            textMeshPro.font = mysteriousFont;
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
    /// Enqueues a monologue line to be displayed in sequence.
    /// </summary>
    public void ShowLine(string text, float exposureTime)
    {
        if (monologuePanel == null)
        {
            Debug.LogWarning("MonologueUI: Monologue Panel reference is missing!");
            return;
        }

        _dialogueQueue.Enqueue(new QueueItem { text = text, exposureTime = exposureTime });

        if (!_isDisplaying)
        {
            _displayCoroutine = StartCoroutine(Co_ProcessQueue());
        }
    }

    /// <summary>
    /// Processes the monologue queue sequentially.
    /// </summary>
    private IEnumerator Co_ProcessQueue()
    {
        _isDisplaying = true;

        while (_dialogueQueue.Count > 0)
        {
            QueueItem nextItem = _dialogueQueue.Dequeue();
            yield return StartCoroutine(Co_ShowLine(nextItem.text, nextItem.exposureTime));
        }

        _isDisplaying = false;
        _displayCoroutine = null;
    }

    private IEnumerator Co_ShowLine(string text, float exposureTime)
    {
        monologuePanel.SetActive(true);
        _isTypingFinished = false;

        string formattedText = text;
        if (applyMysteriousTags)
        {
            // Only wrap if it doesn't already contain formatting tags for movement/effects
            bool hasEffects = text.Contains("<wiggle") || text.Contains("<glitch") || text.Contains("<shake") || text.Contains("<wave") || text.Contains("<bounce") || text.Contains("<rain");
            if (!hasEffects)
            {
                formattedText = $"<wiggle a=0.08 b=12><i>{text}</i></wiggle>";
            }
        }

        if (typewriter != null)
        {
            // Use Text Animator typewriter
            typewriter.ShowText(formattedText);
            typewriter.SetTypewriterSpeed(typewriterSpeedMultiplier);

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
                textMeshPro.text = formattedText;
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
    }

    private void OnTypingFinished()
    {
        _isTypingFinished = true;
    }
}
