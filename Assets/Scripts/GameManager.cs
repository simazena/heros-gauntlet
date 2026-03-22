using UnityEngine;

public class GameManager : MonoBehaviour
{
    private PlayerControl _player;
    private WaveSpawner _waveSpawner;
    private int _initialEnemyCount;
    private bool _won;
    private bool _lost;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.GetComponent<PlayerControl>();
        _waveSpawner = FindFirstObjectByType<WaveSpawner>();
        _initialEnemyCount = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length;
    }

    void Update()
    {
        if (_won || _lost) return;

        if (_player != null && _player.health <= 0)
        {
            _lost = true;
            return;
        }

        if (_waveSpawner != null)
        {
            if (_waveSpawner.AllWavesDone) _won = true;
        }
        else if (_initialEnemyCount > 0 && FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length == 0)
        {
            _won = true;
        }
    }

    void OnGUI()
    {
        if (_waveSpawner != null && !_won && !_lost && _waveSpawner.CurrentWave >= 1)
        {
            GUIStyle waveStyle = new GUIStyle(GUI.skin.label);
            waveStyle.fontSize = 30;
            waveStyle.alignment = TextAnchor.UpperCenter;
            waveStyle.fontStyle = FontStyle.Bold;
            waveStyle.normal.textColor = Color.white;
            string wave = "Wave " + _waveSpawner.CurrentWave + " / " + _waveSpawner.TotalWaves;
            GUI.Label(new Rect(0, 10, Screen.width, 50), wave, waveStyle);
        }

        if (!_won && !_lost) return;

        string message = _won ? "YOU WIN" : "GAME OVER";
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 80;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = _won ? Color.green : Color.red;

        Rect rect = new Rect(0, 0, Screen.width, Screen.height);
        GUI.Label(rect, message, style);
    }
}
