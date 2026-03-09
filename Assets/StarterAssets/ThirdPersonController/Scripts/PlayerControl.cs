using UnityEngine;
using TMPro;
using UnityEngine.UI;
using StarterAssets;

public class PlayerControl : MonoBehaviour
{
    public Slider healthBar;
    public TMP_Text healthText;
    public int health = 100;
    public int maxHealth = 0;

    private Animator _animator;
    private StarterAssetsInputs _input;
    private int _animIDPunch;
    private int _animIDHookPunch;

    void Start()
    {
        maxHealth = health;
        _animator = GetComponent<Animator>();
        _input = GetComponent<StarterAssetsInputs>();
        _animIDPunch = Animator.StringToHash("Punch");
        _animIDHookPunch = Animator.StringToHash("HookPunch");
    }

    void Update()
    {
        healthText.text = health + " / " + maxHealth;
        healthBar.value = (float)health / (float)maxHealth;

        if (_input.attack)
        {
            _animator.SetTrigger(_animIDPunch);
            _input.attack = false;
        }

        if (_input.heavyAttack)
        {
            _animator.SetTrigger(_animIDHookPunch);
            _input.heavyAttack = false;
        }
    }
}
