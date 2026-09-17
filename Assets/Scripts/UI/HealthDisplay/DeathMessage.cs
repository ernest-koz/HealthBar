using TMPro;
using UnityEngine;

public class DeathMessage : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private TMP_Text _text;

    private void Awake()
    {
        if (_health == null)
        {
            Debug.LogError($"{nameof(DeathMessage)} health not assigned on {gameObject.name}.", gameObject);
            enabled = false;
            return;
        }

        if (_text == null)
        {
            Debug.LogError($"{nameof(DeathMessage)} text not assigned on {gameObject.name}.", gameObject);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        _health.Died += Show;
        _text.enabled = false;
    }

    private void OnDisable()
    {
        _health.Died -= Show;
    }

    private void Show()
    {
        _text.enabled = true;
    }
}
