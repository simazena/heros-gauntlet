using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public float gameOverRestartDelay = 5f;
    public AudioClip gameOverSfx;
    public float waveBannerDuration = 3f;
    public string[] waveBanners = new string[]
    {
        "First Blood",
        "The Brutes Awaken",
        "Final Trial"
    };

    private PlayerControl _player;
    private WaveSpawner _waveSpawner;
    private int _initialEnemyCount;
    private bool _won;
    private bool _lost;
    private float _restartTime;
    private int _lastBannerWave;
    private float _bannerEndTime;
    private Texture2D _vignetteTex;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.GetComponent<PlayerControl>();
        _waveSpawner = FindAnyObjectByType<WaveSpawner>();
        _initialEnemyCount = FindObjectsByType<EnemyHealth>().Length;
        _vignetteTex = BuildVignetteTexture(256);
    }

    private Texture2D BuildVignetteTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxDist = center.magnitude;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float a = Mathf.Clamp01(Mathf.Pow(d, 2f));
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    void Update()
    {
        if (_lost && Time.time >= _restartTime)
        {
            WaveSpawner.AutoStartOnLoad = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }
        if (_won || _lost) return;

        if (_player != null && _player.health <= 0)
        {
            _lost = true;
            _restartTime = Time.time + gameOverRestartDelay;
            if (gameOverSfx != null)
            {
                Vector3 pos = _player != null ? _player.transform.position : Vector3.zero;
                AudioSource.PlayClipAtPoint(gameOverSfx, pos);
            }
            return;
        }

        if (_waveSpawner != null)
        {
            if (_waveSpawner.CurrentWave > _lastBannerWave && _waveSpawner.CurrentWave >= 1)
            {
                _lastBannerWave = _waveSpawner.CurrentWave;
                _bannerEndTime = Time.time + waveBannerDuration;
            }
            if (_waveSpawner.AllWavesDone) _won = true;
        }
        else if (_initialEnemyCount > 0 && FindObjectsByType<EnemyHealth>().Length == 0)
        {
            _won = true;
        }
    }

    void OnGUI()
    {
        GUI.depth = 1;

        DrawVignette();
        DrawHealthBar();

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

        if (_waveSpawner != null && !_won && !_lost && Time.time < _bannerEndTime)
        {
            int wave = _waveSpawner.CurrentWave;
            if (wave >= 1 && waveBanners != null && wave - 1 < waveBanners.Length)
            {
                float remaining = _bannerEndTime - Time.time;
                float t = waveBannerDuration <= 0f ? 1f : 1f - (remaining / waveBannerDuration);
                float fade = 0.2f;
                float alpha = 1f;
                if (t < fade) alpha = t / fade;
                else if (t > 1f - fade) alpha = (1f - t) / fade;
                alpha = Mathf.Clamp01(alpha);

                GUIStyle bannerStyle = new GUIStyle(GUI.skin.label);
                bannerStyle.fontSize = 60;
                bannerStyle.alignment = TextAnchor.MiddleCenter;
                bannerStyle.fontStyle = FontStyle.Bold;
                bannerStyle.normal.textColor = new Color(1f, 0.9f, 0.4f, alpha);

                string banner = "Wave " + wave + ": " + waveBanners[wave - 1];
                GUI.Label(new Rect(0, Screen.height * 0.20f, Screen.width, 100), banner, bannerStyle);
            }
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
                GUI.Label(new Rect(0, Screen.height * 0.15f, Screen.width, 100), text, countdownStyle);
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

    private void DrawVignette()
    {
        if (_player == null || _vignetteTex == null) return;
        if (MenuManager.Instance != null && MenuManager.Instance.InMenu) return;
        int max = _player.maxHealth;
        if (max <= 0) return;
        int hp = _player.health;
        if (hp <= 0 || hp > 20) return;

        float fill = Mathf.Clamp01((float)hp / (float)max);
        Color red = new Color(0.85f, 0.15f, 0.15f, 1f);
        Color yellow = new Color(0.95f, 0.8f, 0.1f, 1f);
        Color barColor = Color.Lerp(red, yellow, Mathf.Clamp01(fill * 2f));

        float intensity = hp <= 10 ? 1f : 0.55f;
        Color tint = new Color(barColor.r, barColor.g, barColor.b, intensity);

        Color prev = GUI.color;
        GUI.color = tint;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _vignetteTex);
        GUI.color = prev;
    }

    private void DrawHealthBar()
    {
        if (_player == null) return;
        if (MenuManager.Instance != null && MenuManager.Instance.InMenu) return;
        int max = _player.maxHealth;
        if (max <= 0) return;

        float fill = Mathf.Clamp01((float)_player.health / (float)max);
        int displayHealth = Mathf.Max(0, _player.health);

        float barWidth = 330f;
        float barHeight = 30f;
        float x = 20f;
        float y = Screen.height - barHeight - 20f;

        Color red = new Color(0.85f, 0.15f, 0.15f, 1f);
        Color yellow = new Color(0.95f, 0.8f, 0.1f, 1f);
        Color green = new Color(0.2f, 0.75f, 0.2f, 1f);
        Color barColor = fill > 0.5f
            ? Color.Lerp(yellow, green, (fill - 0.5f) * 2f)
            : Color.Lerp(red, yellow, fill * 2f);

        Color prev = GUI.color;
        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);
        GUI.color = barColor;
        GUI.DrawTexture(new Rect(x, y, barWidth * fill, barHeight), Texture2D.whiteTexture);
        GUI.color = prev;

        GUIStyle hpStyle = new GUIStyle(GUI.skin.label);
        hpStyle.fontSize = 18;
        hpStyle.alignment = TextAnchor.MiddleCenter;
        hpStyle.fontStyle = FontStyle.Bold;
        hpStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y, barWidth, barHeight), displayHealth + " / " + max, hpStyle);
    }
}
