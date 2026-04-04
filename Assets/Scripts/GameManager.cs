using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public float gameOverRestartDelay = 5f;

    private PlayerControl _player;
    private WaveSpawner _waveSpawner;
    private int _initialEnemyCount;
    private bool _won;
    private bool _lost;
    private float _restartTime;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.GetComponent<PlayerControl>();
        _waveSpawner = FindAnyObjectByType<WaveSpawner>();
        _initialEnemyCount = FindObjectsByType<EnemyHealth>().Length;
    }

    void Update()
    {
        if (_lost && Time.time >= _restartTime)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }
        if (_won || _lost) return;

        if (_player != null && _player.health <= 0)
        {
            _lost = true;
            _restartTime = Time.time + gameOverRestartDelay;
            return;
        }

        if (_waveSpawner != null)
        {
            if (_waveSpawner.AllWavesDone) _won = true;
        }
        else if (_initialEnemyCount > 0 && FindObjectsByType<EnemyHealth>().Length == 0)
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

        if (_waveSpawner != null && !_won && !_lost && _waveSpawner.ShowingCountdown)
        {
            int seconds = Mathf.CeilToInt(_waveSpawner.TimeUntilNextWave);
            if (seconds > 0)
            {
                GUIStyle countdownStyle = new GUIStyle(GUI.skin.label);
                countdownStyle.fontSize = 50;
                countdownStyle.alignment = TextAnchor.MiddleCenter;
                countdownStyle.fontStyle = FontStyle.Bold;
                countdownStyle.normal.textColor = Color.yellow;
                string text = "Next wave in " + seconds + "...";
                GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 100), text, countdownStyle);
            }
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
