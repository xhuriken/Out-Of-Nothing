using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DG.Tweening;

[DefaultExecutionOrder(-200)]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Autosave Settings")]
    [SerializeField] private float _autosaveInterval = 60f;
    [SerializeField] private bool _enableAutosave = true;

    [Header("Assets Databases")]
    [SerializeField] private List<BallDataSO> _ballDataList = new List<BallDataSO>();
    [SerializeField] private List<GameObject> _machinePrefabs = new List<GameObject>();
    [SerializeField] private GameObject _blackHolePrefab;
    [SerializeField] private GameObject _shopPrefab;
    [SerializeField] private GameObject _firstBallPrefab;

    [Header("UI Indicators")]
    [SerializeField] private GameObject _saveIndicatorPrefab;
    [SerializeField] private float _indicatorDuration = 2.5f;

    private float _autosaveTimer;
    private string _savePath;
    
    private GameObject _activeIndicator;
    private CanvasGroup _indicatorCanvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
    }

    private void Start()
    {
        if (HasSaveFile())
        {
            LoadGame();
        }
    }

    private void Update()
    {
        if (_enableAutosave && Application.isPlaying)
        {
            _autosaveTimer += Time.deltaTime;
            if (_autosaveTimer >= _autosaveInterval)
            {
                _autosaveTimer = 0f;
                SaveGame(true);
            }
        }
    }

    public bool HasSaveFile()
    {
        return File.Exists(_savePath);
    }

    public void DeleteSaveFile()
    {
        if (File.Exists(_savePath))
        {
            File.Delete(_savePath);
        }
    }

    /// <summary>
    /// Deletes the save file and instantly reloads the active scene to reset the game state.
    /// </summary>
    public void DeleteSaveAndReset()
    {
        DeleteSaveFile();
        
        string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        UnityEngine.SceneManagement.SceneManager.LoadScene(activeSceneName);
    }

    public void SaveGame(bool isAutoSave = false)
    {
        try
        {
            SaveData data = new SaveData();

            // 1. Core values
            if (IncrementManager.Instance != null)
            {
                data.points = IncrementManager.Instance.Points;
            }

            if (GameZone.Instance != null)
            {
                data.zoneWidth = GameZone.Instance.Width;
                data.zoneHeight = GameZone.Instance.Height;
                data.zoneThickness = GameZone.Instance.Thickness;
            }

            // 2. Monologues
            if (MonologueManager.Instance != null)
            {
                data.hasTriggered20PointsEvent = MonologueManager.Instance.HasTriggered20PointsEvent;
                data.triggeredMonologueEvents = MonologueManager.Instance.GetTriggeredEventNames();
            }

            // 3. Black Hole
            BlackHole blackHole = FindAnyObjectByType<BlackHole>();
            if (blackHole != null)
            {
                data.blackHoleExists = true;
                data.blackHolePosition = blackHole.transform.position;
                data.blackHoleRadius = blackHole.GRadius;
            }
            else
            {
                data.blackHoleExists = false;
            }

            // 4. Shop
            Shop shop = FindAnyObjectByType<Shop>();
            if (shop != null)
            {
                data.shopExists = true;
                data.shopPosition = shop.transform.position;
                data.shopRadius = shop.GRadius;
            }
            else
            {
                data.shopExists = false;
            }

            // 5. Balls
            BallEntity[] activeBalls = FindObjectsByType<BallEntity>(FindObjectsSortMode.None);
            List<BallEntity> savedBallEntities = new List<BallEntity>();

            int ballIdCounter = 0;
            Dictionary<BallEntity, int> ballToIdMap = new Dictionary<BallEntity, int>();

            foreach (BallEntity ball in activeBalls)
            {
                if (ball != null && ball.gameObject.activeInHierarchy && ball.Data != null)
                {
                    BallSaveData bData = new BallSaveData();
                    bData.id = ballIdCounter;
                    bData.ballDataId = ball.Data.id;
                    bData.position = ball.transform.position;
                    
                    if (ball.Passport != null)
                    {
                        bData.velocity = ball.Passport.TrueVelocity;
                    }
                    else if (ball.Rb != null)
                    {
                        bData.velocity = ball.Rb.linearVelocity;
                    }
                    
                    bData.currentClickCount = ball.CurrentClickCount;
                    bData.isProcessing = ball.IsProcessing;

                    // Support battery / YellowBall energy
                    if (ball.Behavior is YellowBallBehavior yellowBehavior)
                    {
                        bData.currentEnergy = yellowBehavior.CurrentEnergy;
                    }
                    else
                    {
                        bData.currentEnergy = 0f;
                    }

                    data.balls.Add(bData);
                    ballToIdMap.Add(ball, bData.id);
                    savedBallEntities.Add(ball);
                    ballIdCounter++;
                }
            }

            // 6. Machines
            MachineEntity[] activeMachines = FindObjectsByType<MachineEntity>(FindObjectsSortMode.None);
            foreach (MachineEntity machine in activeMachines)
            {
                if (machine != null)
                {
                    MachineSaveData mData = new MachineSaveData();
                    mData.prefabName = machine.gameObject.name.Replace("(Clone)", "").Trim();
                    mData.position = machine.transform.position;
                    mData.rotationZ = machine.transform.eulerAngles.z;
                    mData.currentEnergy = machine.CurrentEnergy;

                    // Link captured ball if any
                    BallCaptureHandler captureHandler = machine.GetComponent<BallCaptureHandler>();
                    if (captureHandler != null && captureHandler.CapturedBall != null)
                    {
                        if (ballToIdMap.TryGetValue(captureHandler.CapturedBall, out int bId))
                        {
                            mData.capturedBallId = bId;
                        }
                        else
                        {
                            mData.capturedBallId = -1;
                        }
                        mData.entryDirection = captureHandler.EntryDirection;
                    }
                    else
                    {
                        mData.capturedBallId = -1;
                        mData.entryDirection = Vector2.right;
                    }

                    data.machines.Add(mData);
                }
            }

            // Serialize and Save to File
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(_savePath, json);

            if (isAutoSave)
            {
                ShowSaveIndicator();
            }
            Debug.Log($"[SaveManager] Game Saved Successfully to {_savePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to Save Game: {e.Message}\n{e.StackTrace}");
        }
    }

    public void LoadGame()
    {
        if (!HasSaveFile()) return;

        try
        {
            string json = File.ReadAllText(_savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // 1. Clear existing dynamic scene elements
            // Clear Balls
            BallEntity[] existingBalls = FindObjectsByType<BallEntity>(FindObjectsSortMode.None);
            foreach (BallEntity ball in existingBalls)
            {
                if (ball != null)
                {
                    if (BallPoolManager.Instance != null && ball.Data != null)
                    {
                        BallPoolManager.Instance.ReleaseBall(ball);
                    }
                    else
                    {
                        Destroy(ball.gameObject);
                    }
                }
            }

            // Clear Machines
            MachineEntity[] existingMachines = FindObjectsByType<MachineEntity>(FindObjectsSortMode.None);
            foreach (MachineEntity machine in existingMachines)
            {
                if (machine != null)
                {
                    Destroy(machine.gameObject);
                }
            }

            // 2. Restore core points
            if (IncrementManager.Instance != null)
            {
                IncrementManager.Instance.SetPoints(data.points);
            }

            // Restore GameZone size
            if (GameZone.Instance != null && data.zoneWidth > 0f && data.zoneHeight > 0f)
            {
                GameZone.Instance.SetSize(data.zoneWidth, data.zoneHeight, data.zoneThickness);
            }

            // Restore Monologues
            if (MonologueManager.Instance != null)
            {
                MonologueManager.Instance.RestoreState(data.triggeredMonologueEvents, data.hasTriggered20PointsEvent);
            }

            // 3. Restore Black Hole
            BlackHole existingBlackHole = FindAnyObjectByType<BlackHole>();
            if (data.blackHoleExists)
            {
                if (existingBlackHole == null && _blackHolePrefab != null)
                {
                    GameObject bhObj = Instantiate(_blackHolePrefab, data.blackHolePosition, Quaternion.identity);
                    existingBlackHole = bhObj.GetComponent<BlackHole>();
                }
                
                if (existingBlackHole != null)
                {
                    existingBlackHole.transform.position = data.blackHolePosition;
                    existingBlackHole.GRadius = data.blackHoleRadius;
                }
            }
            else
            {
                if (existingBlackHole != null)
                {
                    Destroy(existingBlackHole.gameObject);
                }
            }

            // 4. Restore Shop
            Shop existingShop = FindAnyObjectByType<Shop>();
            if (data.shopExists)
            {
                if (existingShop == null && _shopPrefab != null)
                {
                    GameObject shopObj = Instantiate(_shopPrefab, data.shopPosition, Quaternion.identity);
                    existingShop = shopObj.GetComponentInChildren<Shop>();
                }

                if (existingShop != null)
                {
                    existingShop.transform.position = data.shopPosition;
                    existingShop.GRadius = data.shopRadius;
                }
            }
            else
            {
                if (existingShop != null)
                {
                    // Destroy parent if shop is placed in a parent wrapper
                    GameObject targetToDestroy = existingShop.transform.parent != null ? existingShop.transform.parent.gameObject : existingShop.gameObject;
                    Destroy(targetToDestroy);
                }
            }

            // 5. Restore Balls
            Dictionary<int, BallEntity> idToBallMap = new Dictionary<int, BallEntity>();
            foreach (BallSaveData bData in data.balls)
            {
                BallDataSO bSO = _ballDataList.Find(so => so != null && so.id == bData.ballDataId);
                if (bSO != null)
                {
                    BallEntity spawnedBall = null;
                    if (bData.ballDataId == "FirstBall" && _firstBallPrefab != null)
                    {
                        GameObject ballObj = Instantiate(_firstBallPrefab, bData.position, Quaternion.identity);
                        spawnedBall = ballObj.GetComponent<BallEntity>();
                        if (spawnedBall != null)
                        {
                            spawnedBall.Initialize(bSO);
                        }
                    }
                    else if (BallPoolManager.Instance != null)
                    {
                        spawnedBall = BallPoolManager.Instance.SpawnBall(bSO, bData.position);
                    }

                    if (spawnedBall != null)
                    {
                        spawnedBall.CurrentClickCount = bData.currentClickCount;
                        spawnedBall.IsProcessing = bData.isProcessing;

                        if (spawnedBall.Rb != null)
                        {
                            spawnedBall.Rb.linearVelocity = bData.velocity;
                        }
                        if (spawnedBall.Passport != null)
                        {
                            spawnedBall.Passport.RequestVelocity(bData.velocity, PhysicsPriority.Default, VelocityMode.Override);
                        }

                        // Restore Battery Energy
                        if (spawnedBall.Behavior is YellowBallBehavior yellowBehavior)
                        {
                            yellowBehavior.CurrentEnergy = bData.currentEnergy;
                        }

                        idToBallMap.Add(bData.id, spawnedBall);
                    }
                }
            }

            // 6. Restore Machines
            foreach (MachineSaveData mData in data.machines)
            {
                GameObject machinePrefab = _machinePrefabs.Find(p => p != null && p.name == mData.prefabName);
                if (machinePrefab != null)
                {
                    GameObject machineObj = Instantiate(machinePrefab, mData.position, Quaternion.Euler(0f, 0f, mData.rotationZ));
                    MachineEntity machine = machineObj.GetComponent<MachineEntity>();
                    
                    if (machine != null)
                    {
                        machine.CurrentEnergy = mData.currentEnergy;

                        // Restore captured ball if any
                        if (mData.capturedBallId != -1 && idToBallMap.TryGetValue(mData.capturedBallId, out BallEntity capturedBall))
                        {
                            BallCaptureHandler captureHandler = machine.GetComponent<BallCaptureHandler>();
                            if (captureHandler != null)
                            {
                                Vector3 capturePos = machine.CapturePosition;
                                captureHandler.RestoreCapturedBall(capturedBall, mData.entryDirection, capturePos);
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[SaveManager] Machine Prefab not found for: {mData.prefabName}");
                }
            }

            // 7. Mark Topology Dirty to force Energy Networks rebuild
            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.MarkTopologyDirty();
            }

            Debug.Log($"[SaveManager] Game Loaded Successfully from {_savePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to Load Game: {e.Message}\n{e.StackTrace}");
        }
    }

    private void CreateIndicatorUI()
    {
        if (_activeIndicator != null) return;

        Canvas mainCanvas = FindAnyObjectByType<Canvas>();
        if (mainCanvas == null) return;

        // Panel Container
        GameObject panelObj = new GameObject("SaveIndicatorPanel");
        panelObj.transform.SetParent(mainCanvas.transform, false);

        RectTransform rect = panelObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-40f, 40f);
        rect.sizeDelta = new Vector2(130f, 32f);

        // Styling: Glassmorphism / Sleek dark look
        UnityEngine.UI.Image bgImage = panelObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        // Add sleek border using Outline if possible, or just solid background
        var outline = panelObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.15f);
        outline.effectDistance = new Vector2(1f, -1f);

        _indicatorCanvasGroup = panelObj.AddComponent<CanvasGroup>();
        _indicatorCanvasGroup.alpha = 0f;

        // Text
        GameObject textObj = new GameObject("SaveText");
        textObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TMPro.TextMeshProUGUI textComp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        textComp.text = "<b>AUTOSAVE...</b>";
        textComp.fontSize = 11f;
        textComp.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        textComp.alignment = TMPro.TextAlignmentOptions.Center;
        textComp.fontStyle = TMPro.FontStyles.Normal;

        _activeIndicator = panelObj;
    }

    private void ShowSaveIndicator()
    {
        if (_saveIndicatorPrefab != null)
        {
            RectTransform rectTransform = _saveIndicatorPrefab.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Canvas mainCanvas = FindAnyObjectByType<Canvas>();
                if (mainCanvas == null) return;

                GameObject indicatorObj = Instantiate(_saveIndicatorPrefab, mainCanvas.transform, false);
                
                RectTransform rect = indicatorObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-40f, 40f);

                CanvasGroup cg = indicatorObj.GetComponent<CanvasGroup>();
                if (cg == null) cg = indicatorObj.AddComponent<CanvasGroup>();

                cg.alpha = 0f;
                Vector3 originalScale = indicatorObj.transform.localScale;
                indicatorObj.transform.localScale = originalScale * 0.8f;

                Sequence seq = DOTween.Sequence();
                seq.SetUpdate(true);
                seq.Append(cg.DOFade(1f, 0.35f));
                seq.Join(indicatorObj.transform.DOScale(originalScale * 1.15f, 0.4f).SetEase(Ease.OutElastic));
                seq.Append(indicatorObj.transform.DOScale(originalScale, 0.15f).SetEase(Ease.InOutSine));
                seq.Append(indicatorObj.transform.DOScale(originalScale * 1.05f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine));

                StartCoroutine(DestroyUIIndicatorAfterDelay(indicatorObj, cg, originalScale, _indicatorDuration, seq));
            }
            else
            {
                GameObject indicatorObj = Instantiate(_saveIndicatorPrefab);
                WorldSpaceIndicator worldInd = indicatorObj.AddComponent<WorldSpaceIndicator>();
                worldInd.Initialize(indicatorObj.transform.localScale, _indicatorDuration);
            }
        }
        else
        {
            CreateIndicatorUI();
            if (_activeIndicator == null) return;

            _activeIndicator.SetActive(true);
            _indicatorCanvasGroup.DOKill();
            _activeIndicator.transform.DOKill();

            _indicatorCanvasGroup.alpha = 0f;
            _activeIndicator.transform.localScale = Vector3.one * 0.75f;

            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true); // Run during time pause too
            seq.Append(_indicatorCanvasGroup.DOFade(1f, 0.35f));
            seq.Join(_activeIndicator.transform.DOScale(1.0f, 0.35f).SetEase(Ease.OutBack));
            seq.Append(_activeIndicator.transform.DOScale(1.05f, 0.6f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
            seq.Append(_indicatorCanvasGroup.DOFade(0f, 0.45f));
            seq.Join(_activeIndicator.transform.DOScale(0.75f, 0.45f).SetEase(Ease.InBack));
            seq.OnComplete(() => _activeIndicator.SetActive(false));
        }
    }

    private IEnumerator DestroyUIIndicatorAfterDelay(GameObject obj, CanvasGroup cg, Vector3 originalScale, float duration, Sequence activeSequence)
    {
        yield return new WaitForSecondsRealtime(duration - 0.4f);
        if (obj != null)
        {
            activeSequence.Kill();
            Sequence exitSeq = DOTween.Sequence();
            exitSeq.SetUpdate(true);
            exitSeq.Append(cg.DOFade(0f, 0.35f));
            exitSeq.Join(obj.transform.DOScale(originalScale * 0.8f, 0.35f).SetEase(Ease.InBack));
            yield return new WaitForSecondsRealtime(0.4f);
            if (obj != null) Destroy(obj);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Automatically populate BallDataSO database
        if (_ballDataList == null || _ballDataList.Count == 0)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:BallDataSO");
            _ballDataList = new List<BallDataSO>();
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                BallDataSO asset = UnityEditor.AssetDatabase.LoadAssetAtPath<BallDataSO>(path);
                if (asset != null) _ballDataList.Add(asset);
            }
        }

        // Automatically populate Machine Prefabs database
        if (_machinePrefabs == null || _machinePrefabs.Count == 0)
        {
            _machinePrefabs = new List<GameObject>();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Machines" });
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    // Exclude specific unique prefabs like BlackHole and Shop if necessary,
                    // but keeping them in database is harmless and useful for standard instantiation.
                    if (prefab.name != "BlackHole" && prefab.name != "Shop")
                    {
                        _machinePrefabs.Add(prefab);
                    }
                }
            }
        }

        // Automatically populate unique prefabs
        if (_blackHolePrefab == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Machines" });
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == "BlackHole")
                {
                    _blackHolePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    break;
                }
            }
        }

        if (_shopPrefab == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Machines" });
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == "Shop")
                {
                    _shopPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    break;
                }
            }
        }

        if (_firstBallPrefab == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Balls" });
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == "FirstBall")
                {
                    _firstBallPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    break;
                }
            }
        }
    }
