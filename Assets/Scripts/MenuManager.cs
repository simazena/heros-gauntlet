using UnityEngine;
using StarterAssets;

public class MenuManager : MonoBehaviour
{
    public string gameTitle = "Hero's Gauntlet";
    public float healthFillDuration = 1.5f;

    public static MenuManager Instance;

    private enum State { Title, Controls, HowToPlay, Playing }
    private State _state = State.Title;

    private WaveSpawner _waveSpawner;
    private PlayerControl _player;
    private ThirdPersonController _tpc;
    private float _menuStartTime;

    public bool InMenu => _state != State.Playing;
    public float HealthFill => healthFillDuration <= 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - _menuStartTime) / healthFillDuration);

    void OnEnable() { Instance = this; }
    void OnDisable() { if (Instance == this) Instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<MenuManager>() != null) return;
        GameObject go = new GameObject("MenuManager");
        go.AddComponent<MenuManager>();
    }

    void Start()
    {
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

    void OnGUI()
    {
        if (_state == State.Playing) return;

        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;

        switch (_state)
        {
            case State.Title: DrawTitle(); break;
            case State.Controls: DrawControls(); break;
            case State.HowToPlay: DrawHowToPlay(); break;
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
        if (GUI.Button(new Rect(x, y + spacing * 2, w, h), "How to Play", btnStyle)) _state = State.HowToPlay;
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

    void DrawHowToPlay()
    {
        DrawHeader("How to Play");

        GUIStyle body = new GUIStyle(GUI.skin.label);
        body.fontSize = 22;
        body.alignment = TextAnchor.UpperCenter;
        body.normal.textColor = Color.white;
        body.wordWrap = true;

        string text =
            "Hero's Gauntlet is a wave-based arena combat game.\n\n" +
            "Enemies pour out of the portal in increasingly tough waves. " +
            "Punch and kick them down with combos, and roll to dodge. You're invulnerable during the roll.\n\n" +
            "Between waves, jump onto the platforms around the arena to grab health pickups (10 HP each). " +
            "You can't pick them up at full health, so save them for when you need them.\n\n" +
            "Survive every wave to win. If your health hits zero, it's game over and the run resets.";
        GUI.Label(new Rect(Screen.width * 0.15f, Screen.height * 0.22f, Screen.width * 0.7f, Screen.height * 0.55f), text, body);

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
        _state = State.Playing;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
