using System;
using UnityEngine;

/// <summary>
/// プレイヤーが保持するゲーム内通貨を管理するための軽量ウォレット。
/// シーンに依存せず、加算・消費・残高照会・変更通知を一元的に扱う。
/// </summary>
public class PlayerCurrencyWallet
{
    public event Action<int> OnCurrencyChanged;

    private int balance;

    public PlayerCurrencyWallet(int initialBalance = 0)
    {
        balance = Mathf.Max(0, initialBalance);
    }

    /// <summary>
    /// 現在の所持通貨額。
    /// </summary>
    public int Balance => balance;

    /// <summary>
    /// 現在の残高が指定額以上か判定する。
    /// </summary>
    public bool HasEnough(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("[PlayerCurrencyWallet] HasEnough に負数が指定されました。");
            return true;
        }

        return balance >= amount;
    }

    /// <summary>
    /// 通貨を加算する。負数の加算・オーバーフローは拒否される。
    /// </summary>
    public bool TryAdd(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("[PlayerCurrencyWallet] TryAdd に負数が指定されました。処理を中断します。");
            return false;
        }

        if (amount == 0)
        {
            return true;
        }

        long candidate = (long)balance + amount;
        if (candidate > int.MaxValue)
        {
            Debug.LogWarning("[PlayerCurrencyWallet] 通貨の加算で int.MaxValue を超過しました。加算を拒否します。");
            return false;
        }

        UpdateBalanceInternal((int)candidate);
        return true;
    }

    /// <summary>
    /// 通貨を消費する。負数・残高不足・オーバーフローを防御する。
    /// </summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("[PlayerCurrencyWallet] TrySpend に負数が指定されました。処理を中断します。");
            return false;
        }

        if (amount == 0)
        {
            return true;
        }

        if (amount > balance)
        {
            Debug.LogWarning("[PlayerCurrencyWallet] 残高不足のため消費できません。所持: " + balance + " / 必要: " + amount);
            return false;
        }

        UpdateBalanceInternal(balance - amount);
        return true;
    }

    /// <summary>
    /// 残高を直接設定する。負数指定時は 0 に切り上げる。
    /// </summary>
    public void SetBalance(int amount)
    {
        int clamped = Mathf.Max(0, amount);
        UpdateBalanceInternal(clamped);
    }

    /// <summary>
    /// 現在残高のスナップショットを取得する。
    /// </summary>
    public int GetSnapshot()
    {
        return balance;
    }

    private void UpdateBalanceInternal(int newValue)
    {
        if (newValue == balance)
        {
            return;
        }

        balance = newValue;
        OnCurrencyChanged?.Invoke(balance);
    }
}