#endif
}

[System.Serializable]
public class SaveData
{
    public double points;
    public float zoneWidth;
    public float zoneHeight;
    public float zoneThickness;

    public bool blackHoleExists;
    public Vector2 blackHolePosition;
    public float blackHoleRadius;

    public bool shopExists;
    public Vector2 shopPosition;
    public float shopRadius;

    public bool hasTriggered20PointsEvent;
    public List<string> triggeredMonologueEvents = new List<string>();

    public List<BallSaveData> balls = new List<BallSaveData>();
    public List<MachineSaveData> machines = new List<MachineSaveData>();
}

[System.Serializable]
public class BallSaveData
{
    public int id;
    public string ballDataId;
    public Vector2 position;
    public Vector2 velocity;
    public int currentClickCount;
    public bool isProcessing;
    public float currentEnergy; // Stores batteries' current energy
}

[System.Serializable]
public class MachineSaveData
{
    public string prefabName;
    public Vector2 position;
    public float rotationZ;
    public float currentEnergy;
    public int capturedBallId;
    public Vector2 entryDirection;
}

public class WorldSpaceIndicator : MonoBehaviour
{
    private Camera _cam;
    private float _startOrtho;
    private Vector3 _originalScale;
    private float _duration;
    private CanvasGroup _cg;
    
