using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace SuperJailbreak.Core;

/// <summary>
/// Configuracao principal do plugin Super Jailbreak
/// Versao atualizada para CS2 com CounterStrikeSharp 1.0.300+
/// </summary>
public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 1;

    // ==================== DATABASE ====================
    [JsonPropertyName("DatabaseHost")]
    public string DatabaseHost { get; set; } = "localhost";

    [JsonPropertyName("DatabasePort")]
    public int DatabasePort { get; set; } = 3306;

    [JsonPropertyName("DatabaseName")]
    public string DatabaseName { get; set; } = "superjailbreak";

    [JsonPropertyName("DatabaseUser")]
    public string DatabaseUser { get; set; } = "root";

    [JsonPropertyName("DatabasePassword")]
    public string DatabasePassword { get; set; } = "";

    [JsonPropertyName("EnableDatabase")]
    public bool EnableDatabase { get; set; } = true;

    // ==================== WARDEN ====================
    [JsonPropertyName("WardenEnabled")]
    public bool WardenEnabled { get; set; } = true;

    [JsonPropertyName("WardenLaserEnabled")]
    public bool WardenLaserEnabled { get; set; } = true;

    [JsonPropertyName("WardenLaserColor")]
    public string WardenLaserColor { get; set; } = "0,0,255"; // Azul

    [JsonPropertyName("WardenPaintEnabled")]
    public bool WardenPaintEnabled { get; set; } = true;

    [JsonPropertyName("WardenAutoAssign")]
    public bool WardenAutoAssign { get; set; } = false;

    [JsonPropertyName("WardenCooldownSeconds")]
    public int WardenCooldownSeconds { get; set; } = 60;

    [JsonPropertyName("WardenMaxTimeMinutes")]
    public int WardenMaxTimeMinutes { get; set; } = 5;

    [JsonPropertyName("WardenIconEnabled")]
    public bool WardenIconEnabled { get; set; } = true;

    [JsonPropertyName("WardenGlowEnabled")]
    public bool WardenGlowEnabled { get; set; } = true;

    // ==================== LAST REQUEST ====================
    [JsonPropertyName("LREnabled")]
    public bool LREnabled { get; set; } = true;

    [JsonPropertyName("LRMinCTsAlive")]
    public int LRMinCTsAlive { get; set; } = 1;

    [JsonPropertyName("LRMaxTsForLR")]
    public int LRMaxTsForLR { get; set; } = 2;

    [JsonPropertyName("LRTimeoutSeconds")]
    public int LRTimeoutSeconds { get; set; } = 60;

    [JsonPropertyName("LRRebelOnWeaponPickup")]
    public bool LRRebelOnWeaponPickup { get; set; } = true;

    [JsonPropertyName("LRAllowedTypes")]
    public List<string> LRAllowedTypes { get; set; } = new()
    {
        "Knife", "NoScope", "Shot4Shot", "Mag4Mag", "Dodgeball",
        "GunToss", "Grenade", "RussianRoulette", "HeadshotOnly",
        "ScoutKnife", "Shotgun", "Deagle", "Race", "HotPotato"
    };

    // ==================== SPECIAL DAYS ====================
    [JsonPropertyName("SpecialDaysEnabled")]
    public bool SpecialDaysEnabled { get; set; } = true;

    [JsonPropertyName("MaxSpecialDaysPerMap")]
    public int MaxSpecialDaysPerMap { get; set; } = 3;

    [JsonPropertyName("SpecialDayCooldownRounds")]
    public int SpecialDayCooldownRounds { get; set; } = 3;

    [JsonPropertyName("AllowedSpecialDays")]
    public List<string> AllowedSpecialDays { get; set; } = new()
    {
        "Freeday", "Warday", "HideAndSeek", "Zombie", "GunGame",
        "Dodgeball", "HeadshotOnly", "KnifeFight", "NoScope",
        "GravityFreeday", "BattleRoyale", "OneInTheChamber",
        "GoldenKnife", "SimonSays", "HotPotato", "FreezeTag"
    };

    // ==================== ECONOMY ====================
    [JsonPropertyName("EconomyEnabled")]
    public bool EconomyEnabled { get; set; } = true;

    [JsonPropertyName("StartingCredits")]
    public int StartingCredits { get; set; } = 100;

    [JsonPropertyName("CreditsPerRound")]
    public int CreditsPerRound { get; set; } = 10;

    [JsonPropertyName("CreditsPerKill")]
    public int CreditsPerKill { get; set; } = 25;

    [JsonPropertyName("CreditsPerLRWin")]
    public int CreditsPerLRWin { get; set; } = 100;

    [JsonPropertyName("CreditsPerWardenRound")]
    public int CreditsPerWardenRound { get; set; } = 50;

    [JsonPropertyName("CreditsPerRebellionKill")]
    public int CreditsPerRebellionKill { get; set; } = 50;

    // ==================== GANGS ====================
    [JsonPropertyName("GangsEnabled")]
    public bool GangsEnabled { get; set; } = true;

    [JsonPropertyName("GangCreationCost")]
    public int GangCreationCost { get; set; } = 5000;

    [JsonPropertyName("GangMaxMembers")]
    public int GangMaxMembers { get; set; } = 10;

    [JsonPropertyName("GangTagMaxLength")]
    public int GangTagMaxLength { get; set; } = 6;

    // ==================== ACHIEVEMENTS ====================
    [JsonPropertyName("AchievementsEnabled")]
    public bool AchievementsEnabled { get; set; } = true;

    [JsonPropertyName("AchievementAnnouncements")]
    public bool AchievementAnnouncements { get; set; } = true;

    // ==================== MUTE SYSTEM ====================
    [JsonPropertyName("MuteDeadEnabled")]
    public bool MuteDeadEnabled { get; set; } = true;

    [JsonPropertyName("MuteTsOnRoundStart")]
    public bool MuteTsOnRoundStart { get; set; } = true;

    [JsonPropertyName("MuteTsDurationSeconds")]
    public int MuteTsDurationSeconds { get; set; } = 30;

    // ==================== TEAM BALANCE ====================
    [JsonPropertyName("TeamBalanceEnabled")]
    public bool TeamBalanceEnabled { get; set; } = true;

    [JsonPropertyName("CTRatio")]
    public float CTRatio { get; set; } = 0.33f; // 1 CT para 3 Ts

    [JsonPropertyName("MinCTsRequired")]
    public int MinCTsRequired { get; set; } = 1;

    // ==================== REBEL SYSTEM ====================
    [JsonPropertyName("RebelEnabled")]
    public bool RebelEnabled { get; set; } = true;

    [JsonPropertyName("RebelOnDamageToGuard")]
    public bool RebelOnDamageToGuard { get; set; } = true;

    [JsonPropertyName("RebelOnWeaponPickup")]
    public bool RebelOnWeaponPickup { get; set; } = true;

    [JsonPropertyName("RebelOnEnterRestrictedArea")]
    public bool RebelOnEnterRestrictedArea { get; set; } = true;

    [JsonPropertyName("RebelGlowEnabled")]
    public bool RebelGlowEnabled { get; set; } = true;

    [JsonPropertyName("RebelGlowColor")]
    public string RebelGlowColor { get; set; } = "255,0,0"; // Vermelho

    // ==================== DOORS ====================
    [JsonPropertyName("OpenCellsOnRoundStart")]
    public bool OpenCellsOnRoundStart { get; set; } = false;

    [JsonPropertyName("OpenCellsDelaySeconds")]
    public int OpenCellsDelaySeconds { get; set; } = 60;

    [JsonPropertyName("WardenCanOpenCells")]
    public bool WardenCanOpenCells { get; set; } = true;

    // ==================== SOUNDS ====================
    [JsonPropertyName("SoundsEnabled")]
    public bool SoundsEnabled { get; set; } = true;

    [JsonPropertyName("WardenAssignedSound")]
    public string WardenAssignedSound { get; set; } = "sounds/superjailbreak/warden_assigned.wav";

    [JsonPropertyName("WardenDiedSound")]
    public string WardenDiedSound { get; set; } = "sounds/superjailbreak/warden_died.wav";

    [JsonPropertyName("LRStartSound")]
    public string LRStartSound { get; set; } = "sounds/superjailbreak/lr_start.wav";

    // ==================== MISC ====================
    [JsonPropertyName("PluginPrefix")]
    public string PluginPrefix { get; set; } = "[SuperJailbreak]";

    [JsonPropertyName("PluginPrefixColor")]
    public string PluginPrefixColor { get; set; } = "Purple";

    [JsonPropertyName("Language")]
    public string Language { get; set; } = "pt-BR";

    [JsonPropertyName("DebugMode")]
    public bool DebugMode { get; set; } = false;
}
