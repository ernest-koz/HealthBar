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

        int previous = Current;

        Current = Mathf.Max(previous - amount, 0);

        if (Current == previous)
        {
            return;
        }

        Changed?.Invoke(Current, _maximum);

        if (Current == 0)
        {
            Died?.Invoke();
        }
    }

    public void ReceiveHealing(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int previous = Current;

        Current = Mathf.Min(previous + amount, _maximum);

        if (Current == previous)
        {
            return;
        }

        Changed?.Invoke(Current, _maximum);
    }
}
