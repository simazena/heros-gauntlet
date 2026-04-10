using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using StarterAssets;

[DefaultExecutionOrder(-100)]
public class PlayerControl : MonoBehaviour
{
    public Slider healthBar;
    public TMP_Text healthText;
    public int health = 100;
    public int maxHealth = 0;

    public int lightDamage = 10;
    public int heavyDamage = 25;
    public float attackRange = 1.5f;
    public float attackRadius = 1f;
    public float lightPunchLockout = 0.5f;
    public float heavyPunchLockout = 0.8f;
    public float comboWindow = 0.6f;

    public float rollDuration = 1.4f;
    public float rollCooldown = 1.6f;
    public float rollDistance = 3f;

    public float jumpOverDuration = 1.5f;
    public float jumpOverCooldown = 2.0f;
    public float jumpOverDistance = 5f;
    public float jumpOverHeight = 0.5f;
    public float jumpOverLaunchDelay = 0.25f;

    public float holdThreshold = 0.5f;

    public bool IsInvulnerable { get; private set; }

    private Animator _animator;
    private StarterAssetsInputs _input;
    private CharacterController _characterController;
    private ThirdPersonController _thirdPersonController;
    private int _animIDPunch1;
    private int _animIDPunch2;
    private int _animIDPunch3;
    private int _animIDKick1;
    private int _animIDKick2;
    private int _animIDPlayerDeath;
    private bool _isDead;
    private int _animIDRoll;
    private int _animIDJumpOver;
    private float _activeEndTime;
    private float _activeSpeed;
    private float _nextActionTime;
    private float _spacePressedAt = -1f;
    private bool _jumpOverFired;
    private bool _activeDisableCC;
    private float _activeStartTime;
    private float _activeDuration;
    private float _activeArcHeight;
    private float _activeStartY;
    private float _activeArcDelay;
    private float _punchLockoutEnd;
    private float _comboWindowEnd;
    private int _comboStep;
    private float _kickComboWindowEnd;
    private int _kickComboStep;

    void Start()
    {
        maxHealth = health;
        _animator = GetComponent<Animator>();
        _input = GetComponent<StarterAssetsInputs>();
        _characterController = GetComponent<CharacterController>();
        _thirdPersonController = GetComponent<ThirdPersonController>();
        _animIDPunch1 = Animator.StringToHash("Punch1");
        _animIDPunch2 = Animator.StringToHash("Punch2");
        _animIDPunch3 = Animator.StringToHash("Punch3");
        _animIDKick1 = Animator.StringToHash("Kick1");
        _animIDKick2 = Animator.StringToHash("Kick2");
        _animIDPlayerDeath = Animator.StringToHash("PlayerDeath");
        _animIDRoll = Animator.StringToHash("Roll");
        _animIDJumpOver = Animator.StringToHash("JumpOver");
    }

