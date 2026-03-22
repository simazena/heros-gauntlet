using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int[] waveCounts = new int[] { 2, 4, 6 };
    public float spawnRadius = 10f;
    public float timeBetweenWaves = 2f;

    private int _currentWave = -1;
    private bool _allWavesDone;
    private float _nextWaveTime;
    private RuntimeAnimatorController _sharedAnimatorController;
    private readonly List<EnemyHealth> _activeEnemies = new List<EnemyHealth>();

    public int CurrentWave => _currentWave + 1;
    public int TotalWaves => waveCounts.Length;
    public bool AllWavesDone => _allWavesDone;

    void Start()
    {
        _nextWaveTime = Time.time + timeBetweenWaves;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            Animator playerAnim = p.GetComponentInChildren<Animator>();
            if (playerAnim != null) _sharedAnimatorController = playerAnim.runtimeAnimatorController;
        }
    }

    void Update()
    {
        if (_allWavesDone) return;

        _activeEnemies.RemoveAll(e => e == null);

        if (_activeEnemies.Count == 0 && Time.time >= _nextWaveTime)
        {
            _currentWave++;
            if (_currentWave >= waveCounts.Length)
            {
                _allWavesDone = true;
                return;
            }
            SpawnWave(waveCounts[_currentWave]);
            _nextWaveTime = Time.time + timeBetweenWaves;
        }
    }

    private void SpawnWave(int count)
    {
        if (enemyPrefab == null) return;
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * spawnRadius;
            Vector3 pos = transform.position + new Vector3(offset.x, 0f, offset.y);
            GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
            ConfigureAsEnemy(enemy);
            EnemyHealth eh = enemy.GetComponent<EnemyHealth>();
            if (eh != null) _activeEnemies.Add(eh);
        }
    }

    private void ConfigureAsEnemy(GameObject enemy)
    {
        enemy.name = "Enemy";

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
