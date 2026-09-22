using UnityEngine;
using UnityEngine.UI;

public class HealthSimulator : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private Button _damageButton;
    [SerializeField] private Button _healButton;
    [SerializeField, Min(1)] private int _damageAmount = 10;
    [SerializeField, Min(1)] private int _healAmount = 10;

    private void Awake()
    {
        if (_health == null)
        {
            Debug.LogError($"{nameof(HealthSimulator)} health not assigned on {gameObject.name}.", gameObject);
            enabled = false;
            return;
        }

        if (_damageButton == null)
        {
            Debug.LogError($"{nameof(HealthSimulator)} damage button not assigned on {gameObject.name}.", gameObject);
            enabled = false;
            return;
        }

        if (_healButton == null)
        {
            Debug.LogError($"{nameof(HealthSimulator)} heal button not assigned on {gameObject.name}.", gameObject);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        _damageButton.onClick.AddListener(OnDamageButtonClicked);
        _healButton.onClick.AddListener(OnHealButtonClicked);
    }

    private void OnDisable()
    {
        _damageButton.onClick.RemoveListener(OnDamageButtonClicked);
        _healButton.onClick.RemoveListener(OnHealButtonClicked);
    }

    private void OnDamageButtonClicked()
    {
        _health.TakeDamage(_damageAmount);
    }

    private void OnHealButtonClicked()
    {
        _health.Heal(_healAmount);
    }
}
