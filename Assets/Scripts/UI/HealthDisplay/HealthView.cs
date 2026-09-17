using UnityEngine;

public abstract class HealthView : MonoBehaviour
{
    [SerializeField] private Health _health;

    protected Health Health => _health;

    protected virtual void Awake()
    {
        if (_health == null)
        {
            _health = GetComponentInParent<Health>();
        }

        if (_health == null)
        {
            Debug.LogError($"{nameof(HealthView)} health not assigned on {gameObject.name}.", gameObject);
            enabled = false;
        }
    }

    protected virtual void OnEnable()
    {
        Health.Changed += Render;
        Render(Health.Current, Health.Maximum);
    }

    protected virtual void Start()
    {
        // Порядок Awake между объектами не гарантирован: рендер в OnEnable мог пройти
        // до Health.Awake и показать Current = 0. К Start все Awake уже выполнены.
        Render(Health.Current, Health.Maximum);
    }

    protected virtual void OnDisable()
    {
        Health.Changed -= Render;
    }

    protected abstract void Render(int current, int maximum);
}
