using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField, Min(1)] private int _maximum = 3;

    public event Action<int, int> Changed;
    public event Action Died;

    public int Current { get; private set; }
    public int Maximum => _maximum;
    public bool IsAlive => Current > 0;

    private void Awake()
    {
        Current = _maximum;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (IsAlive == false)
        {
            return;
        }

        Current = Mathf.Max(Current - amount, 0);
        Changed?.Invoke(Current, _maximum);

        if (Current == 0)
        {
            Died?.Invoke();
        }
    }

    public bool Heal(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        if (IsAlive == false)
        {
            return false;
        }

        if (Current >= _maximum)
        {
            return false;
        }

        int missing = _maximum - Current;
        int restored = Mathf.Min(amount, missing);
        Current += restored;
        Changed?.Invoke(Current, _maximum);

        return true;
    }
}
