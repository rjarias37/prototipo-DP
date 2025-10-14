using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerKeyring : MonoBehaviour
{
    [SerializeField] private int keyCount = 0;
    public int KeyCount => keyCount;

    public event Action<int> OnKeyCountChanged;

    public void AddKey(int amount = 1)
    {
        if (amount < 1) amount = 1;
        keyCount += amount;
        OnKeyCountChanged?.Invoke(keyCount);
    }

    public bool TryUseKey(int amount = 1)
    {
        if (amount < 1) amount = 1;
        if (keyCount < amount) return false;
        keyCount -= amount;
        OnKeyCountChanged?.Invoke(keyCount);
        return true;
    }
}
