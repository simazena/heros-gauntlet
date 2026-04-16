using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    public int healAmount = 10;
    public float rotateSpeed = 90f;
    public float bobAmplitude = 0.15f;
    public float bobSpeed = 2f;
    public AudioClip pickupSfx;

    private float _baseY;

    void Start()
    {
        _baseY = transform.position.y;
    }

    void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
        Vector3 p = transform.position;
        p.y = _baseY + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = p;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerControl player = other.GetComponent<PlayerControl>();
        if (player == null || player.health <= 0) return;
        if (player.health >= player.maxHealth) return;
        player.health = Mathf.Min(player.maxHealth, player.health + healAmount);
        if (pickupSfx != null) AudioSource.PlayClipAtPoint(pickupSfx, transform.position);
        Destroy(gameObject);
    }
}
