using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Centralized multi-pool manager for ball entities.
/// Manages separate pools for each ball type based on their unique ID.
/// </summary>
public class BallPoolManager : MonoBehaviour
{
    private readonly Dictionary<string, ObjectPool<BallEntity>> _pools = new Dictionary<string, ObjectPool<BallEntity>>();

    [SerializeField]
    private int _defaultCapacity = 10;

    [SerializeField]
    private int _maxSize = 10000;

    /// <summary>
    /// Gets the singleton instance of the pool manager.
    /// </summary>
    public static BallPoolManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Spawns a ball from the appropriate pool based on the provided data.
    /// </summary>
    /// <param name="data">The configuration data of the ball to spawn.</param>
    /// <param name="position">The world position where the ball should spawn.</param>
    /// <returns>The spawned BallEntity instance.</returns>
    public BallEntity SpawnBall(BallDataSO data, Vector2 position)
    {
        if (data == null || data.prefab == null)
        {
            Debug.LogError($"[BallPoolManager] Cannot spawn ball: Data or Prefab is null.");
            return null;
        }

        if (!_pools.TryGetValue(data.id, out ObjectPool<BallEntity> pool))
        {
            // Create a new pool for this specific ball type if it doesn't exist
            pool = new ObjectPool<BallEntity>(
                createFunc: () => Instantiate(data.prefab),
                actionOnGet: OnTakeFromPool,
                actionOnRelease: OnReturnedToPool,
                actionOnDestroy: OnDestroyPoolObject,
                collectionCheck: true,
                defaultCapacity: _defaultCapacity,
                maxSize: _maxSize);

            _pools.Add(data.id, pool);
        }

        BallEntity ball = pool.Get();
        ball.transform.position = position;
        ball.Initialize(data);

        return ball;
    }

    /// <summary>
    /// Returns a ball to its respective pool based on its data ID.
    /// </summary>
    /// <param name="ball">The ball instance to return.</param>
    public void ReleaseBall(BallEntity ball)
    {
        if (ball == null || ball.Data == null)
        {
            return;
        }

        if (_pools.TryGetValue(ball.Data.id, out ObjectPool<BallEntity> pool))
        {
            pool.Release(ball);
        }
        else
        {
            // Fallback if no pool exists for this ID
            Destroy(ball.gameObject);
        }
    }

    private void OnTakeFromPool(BallEntity ball)
    {
        ball.gameObject.SetActive(true);
    }

    private void OnReturnedToPool(BallEntity ball)
    {
        ball.gameObject.SetActive(false);
    }

    private void OnDestroyPoolObject(BallEntity ball)
    {
        Destroy(ball.gameObject);
    }
}