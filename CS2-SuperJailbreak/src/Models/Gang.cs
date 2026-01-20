namespace SuperJailbreak.Models;

/// <summary>
/// Representa uma Gang/Cla no sistema de Jailbreak
/// </summary>
public class Gang
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ulong LeaderSteamId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Membros
    public List<GangMember> Members { get; set; } = new();
    public int MaxMembers { get; set; } = 10;

    // Estatisticas
    public int TotalCredits { get; set; }
    public int TotalLRWins { get; set; }
    public int TotalRebellions { get; set; }
    public int TotalKills { get; set; }
    public int Level { get; set; } = 1;
    public int Experience { get; set; }

    // Upgrades/Perks
    public List<GangPerk> UnlockedPerks { get; set; } = new();

    // Visual
    public Color GangColor { get; set; } = Color.White;
    public string? CustomPrefix { get; set; }

    public int GetMemberCount() => Members.Count;

    public bool CanAddMember() => Members.Count < MaxMembers;

    public int GetRequiredExpForNextLevel()
    {
        return Level * 1000; // 1000 XP por level
    }

    public void AddExperience(int amount)
    {
        Experience += amount;
        while (Experience >= GetRequiredExpForNextLevel())
        {
            Experience -= GetRequiredExpForNextLevel();
            Level++;
            MaxMembers = Math.Min(10 + Level, 25); // Max 25 membros
        }
    }
}

public class GangMember
{
    public ulong SteamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public GangRank Rank { get; set; }
    public DateTime JoinedAt { get; set; }
    public int ContributedCredits { get; set; }
    public int ContributedKills { get; set; }
}

public class GangPerk
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Cost { get; set; }
    public int RequiredLevel { get; set; }
    public PerkType Type { get; set; }
    public float Value { get; set; }
}

public enum PerkType
{
    CreditBonus,        // +X% creditos ganhos
    HealthBonus,        // +X HP inicial
    SpeedBonus,         // +X% velocidade
    LRWinBonus,         // +X% chance de bonus em LR
    RebelDamageBonus,   // +X% dano como rebelde
    ArmorBonus,         // +X armor inicial
    GrenadeOnSpawn,     // Granada ao spawnar
    FlashOnSpawn,       // Flash ao spawnar
    KnifeSpeed,         // +X% velocidade com faca
    DoubleJump          // Pulo duplo
}
