using UnityEngine;

public class MonologueManager : MonoBehaviour
{
    public static MonologueManager Instance { get; private set; }

    [Header("UI Reference")]
    [SerializeField] private MonologueUI monologueUI;

    private void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-find MonologueUI in the scene if not set
        if (monologueUI == null)
        {
            monologueUI = FindAnyObjectByType<MonologueUI>();
            if (monologueUI == null)
            {
                Debug.LogWarning("MonologueManager: MonologueUI component not found in the scene.");
            }
        }
    }

    /// <summary>
    /// Triggers a monologue event. Gets a line according to the configuration and sends it to the UI.
    /// </summary>
    /// <param name="monologueEvent">The scriptable object representing the monologue event.</param>
    public void TriggerMonologue(MonologueEventSO monologueEvent)
    {
        if (monologueEvent == null)
        {
            Debug.LogWarning("MonologueManager: Triggered monologue event is null.");
            return;
        }

        if (monologueUI == null)
        {
            // Try to find it one last time in case it was instantiated later
            monologueUI = FindAnyObjectByType<MonologueUI>();
            
            if (monologueUI == null)
            {
                Debug.LogError("MonologueManager: Cannot show monologue because MonologueUI is missing from the scene!");
                return;
            }
        }

        MonologueLine line = monologueEvent.GetLine();
        monologueUI.ShowLine(line.text, line.exposureTime);
    }
}
