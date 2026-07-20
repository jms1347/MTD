using System;
using UnityEngine;

namespace SamgukMarble
{
    /// <summary>
    /// 낙양 국고 적립금 증가 및 획득 로직.
    /// </summary>
    public class TreasuryManager : MonoBehaviour
    {
        public static TreasuryManager Instance { get; private set; }

        [SerializeField] int nationalTreasury;

        public int NationalTreasury => nationalTreasury;
        public event Action<int> OnTreasuryChanged;

        void Awake()
        {
            Instance = this;
            nationalTreasury = 0;
        }

        public void Deposit(int amount)
        {
            if (amount <= 0) return;
            nationalTreasury += amount;
            OnTreasuryChanged?.Invoke(nationalTreasury);
            Debug.Log($"[국고] +{amount} → 잔액 {nationalTreasury}");
        }

        /// <summary>
        /// 국고 전액을 플레이어에게 지급 후 0으로 리셋.
        /// </summary>
        public int CollectAll(Player3D player)
        {
            int amount = nationalTreasury;
            nationalTreasury = 0;
            OnTreasuryChanged?.Invoke(nationalTreasury);
            if (player != null && amount > 0)
            {
                player.AddGold(amount);
                Debug.Log($"[국고] {player.PlayerName}이(가) {amount} 금화 수령");
            }
            return amount;
        }

        public void ResetTreasury()
        {
            nationalTreasury = 0;
            OnTreasuryChanged?.Invoke(nationalTreasury);
        }
    }
}