    void Update()
    {
        healthText.text = health + " / " + maxHealth;
        healthBar.value = (float)health / (float)maxHealth;

        if (!_isDead && health <= 0)
        {
            _isDead = true;
            _animator.SetTrigger(_animIDPlayerDeath);
            if (_characterController != null) _characterController.enabled = false;
        }
        if (_isDead)
        {
            if (_thirdPersonController != null && _thirdPersonController.enabled)
            {
                _thirdPersonController.enabled = false;
            }
            return;
        }

        if (IsInvulnerable && Time.time >= _activeEndTime)
        {
            IsInvulnerable = false;
            if (_activeDisableCC && _characterController != null) _characterController.enabled = true;
            _activeDisableCC = false;
        }

        if (IsInvulnerable)
        {
            Vector3 step = transform.forward * _activeSpeed * Time.deltaTime;
            if (_characterController != null && _characterController.enabled)
            {
                _characterController.Move(step);
            }
            else
            {
                float progress = Mathf.Clamp01((Time.time - _activeStartTime) / _activeDuration);
                float arcProgress = Mathf.Clamp01((progress - _activeArcDelay) / (1f - _activeArcDelay));
                float sinValue = Mathf.Sin(arcProgress * Mathf.PI);
                float yArc = _activeArcHeight * sinValue * sinValue;
                transform.position = new Vector3(
                    transform.position.x + step.x,
                    _activeStartY + yArc,
                    transform.position.z + step.z);
            }
        }

        HandleActionInput();

        if (_input.attack)
        {
            if (Time.time < _punchLockoutEnd)
            {
                _input.attack = false;
            }
            else
            {
                int nextStep = (Time.time < _comboWindowEnd) ? _comboStep + 1 : 1;
                if (nextStep > 3) nextStep = 1;
                int trigger = nextStep == 2 ? _animIDPunch2
                            : nextStep == 3 ? _animIDPunch3
                            : _animIDPunch1;
                _animator.SetTrigger(trigger);
                DealDamage(lightDamage);
                _punchLockoutEnd = Time.time + lightPunchLockout;
                _comboStep = nextStep == 3 ? 0 : nextStep;
                _comboWindowEnd = _punchLockoutEnd + comboWindow;
                _input.attack = false;
            }
        }

        if (_input.heavyAttack)
        {
            if (Time.time < _punchLockoutEnd)
            {
                _input.heavyAttack = false;
            }
            else
            {
                bool combo = _kickComboStep == 1 && Time.time < _kickComboWindowEnd;
                _animator.SetTrigger(combo ? _animIDKick2 : _animIDKick1);
                DealDamage(heavyDamage);
                _punchLockoutEnd = Time.time + heavyPunchLockout;
                _kickComboStep = combo ? 0 : 1;
                _kickComboWindowEnd = _punchLockoutEnd + comboWindow;
                _input.heavyAttack = false;
            }
        }

        if (_thirdPersonController != null)
        {
            bool shouldEnable = !IsInvulnerable && Time.time >= _punchLockoutEnd;
            if (_thirdPersonController.enabled != shouldEnable)
            {
                _thirdPersonController.enabled = shouldEnable;
            }
        }
    }

    private void HandleActionInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            TryStartAction(_animIDRoll, rollDuration, rollDistance, rollCooldown, false, 0f, 0f);
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            _spacePressedAt = Time.time;
            _jumpOverFired = false;
            _input.jump = false;
        }

        if (Keyboard.current.spaceKey.isPressed && !_jumpOverFired && _spacePressedAt >= 0f)
        {
            if (Time.time - _spacePressedAt >= holdThreshold)
            {
                TryStartAction(_animIDJumpOver, jumpOverDuration, jumpOverDistance, jumpOverCooldown, true, jumpOverHeight, jumpOverLaunchDelay);
                _jumpOverFired = true;
            }
            else
            {
                _input.jump = false;
            }
        }

        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            if (!_jumpOverFired && _spacePressedAt >= 0f)
            {
                _input.jump = true;
            }
            _spacePressedAt = -1f;
        }
    }

    private void TryStartAction(int triggerID, float duration, float distance, float cooldown, bool disableCC, float arcHeight, float arcDelay)
    {
        if (Time.time < _nextActionTime || health <= 0) return;
        if (_thirdPersonController == null || !_thirdPersonController.Grounded) return;

        _animator.SetTrigger(triggerID);
        IsInvulnerable = true;
        _activeSpeed = distance / duration;
        _activeEndTime = Time.time + duration;
        _nextActionTime = Time.time + cooldown;
        _activeStartTime = Time.time;
        _activeDuration = duration;
        _activeArcHeight = arcHeight;
        _activeArcDelay = arcDelay;
        _activeStartY = transform.position.y;
        _activeDisableCC = disableCC;
        if (disableCC && _characterController != null) _characterController.enabled = false;
    }

    private void DealDamage(int amount)
    {
        Vector3 center = transform.position + transform.forward * attackRange + Vector3.up;
        Collider[] hits = Physics.OverlapSphere(center, attackRadius);
        foreach (var hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(amount);
            }
        }
    }
}
