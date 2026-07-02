using System;

public class GameManager : Singleton<GameManager>
{
    private long money;

    public long Money => money;
    public event Action<long> OnMoneyChanged;

    public void AddMoney(long amount)
    {
        if (amount <= 0)
            return;

        money += amount;
        OnMoneyChanged?.Invoke(money);
    }

    public bool TrySpendMoney(long amount)
    {
        if (amount == 0)
            return true;

        if (amount < 0 || money < amount)
            return false;

        money -= amount;
        OnMoneyChanged?.Invoke(money);
        RoguelikeRunEvents.NotifyGoldSpent(amount);
        return true;
    }

    public void SetMoney(long amount)
    {
        money = Math.Max(0, amount);
        OnMoneyChanged?.Invoke(money);
    }
}
