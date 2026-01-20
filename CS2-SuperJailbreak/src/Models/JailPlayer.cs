using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace SuperJailbreak.Models;

/// <summary>
/// Representa um jogador no modo Jailbreak com todas suas propriedades e estados
/// </summary>
public class JailPlayer
{
    // Identificacao
    public int Slot { get; set; }
    public ulong SteamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CCSPlayerController? Controller { get; set; }

    // Estados de Jogo
    public bool IsRebel { get; set; }
    public bool IsMuted { get; set; }
    public bool IsFreedayPlayer { get; set; }
    public bool IsPardonned { get; set; }
    public bool IsInLR { get; set; }
    public bool IsAlive { get; set; }

    // Sistema de Warden
    public bool IsWarden { get; set; }
    public int WardenTime { get; set; }
    public int TimesWarden { get; set; }

    // Estatisticas de Last Request
    public int LRWins { get; set; }
    public int LRLosses { get; set; }
    public int LRStreak { get; set; }
    public LRType? CurrentLR { get; set; }

    // Sistema de Economia
    public int Credits { get; set; }
    public int TotalCreditsEarned { get; set; }
    public int TotalCreditsSpent { get; set; }

    // Sistema de Gang
    public int? GangId { get; set; }
    public string? GangName { get; set; }
    public GangRank GangRank { get; set; } = GangRank.Member;

    // Achievements
    public List<string> UnlockedAchievements { get; set; } = new();
    public int AchievementPoints { get; set; }

    // Estatisticas Gerais
    public int RoundsPlayed { get; set; }
    public int RoundsWonAsCT { get; set; }
    public int RoundsWonAsT { get; set; }
    public int TimesRebelled { get; set; }
    public int SuccessfulRebellions { get; set; }
    public int GuardsKilled { get; set; }
    public int PrisonersKilled { get; set; }

    // Cooldowns e Timers
    public DateTime? LastWardenTime { get; set; }
    public DateTime? LastLRTime { get; set; }
    public DateTime? LastShopPurchase { get; set; }

    // Cores e Visual
    public Color PlayerColor { get; set; } = Color.White;
    public bool HasGlow { get; set; }

    public void Reset()
    {
        IsRebel = false;
        IsMuted = false;
        IsFreedayPlayer = false;
        IsPardonned = false;
        IsInLR = false;
        CurrentLR = null;
        PlayerColor = Color.White;
        HasGlow = false;
    }

    public void ResetRound()
    {
        IsRebel = false;
        IsPardonned = false;
        IsInLR = false;
        CurrentLR = null;
        PlayerColor = Color.White;
        HasGlow = false;
    }
}

public enum GangRank
{
    Member = 0,
    Officer = 1,
    CoLeader = 2,
    Leader = 3
}

public enum LRType
{
    Knife,
    NoScope,
    Shot4Shot,
    Mag4Mag,
    Dodgeball,
    GunToss,
    Grenade,
    RussianRoulette,
    HeadshotOnly,
    ScoutKnife,
    Shotgun,
    Deagle,
    Rebel,
    Race,
    MathChallenge,
    HotPotato
}

public struct Color
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; }

    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static Color White => new(255, 255, 255);
    public static Color Red => new(255, 0, 0);
    public static Color Green => new(0, 255, 0);
    public static Color Blue => new(0, 0, 255);
    public static Color Yellow => new(255, 255, 0);
    public static Color Orange => new(255, 165, 0);
    public static Color Purple => new(128, 0, 128);
    public static Color Cyan => new(0, 255, 255);
    public static Color Pink => new(255, 192, 203);
    public static Color Lime => new(0, 255, 0);
}
