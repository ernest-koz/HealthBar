using UnityEngine;

public class HealthSimulator : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField, Min(1)] private int _damageAmount = 10;
    [SerializeField, Min(1)] private int _healAmount = 10;

    private void Awake()
    {
        if (_health == null)
        {
            Debug.LogError($"{nameof(HealthSimulator)} health not assigned on {gameObject.name}.", gameObject);
            enabled = false;
        }
    }

    public void TakeDamage()
    {
        if (_health == null)
        {
            return;
        }

        _health.TakeDamage(_damageAmount, transform.position);
    }

    public void Heal()
    {
        if (_health == null)
        {
            return;
        }

        _health.Heal(_healAmount);
    }
}