    private float _animatedScaleMultiplier = 0f;
    private float _elapsed = 0f;
    private bool _isExiting = false;

    public void Initialize(Vector3 originalScale, float duration)
    {
        _cam = Camera.main;
        _startOrtho = _cam != null ? _cam.orthographicSize : 5f;
        _originalScale = originalScale;
        _duration = duration;
        
        if (_cam != null)
        {
            transform.SetParent(_cam.transform, false);
        }

        _cg = GetComponent<CanvasGroup>();
        if (_cg != null) _cg.alpha = 0f;

        // Fade in
        if (_cg != null) _cg.DOFade(1f, 0.35f).SetUpdate(true);
        
        // Scale in with an elastic feel
        DOTween.To(() => _animatedScaleMultiplier, x => _animatedScaleMultiplier = x, 1f, 0.5f)
            .SetEase(Ease.OutElastic)
            .SetUpdate(true);

        StartCoroutine(LifecycleRoutine());
    }

    private void Update()
    {
        if (_cam == null) return;

        // 1. Position bottom-right of viewport (margin = 25% from boundaries)
        float aspect = _cam.aspect;
        float halfHeight = _cam.orthographicSize;
        float halfWidth = halfHeight * aspect;
        
        float marginX = halfWidth * 0.15f;
        float marginY = halfHeight * 0.25f;
        
        transform.localPosition = new Vector3(halfWidth - marginX, -halfHeight + marginY, 10f);

        // 2. Adjust for camera zoom
        float zoomFactor = _cam.orthographicSize / _startOrtho;

        // 3. Smooth satisfying continuous oscillation (breathing pulse)
        float pulse = 1f;
        if (!_isExiting)
        {
            _elapsed += Time.unscaledDeltaTime;
            // Smoothly blend in the oscillation as the opening scale completes
            float blend = Mathf.Clamp01((_animatedScaleMultiplier - 0.8f) / 0.2f);
            pulse = 1f + Mathf.Sin(_elapsed * 4f) * 0.06f * blend;
        }

        transform.localScale = _originalScale * _animatedScaleMultiplier * zoomFactor * pulse;
    }

    private IEnumerator LifecycleRoutine()
    {
        yield return new WaitForSecondsRealtime(_duration - 0.4f);
        
        _isExiting = true;
        
        // Fade out
        if (_cg != null) _cg.DOFade(0f, 0.35f).SetUpdate(true);
        
        // Scale out
        DOTween.To(() => _animatedScaleMultiplier, x => _animatedScaleMultiplier = x, 0f, 0.35f)
            .SetEase(Ease.InBack)
            .SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.4f);
        Destroy(gameObject);
    }
}
