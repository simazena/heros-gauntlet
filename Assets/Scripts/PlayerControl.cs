using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

[DefaultExecutionOrder(-100)]
public class PlayerControl : MonoBehaviour
{
    public int health = 100;
    public int maxHealth = 0;

    public int punch1Damage = 10;
    public int punch2Damage = 15;
    public int punch3Damage = 20;
    public int kick1Damage = 20;
    public int kick2Damage = 25;
    public float attackRange = 0.5f;
    public float attackRadius = 1f;
    public float lightPunchLockout = 0.5f;
    public float heavyPunchLockout = 0.8f;
    public float comboWindow = 0.5f;
    public float attackHitNormalizedTime = 0.45f;
    public float attackFallbackHitDelay = 0.3f;
    public float attackLockoutTrim = 0.35f;
    public float punch1Arc = 60f;
    public float punch2Arc = 90f;
    public float punch3Arc = 60f;
    public float kick1Arc = 90f;
    public float kick2Arc = 210f;
    public float punch1ArcOffset = 0f;
    public float punch2ArcOffset = 45f;
    public float punch3ArcOffset = 0f;
    public float kick1ArcOffset = 45f;
    public float kick2ArcOffset = 0f;

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
    private float _attackInputLockEnd;
    private float _comboWindowEnd;
    private int _comboStep;
    private float _kickComboWindowEnd;
    private int _kickComboStep;
    private float _groundY;

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
        _groundY = transform.position.y;
    }

    void Update()
    {
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
                float effectiveHeight = _activeArcHeight + Mathf.Max(0f, (_activeStartY - _groundY) * 0.5f);
                float yArc = effectiveHeight * sinValue * sinValue;
                float yLerp = Mathf.Lerp(_activeStartY, _groundY, arcProgress);
                transform.position = new Vector3(
                    transform.position.x + step.x,
                    yLerp + yArc,
                    transform.position.z + step.z);
            }
        }

        HandleActionInput();

        bool grounded = _thirdPersonController != null && _thirdPersonController.Grounded;

        if (_input.attack)
        {
            if (Time.time < _attackInputLockEnd || !grounded || IsInvulnerable)
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
                int dmg = nextStep == 2 ? punch2Damage
                        : nextStep == 3 ? punch3Damage
                        : punch1Damage;
                float arc = nextStep == 2 ? punch2Arc
                          : nextStep == 3 ? punch3Arc
                          : punch1Arc;
                float arcOffset = nextStep == 2 ? punch2ArcOffset
                                : nextStep == 3 ? punch3ArcOffset
                                : punch1ArcOffset;
                _animator.SetTrigger(trigger);
                StartCoroutine(DealDamageDelayed(dmg, trigger, arc, arcOffset));
                _punchLockoutEnd = Time.time + lightPunchLockout;
                _attackInputLockEnd = _punchLockoutEnd;
                _comboStep = nextStep == 3 ? 0 : nextStep;
                _comboWindowEnd = _attackInputLockEnd + comboWindow;
                StartCoroutine(SyncLockoutToAnimation(trigger));
                _input.attack = false;
            }
        }

        if (_input.heavyAttack)
        {
            if (Time.time < _attackInputLockEnd || !grounded || IsInvulnerable)
            {
                _input.heavyAttack = false;
            }
            else
            {
                bool combo = _kickComboStep == 1 && Time.time < _kickComboWindowEnd;
                int kickTrigger = combo ? _animIDKick2 : _animIDKick1;
                _animator.SetTrigger(kickTrigger);
                StartCoroutine(DealDamageDelayed(combo ? kick2Damage : kick1Damage, kickTrigger, combo ? kick2Arc : kick1Arc, combo ? kick2ArcOffset : kick1ArcOffset));
                _punchLockoutEnd = Time.time + heavyPunchLockout;
                _attackInputLockEnd = _punchLockoutEnd;
                _kickComboStep = combo ? 0 : 1;
                _kickComboWindowEnd = _attackInputLockEnd + comboWindow;
                StartCoroutine(SyncLockoutToAnimation(kickTrigger));
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

    private void DealDamage(int amount, float arcDegrees, float arcOffset)
    {
        if (transform.position.y > _groundY + 0.5f) return;
        float reach = attackRange + attackRadius;
        Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up, reach);
        Vector3 centerDir = Quaternion.Euler(0f, arcOffset, 0f) * transform.forward;
        float halfCos = Mathf.Cos(arcDegrees * 0.5f * Mathf.Deg2Rad);
        float reachSqr = reach * reach;
        foreach (var hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
            if (enemy == null) continue;
            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;
            float distSqr = toEnemy.sqrMagnitude;
            if (distSqr > reachSqr) continue;
            if (distSqr < 0.0001f)
            {
                enemy.TakeDamage(amount);
                continue;
            }
            if (Vector3.Dot(centerDir, toEnemy.normalized) < halfCos) continue;
            enemy.TakeDamage(amount);
        }
    }

    private IEnumerator SyncLockoutToAnimation(int stateHash)
    {
        if (_animator == null) yield break;
        yield return null;
        float waitStart = Time.time;
        while (_animator.IsInTransition(0))
        {
            if (Time.time - waitStart > 0.5f) yield break;
            yield return null;
        }
        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
        if (info.shortNameHash != stateHash) yield break;
        float speed = Mathf.Max(0.01f, info.speed);
        float fullDuration = info.length / speed;
        float trimmedDuration = Mathf.Max(0.1f, fullDuration - attackLockoutTrim);
        _punchLockoutEnd = Time.time + trimmedDuration;
        _attackInputLockEnd = Time.time + fullDuration;
        if (stateHash == _animIDKick1 || stateHash == _animIDKick2)
        {
            _kickComboWindowEnd = _attackInputLockEnd + comboWindow;
        }
        else
        {
            _comboWindowEnd = _attackInputLockEnd + comboWindow;
        }
    }

    private IEnumerator DealDamageDelayed(int damage, int stateHash, float arcDegrees, float arcOffset)
    {
        if (_animator == null)
        {
            yield return new WaitForSeconds(attackFallbackHitDelay);
            DealDamage(damage, arcDegrees, arcOffset);
            yield break;
        }

        yield return null;
        float waitStart = Time.time;
        while (_animator.IsInTransition(0))
        {
            if (Time.time - waitStart > 0.5f) break;
            yield return null;
        }

        float pollStart = Time.time;
        while (true)
        {
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash != stateHash) yield break;
            if (info.normalizedTime >= attackHitNormalizedTime) break;
            if (Time.time - pollStart > 1f) break;
            yield return null;
        }

        DealDamage(damage, arcDegrees, arcOffset);
    }
}
