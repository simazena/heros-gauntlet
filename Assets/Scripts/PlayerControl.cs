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
    private int _animIDPunch;
    private int _animIDHookPunch;
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

    void Start()
    {
        maxHealth = health;
        _animator = GetComponent<Animator>();
        _input = GetComponent<StarterAssetsInputs>();
        _characterController = GetComponent<CharacterController>();
        _thirdPersonController = GetComponent<ThirdPersonController>();
        _animIDPunch = Animator.StringToHash("Punch");
        _animIDHookPunch = Animator.StringToHash("HookPunch");
        _animIDRoll = Animator.StringToHash("Roll");
        _animIDJumpOver = Animator.StringToHash("JumpOver");
    }

    void Update()
    {
        healthText.text = health + " / " + maxHealth;
        healthBar.value = (float)health / (float)maxHealth;

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
            _animator.SetTrigger(_animIDPunch);
            DealDamage(lightDamage);
            _punchLockoutEnd = Time.time + lightPunchLockout;
            _input.attack = false;
        }

        if (_input.heavyAttack)
        {
            _animator.SetTrigger(_animIDHookPunch);
            DealDamage(heavyDamage);
            _punchLockoutEnd = Time.time + heavyPunchLockout;
            _input.heavyAttack = false;
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
