using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;

public class MenuManager : MonoBehaviour
{
    public string gameTitle = "Hero's Gauntlet";
    public float healthFillDuration = 1.5f;
    public float storyChunkDuration = 5f;
    public float storyFadeFraction = 0.15f;
    public string[] storyChunks = new string[]
    {
        "You are an enhanced hero, placed inside the Gauntlet, an arena built to test superhuman fighters.",
        "To prove yourself, you must survive escalating waves of fast brawlers and heavy brutes.",
        "Chain punch and kick combos, roll to evade strikes, and vault over foes to reposition.",
        "Restorative pickups appear on platforms between waves. Complete the Gauntlet to prove you are ready for greater threats beyond."
    };

    public static MenuManager Instance;
    public static bool SkipMenuOnLoad;
    public enum Difficulty { Easy, Normal, Hard }
    public static Difficulty SelectedDifficulty = Difficulty.Normal;

    private enum State { Title, Controls, Story, Playing }
    private State _state = State.Title;

    private WaveSpawner _waveSpawner;
    private PlayerControl _player;
    private ThirdPersonController _tpc;
    private float _menuStartTime;
    private int _storyIndex;
    private float _storyChunkStart;

    public bool InMenu => _state == State.Title || _state == State.Controls;
    public float HealthFill => healthFillDuration <= 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - _menuStartTime) / healthFillDuration);

    void OnEnable() { Instance = this; }
    void OnDisable() { if (Instance == this) Instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureExists();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureExists();
    }

    static void EnsureExists()
    {
        if (FindAnyObjectByType<MenuManager>() != null) return;
        GameObject go = new GameObject("MenuManager");
        go.AddComponent<MenuManager>();
    }

    void Start()
    {
        if (SkipMenuOnLoad)
        {
            SkipMenuOnLoad = false;
            _state = State.Playing;
            return;
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _menuStartTime = Time.unscaledTime;

        _waveSpawner = FindAnyObjectByType<WaveSpawner>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _player = p.GetComponent<PlayerControl>();
            _tpc = p.GetComponent<ThirdPersonController>();
            if (_player != null) _player.enabled = false;
            if (_tpc != null) _tpc.enabled = false;
        }
    }

    void Update()
    {
        if (_state != State.Story) return;
        if (storyChunks == null || storyChunks.Length == 0) { FinishStory(); return; }
        if (Time.time - _storyChunkStart >= storyChunkDuration)
        {
            _storyIndex++;
            if (_storyIndex >= storyChunks.Length) { FinishStory(); return; }
            _storyChunkStart = Time.time;
        }
    }

    void OnGUI()
    {
        if (_state == State.Playing) return;
        if (_state == State.Story) { DrawStory(); return; }

        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;

        switch (_state)
        {
            case State.Title: DrawTitle(); break;
            case State.Controls: DrawControls(); break;
        }
    }

    void DrawStory()
    {
        if (storyChunks == null || _storyIndex >= storyChunks.Length) return;

        float panelHeight = 140f;
        float panelY = Screen.height - panelHeight - 150f;

        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(0, panelY, Screen.width, panelHeight), Texture2D.whiteTexture);
        GUI.color = prev;

        float t = Mathf.Clamp01((Time.time - _storyChunkStart) / storyChunkDuration);
        float fade = Mathf.Clamp01(storyFadeFraction);
        float alpha = 1f;
        if (fade > 0f)
        {
            if (t < fade) alpha = t / fade;
            else if (t > 1f - fade) alpha = (1f - t) / fade;
        }
        alpha = Mathf.Clamp01(alpha);

        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 28;
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.wordWrap = true;
        textStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);

        GUI.Label(new Rect(Screen.width * 0.1f, panelY, Screen.width * 0.8f, panelHeight), storyChunks[_storyIndex], textStyle);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 18;
        btnStyle.fontStyle = FontStyle.Bold;
        Color btnText = btnStyle.normal.textColor;
        btnStyle.hover.textColor = btnText;
        btnStyle.active.textColor = btnText;
        btnStyle.focused.textColor = btnText;
        btnStyle.onHover.textColor = btnText;
        btnStyle.onActive.textColor = btnText;
        btnStyle.onFocused.textColor = btnText;
        float bw = 140f, bh = 36f;
        if (GUI.Button(new Rect(Screen.width - bw - 20f, Screen.height - bh - 20f, bw, bh), "Skip Story", btnStyle))
        {
            FinishStory();
        }
    }

    void DrawTitle()
    {
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 80;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height * 0.12f, Screen.width, 120), gameTitle, titleStyle);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 28;
        btnStyle.fontStyle = FontStyle.Bold;
        Color titleTextColor = btnStyle.normal.textColor;
        btnStyle.hover.textColor = titleTextColor;
        btnStyle.active.textColor = titleTextColor;
        btnStyle.focused.textColor = titleTextColor;
        btnStyle.onHover.textColor = titleTextColor;
        btnStyle.onActive.textColor = titleTextColor;
        btnStyle.onFocused.textColor = titleTextColor;

        float w = 300, h = 60, spacing = 80;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height * 0.4f;

        if (GUI.Button(new Rect(x, y, w, h), "Play", btnStyle)) StartGame();
        if (GUI.Button(new Rect(x, y + spacing, w, h), "Controls", btnStyle)) _state = State.Controls;

        float diffY = y + spacing * 2 + 20f;
        GUIStyle diffLabel = new GUIStyle(GUI.skin.label);
        diffLabel.fontSize = 22;
        diffLabel.alignment = TextAnchor.MiddleCenter;
        diffLabel.fontStyle = FontStyle.Bold;
        diffLabel.normal.textColor = Color.white;
        GUI.Label(new Rect(0, diffY, Screen.width, 30), "Difficulty", diffLabel);

        float dw = 110f, dh = 50f, dgap = 12f;
        float total = 3 * dw + 2 * dgap;
        float dx = (Screen.width - total) / 2f;
        float dyy = diffY + 36f;
        string[] names = { "Easy", "Normal", "Hard" };
        for (int i = 0; i < 3; i++)
        {
            Difficulty d = (Difficulty)i;
            bool selected = SelectedDifficulty == d;
            GUIStyle ds = new GUIStyle(GUI.skin.button);
            ds.fontSize = 22;
            ds.fontStyle = FontStyle.Bold;
            Color c = selected ? new Color(1f, 0.9f, 0.4f, 1f) : btnStyle.normal.textColor;
            ds.normal.textColor = c;
            ds.hover.textColor = c;
            ds.active.textColor = c;
            ds.focused.textColor = c;
            ds.onHover.textColor = c;
            ds.onActive.textColor = c;
            ds.onFocused.textColor = c;
            if (GUI.Button(new Rect(dx + i * (dw + dgap), dyy, dw, dh), names[i], ds))
            {
                SelectedDifficulty = d;
            }
        }
    }

    void DrawControls()
    {
        DrawHeader("Controls");

        GUIStyle body = new GUIStyle(GUI.skin.label);
        body.fontSize = 24;
        body.alignment = TextAnchor.MiddleCenter;
        body.normal.textColor = Color.white;

        string text =
            "W A S D - Move\n" +
            "Mouse - Look\n" +
            "Shift - Sprint\n" +
            "Space (tap) - Jump\n" +
            "Space (hold) - Jump Over\n" +
            "Left Ctrl - Roll (invulnerable)\n" +
            "Left Click - Punch combo (3 hits)\n" +
            "Right Click - Kick combo (2 hits)";
        GUI.Label(new Rect(0, Screen.height * 0.22f, Screen.width, Screen.height * 0.55f), text, body);

        DrawBackButton();
    }

    void DrawHeader(string title)
    {
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 50;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height * 0.08f, Screen.width, 80), title, headerStyle);
    }

    void DrawBackButton()
    {
        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 24;
        btnStyle.fontStyle = FontStyle.Bold;
        Color backTextColor = btnStyle.normal.textColor;
        btnStyle.hover.textColor = backTextColor;
        btnStyle.active.textColor = backTextColor;
        btnStyle.focused.textColor = backTextColor;
        btnStyle.onHover.textColor = backTextColor;
        btnStyle.onActive.textColor = backTextColor;
        btnStyle.onFocused.textColor = backTextColor;
        float w = 200, h = 50;
        if (GUI.Button(new Rect((Screen.width - w) / 2f, Screen.height * 0.85f, w, h), "Back", btnStyle))
        {
            _state = State.Title;
        }
    }

    void StartGame()
    {
        _state = State.Story;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _storyIndex = 0;
        _storyChunkStart = Time.time;
        if (storyChunks == null || storyChunks.Length == 0) FinishStory();
    }

    void FinishStory()
    {
        _state = State.Playing;
        if (_player != null)
        {
            StarterAssetsInputs input = _player.GetComponent<StarterAssetsInputs>();
            if (input != null)
            {
                input.attack = false;
                input.heavyAttack = false;
                input.jump = false;
            }
            _player.enabled = true;
        }
        if (_tpc != null) _tpc.enabled = true;
        if (_waveSpawner != null) _waveSpawner.StartGame();
    }
}
