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
    /// Gets the number of active (spawned) balls of a given type ID.
    /// </summary>
    public int GetActiveBallCount(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        
        if (_pools.TryGetValue(id, out ObjectPool<BallEntity> pool))
        {
            return pool.CountActive;
        }
        return 0;
    }

    /// <summary>
    /// Spawns a ball from the appropriate pool based on the provided data.
    /// </summary>
    /// <param name="data">The configuration data of the ball to spawn.</param>
    /// <param name="position">The world position where the ball should spawn.</param>
    /// <returns>The spawned BallEntity instance.</returns>
    public BallEntity SpawnBall(BallDataSO data, Vector2 position)
    {
        if (data == null || data.prefab == null) return null;

        if (!_pools.TryGetValue(data.id, out ObjectPool<BallEntity> pool))
        {
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
        
        // Set positions while the object is still inactive to prevent physics engine overlaps
        ball.transform.position = position;
        if (ball.Rb != null)
        {
            ball.Rb.position = position;
        }
        
        ball.Initialize(data);
        ball.gameObject.SetActive(true);

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
        // SetActive(true) is handled inside SpawnBall after position assignment to prevent physics overlap issues
    }

    private void OnReturnedToPool(BallEntity ball)
    {
        ball.gameObject.SetActive(false);
        
        // Teleport to a far away position so it doesn't trigger overlaps at (0,0) before next spawn
        ball.transform.position = new Vector3(9999f, 9999f, 0f);
        if (ball.Rb != null)
        {
            ball.Rb.position = new Vector2(9999f, 9999f);
        }
    }

    private void OnDestroyPoolObject(BallEntity ball)
    {
        Destroy(ball.gameObject);
    }
}