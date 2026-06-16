using UnityEngine;
using System.Collections;

public class MonologueTrigger : MonoBehaviour
{
    [Header("Event Configuration")]
    [SerializeField] private MonologueEventSO monologueEvent;

    [Header("Start Trigger")]
    [SerializeField] private bool triggerOnStart = false;
    [SerializeField] private float startDelay = 2f;

    [Header("Collision Trigger")]
    [SerializeField] private bool triggerOnCollision = false;
    [SerializeField] private string playerTag = "Player";

    private void Start()
    {
        if (triggerOnStart)
        {
            StartCoroutine(Co_TriggerAfterDelay());
        }
    }

    private IEnumerator Co_TriggerAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        Trigger();
    }

    /// <summary>
    /// Triggers the referenced monologue event immediately.
    /// </summary>
    public void Trigger()
    {
        if (monologueEvent == null)
        {
            Debug.LogWarning($"MonologueTrigger on {gameObject.name}: No MonologueEventSO assigned!");
            return;
        }

        if (MonologueManager.Instance != null)
        {
            MonologueManager.Instance.TriggerMonologue(monologueEvent);
        }
        else
        {
            Debug.LogError("MonologueTrigger: MonologueManager instance is missing in the scene.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnCollision && other.CompareTag(playerTag))
        {
            Trigger();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnCollision && other.CompareTag(playerTag))
        {
            Trigger();
        }
    }
}
