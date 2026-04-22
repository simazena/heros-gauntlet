using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public GameObject enemy2Prefab;
    public Vector3 spawnPosition = new Vector3(0.3f, 0.6f, 16f);
    public int[] waveCounts = new int[] { 2, 4, 6 };
    public int[] enemy2Counts = new int[] { 0, 1, 2 };
    public int enemy2Health = 100;
    public float enemy2MoveSpeed = 1f;
    public int enemy2Damage = 15;
    public float enemy2AttackCooldown = 5f;
    public AudioClip enemy1AttackSfx;
    public AudioClip enemy2AttackSfx;
    public AudioClip healthPickupSfx;
    public float breakDuration = 5f;
    public float countdownDuration = 3f;
    public float spawnDelay = 1f;

    public Vector3[] platformPositions = new Vector3[] {
        new Vector3(-7f, 1.5f, -2f),
        new Vector3(7f, 1.5f, -2f),
        new Vector3(0f, 1.5f, 8f)
    };
    public Vector3 platformScale = new Vector3(3f, 0.5f, 3f);
    public float pickupHeightAbovePlatform = 1f;
    public Color platformColor = new Color(0.3f, 0.3f, 0.35f);
    public Color pickupColor = new Color(0.2f, 1f, 0.4f);

    public static bool AutoStartOnLoad;

    private int _currentWave = -1;
    private bool _allWavesDone;
    private bool _isSpawning;
    private bool _interWaveTimerSet;
    private bool _gameStarted;
    private float _nextWaveTime;
    private RuntimeAnimatorController _sharedAnimatorController;
    private PlayerControl _playerControl;
    private readonly List<EnemyHealth> _activeEnemies = new List<EnemyHealth>();
    private readonly List<GameObject> _activePickups = new List<GameObject>();

    public int CurrentWave => _currentWave + 1;
    public int TotalWaves => waveCounts.Length;
    public bool AllWavesDone => _allWavesDone;
    public bool ShowingCountdown =>
        !_allWavesDone && !_isSpawning && _activeEnemies.Count == 0
        && _nextWaveTime > Time.time && (_nextWaveTime - Time.time) <= countdownDuration;
    public float TimeUntilNextWave => Mathf.Max(0f, _nextWaveTime - Time.time);

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _playerControl = p.GetComponent<PlayerControl>();
            Animator playerAnim = p.GetComponentInChildren<Animator>();
            if (playerAnim != null) _sharedAnimatorController = playerAnim.runtimeAnimatorController;
        }

        SpawnPlatforms();

        if (AutoStartOnLoad)
        {
            AutoStartOnLoad = false;
            StartGame();
        }
    }

    public void StartGame()
    {
        _gameStarted = true;
        _nextWaveTime = Time.time + breakDuration + countdownDuration;
    }

    void Update()
    {
        if (!_gameStarted) return;
        if (_allWavesDone) return;
        if (_playerControl != null && _playerControl.health <= 0) return;

        _activeEnemies.RemoveAll(e => e == null || e.health <= 0);

        if (!_isSpawning && _activeEnemies.Count == 0 && _currentWave >= 0 && !_interWaveTimerSet)
        {
            if (_currentWave >= waveCounts.Length - 1)
            {
                _allWavesDone = true;
                return;
            }
            _nextWaveTime = Time.time + breakDuration + countdownDuration;
            _interWaveTimerSet = true;
            SpawnHealthPickups();
        }

        if (!_isSpawning && _activeEnemies.Count == 0 && Time.time >= _nextWaveTime)
        {
            _currentWave++;
            if (_currentWave >= waveCounts.Length)
            {
                _allWavesDone = true;
                return;
            }
            _interWaveTimerSet = false;
            StartCoroutine(SpawnWave(_currentWave));
        }
    }

    private IEnumerator SpawnWave(int wave)
    {
        _isSpawning = true;
        int e1 = wave < waveCounts.Length ? waveCounts[wave] : 0;
        int e2 = wave < enemy2Counts.Length ? enemy2Counts[wave] : 0;
        int total = (enemyPrefab != null ? e1 : 0) + (enemy2Prefab != null ? e2 : 0);
        int spawned = 0;

        if (enemyPrefab != null)
        {
            for (int i = 0; i < e1; i++)
            {
                SpawnOne(enemyPrefab, false);
                spawned++;
                if (spawned < total) yield return new WaitForSeconds(spawnDelay);
            }
        }
        if (enemy2Prefab != null)
        {
            for (int i = 0; i < e2; i++)
            {
                SpawnOne(enemy2Prefab, true);
                spawned++;
                if (spawned < total) yield return new WaitForSeconds(spawnDelay);
            }
        }
        _isSpawning = false;
    }

    private void SpawnOne(GameObject prefab, bool isEnemy2)
    {
        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
        ConfigureAsEnemy(enemy);
        if (isEnemy2)
        {
            ApplyEnemy2Stats(enemy);
        }
        else
        {
            EnemyAttack ea = enemy.GetComponent<EnemyAttack>();
            if (ea != null) ea.attackSfx = enemy1AttackSfx;
        }
        EnemyHealth eh = enemy.GetComponent<EnemyHealth>();
        if (eh != null) _activeEnemies.Add(eh);
    }

    private void ApplyEnemy2Stats(GameObject enemy)
    {
        EnemyHealth eh = enemy.GetComponent<EnemyHealth>();
        if (eh != null) eh.health = enemy2Health;
        EnemyChase ec = enemy.GetComponent<EnemyChase>();
        if (ec != null) ec.moveSpeed = enemy2MoveSpeed;
        EnemyAttack ea = enemy.GetComponent<EnemyAttack>();
        if (ea != null)
        {
            ea.damage = enemy2Damage;
            ea.attackCooldown = enemy2AttackCooldown;
            ea.attackSfx = enemy2AttackSfx;
        }
    }

    private void SpawnPlatforms()
    {
        foreach (Vector3 pos in platformPositions)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "HealthPlatform";
            platform.transform.position = pos;
            platform.transform.localScale = platformScale;
            Renderer r = platform.GetComponent<Renderer>();
            if (r != null) r.material.color = platformColor;
        }
    }

    private void SpawnHealthPickups()
    {
        for (int i = _activePickups.Count - 1; i >= 0; i--)
        {
            if (_activePickups[i] != null) Destroy(_activePickups[i]);
        }
        _activePickups.Clear();

        foreach (Vector3 platformPos in platformPositions)
        {
            Vector3 pickupPos = platformPos + Vector3.up * (platformScale.y * 0.5f + pickupHeightAbovePlatform);
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickup.name = "HealthPickup";
            pickup.transform.position = pickupPos;
            pickup.transform.localScale = Vector3.one * 0.5f;
            Collider col = pickup.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            Renderer r = pickup.GetComponent<Renderer>();
            if (r != null) r.material.color = pickupColor;
            HealthPickup hp = pickup.AddComponent<HealthPickup>();
            hp.pickupSfx = healthPickupSfx;
            _activePickups.Add(pickup);
        }
    }

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    private void ConfigureAsEnemy(GameObject enemy)
    {
        enemy.name = "Enemy";
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) SetLayerRecursive(enemy, enemyLayer);

        if (enemy.GetComponent<EnemyHealth>() == null) enemy.AddComponent<EnemyHealth>();
        if (enemy.GetComponent<EnemyChase>() == null) enemy.AddComponent<EnemyChase>();
        if (enemy.GetComponent<EnemyAttack>() == null) enemy.AddComponent<EnemyAttack>();

        if (enemy.GetComponent<Collider>() == null)
        {
            CapsuleCollider cap = enemy.AddComponent<CapsuleCollider>();
            cap.height = 1.8f;
            cap.radius = 0.4f;
            cap.center = new Vector3(0f, 0.9f, 0f);
        }

        Animator anim = enemy.GetComponentInChildren<Animator>();
        if (anim != null && _sharedAnimatorController != null && anim.runtimeAnimatorController == null)
        {
            anim.runtimeAnimatorController = _sharedAnimatorController;
        }
    }
}
