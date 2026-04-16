using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int health = 50;
    public float deathDuration = 5f;
    public Vector3 healthBarOffset = new Vector3(0f, 2.2f, 0f);
    public float healthBarWidth = 60f;
    public float healthBarHeight = 6f;

    private bool _dying;
    private int _maxHealth;

    void Start()
    {
        if (_maxHealth <= 0) _maxHealth = Mathf.Max(1, health);
    }

    public void TakeDamage(int amount)
    {
        if (_dying) return;
        health -= amount;
        if (health <= 0) StartCoroutine(Die());
    }

    void OnGUI()
    {
        if (_dying || health <= 0) return;
        if (_maxHealth <= 0) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + healthBarOffset);
        if (screenPos.z <= 0f) return;

        float x = screenPos.x - healthBarWidth * 0.5f;
        float y = Screen.height - screenPos.y - healthBarHeight * 0.5f;
        float fill = Mathf.Clamp01((float)health / (float)_maxHealth);

        Color prev = GUI.color;
        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        GUI.DrawTexture(new Rect(x, y, healthBarWidth, healthBarHeight), Texture2D.whiteTexture);
        GUI.color = new Color(0.85f, 0.15f, 0.15f, 1f);
        GUI.DrawTexture(new Rect(x, y, healthBarWidth * fill, healthBarHeight), Texture2D.whiteTexture);
        GUI.color = prev;
    }

    private IEnumerator Die()
    {
        _dying = true;
        health = 0;

        EnemyChase chase = GetComponent<EnemyChase>();
        if (chase != null) chase.enabled = false;
        EnemyAttack attack = GetComponent<EnemyAttack>();
        if (attack != null) attack.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.SetTrigger("EnemyDeath");

        yield return new WaitForSeconds(deathDuration);
        Destroy(gameObject);
    }
}
