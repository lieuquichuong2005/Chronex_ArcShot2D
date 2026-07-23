public sealed class MatchResultData
{
    public string PlayerName { get; set; }
    public bool IsVictory { get; set; }

    public float DamageDealt { get; set; }
    public float DamageTaken { get; set; }
    public float MatchDuration { get; set; }
    public float Accuracy { get; set; }
    public int Turns { get; set; }
    public int CritCount { get; set; }

    public int CoinBonus { get; set; }
    public int ExpBonus { get; set; }

    public int CurrentLevel { get; set; }
    public int CurrentExp { get; set; }
    public int RequiredExp { get; set; }

    public bool IsMVP => IsVictory;
}