namespace SuperJailbreak.Models;

/// <summary>
/// Sistema de Achievements/Conquistas do Jailbreak
/// </summary>
public class Achievement
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconEmoji { get; set; } = string.Empty;
    public int Points { get; set; }
    public AchievementCategory Category { get; set; }
    public AchievementRarity Rarity { get; set; }
    public bool IsSecret { get; set; }

    // Requisitos
    public AchievementRequirement Requirement { get; set; } = new();

    // Recompensas
    public int CreditReward { get; set; }
    public string? SpecialReward { get; set; }
}

public class AchievementRequirement
{
    public AchievementType Type { get; set; }
    public int RequiredCount { get; set; }
    public string? SpecificCondition { get; set; }
}

public enum AchievementCategory
{
    Combat,
    LastRequest,
    Warden,
    Rebel,
    Economy,
    Social,
    Special,
    Seasonal
}

public enum AchievementRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum AchievementType
{
    // Combat
    KillGuards,
    KillPrisoners,
    Headshots,
    KnifeKills,
    NadeKills,

    // Last Request
    WinLR,
    WinLRStreak,
    WinSpecificLR,
    PlayLRCount,

    // Warden
    BecomeWarden,
    WardenRoundsWon,
    WardenTime,
    ExecuteMinigame,

    // Rebel
    SuccessfulRebellion,
    KillWardenAsRebel,
    EscapeAsRebel,

    // Economy
    EarnCredits,
    SpendCredits,
    PurchaseItems,

    // Social
    JoinGang,
    CreateGang,
    GangLevel,

    // Special
    PlayRounds,
    PlayTime,
    FirstBlood,
    Survivor
}

/// <summary>
/// Lista predefinida de achievements
/// </summary>
public static class AchievementList
{
    public static List<Achievement> GetAll()
    {
        return new List<Achievement>
        {
            // Combat Achievements
            new Achievement
            {
                Id = "first_blood",
                Name = "First Blood",
                Description = "Consiga o primeiro kill da rodada",
                IconEmoji = "🩸",
                Points = 10,
                Category = AchievementCategory.Combat,
                Rarity = AchievementRarity.Common,
                Requirement = new() { Type = AchievementType.FirstBlood, RequiredCount = 1 },
                CreditReward = 50
            },
            new Achievement
            {
                Id = "guard_slayer",
                Name = "Guard Slayer",
                Description = "Mate 100 guardas",
                IconEmoji = "💀",
                Points = 50,
                Category = AchievementCategory.Combat,
                Rarity = AchievementRarity.Rare,
                Requirement = new() { Type = AchievementType.KillGuards, RequiredCount = 100 },
                CreditReward = 500
            },
            new Achievement
            {
                Id = "headhunter",
                Name = "Headhunter",
                Description = "Faca 500 headshots",
                IconEmoji = "🎯",
                Points = 100,
                Category = AchievementCategory.Combat,
                Rarity = AchievementRarity.Epic,
                Requirement = new() { Type = AchievementType.Headshots, RequiredCount = 500 },
                CreditReward = 1000
            },

            // Last Request Achievements
            new Achievement
            {
                Id = "lr_beginner",
                Name = "LR Beginner",
                Description = "Venca seu primeiro Last Request",
                IconEmoji = "🏆",
                Points = 10,
                Category = AchievementCategory.LastRequest,
                Rarity = AchievementRarity.Common,
                Requirement = new() { Type = AchievementType.WinLR, RequiredCount = 1 },
                CreditReward = 100
            },
            new Achievement
            {
                Id = "lr_champion",
                Name = "LR Champion",
                Description = "Venca 50 Last Requests",
                IconEmoji = "👑",
                Points = 75,
                Category = AchievementCategory.LastRequest,
                Rarity = AchievementRarity.Rare,
                Requirement = new() { Type = AchievementType.WinLR, RequiredCount = 50 },
                CreditReward = 750
            },
            new Achievement
            {
                Id = "unstoppable",
                Name = "Unstoppable",
                Description = "Venca 5 LRs seguidos",
                IconEmoji = "🔥",
                Points = 100,
                Category = AchievementCategory.LastRequest,
                Rarity = AchievementRarity.Epic,
                Requirement = new() { Type = AchievementType.WinLRStreak, RequiredCount = 5 },
                CreditReward = 1000
            },

            // Warden Achievements
            new Achievement
            {
                Id = "new_warden",
                Name = "New Warden",
                Description = "Se torne Warden pela primeira vez",
                IconEmoji = "🎖️",
                Points = 10,
                Category = AchievementCategory.Warden,
                Rarity = AchievementRarity.Common,
                Requirement = new() { Type = AchievementType.BecomeWarden, RequiredCount = 1 },
                CreditReward = 50
            },
            new Achievement
            {
                Id = "warden_master",
                Name = "Warden Master",
                Description = "Seja Warden em 100 rodadas",
                IconEmoji = "⭐",
                Points = 100,
                Category = AchievementCategory.Warden,
                Rarity = AchievementRarity.Epic,
                Requirement = new() { Type = AchievementType.BecomeWarden, RequiredCount = 100 },
                CreditReward = 1500
            },

            // Rebel Achievements
            new Achievement
            {
                Id = "rebel_scum",
                Name = "Rebel Scum",
                Description = "Seja marcado como rebelde 10 vezes",
                IconEmoji = "😈",
                Points = 25,
                Category = AchievementCategory.Rebel,
                Rarity = AchievementRarity.Uncommon,
                Requirement = new() { Type = AchievementType.SuccessfulRebellion, RequiredCount = 10 },
                CreditReward = 200
            },
            new Achievement
            {
                Id = "cop_killer",
                Name = "Cop Killer",
                Description = "Mate o Warden como rebelde",
                IconEmoji = "🗡️",
                Points = 50,
                Category = AchievementCategory.Rebel,
                Rarity = AchievementRarity.Rare,
                Requirement = new() { Type = AchievementType.KillWardenAsRebel, RequiredCount = 1 },
                CreditReward = 500,
                IsSecret = true
            },

            // Economy Achievements
            new Achievement
            {
                Id = "rich_prisoner",
                Name = "Rich Prisoner",
                Description = "Acumule 10.000 creditos",
                IconEmoji = "💰",
                Points = 50,
                Category = AchievementCategory.Economy,
                Rarity = AchievementRarity.Rare,
                Requirement = new() { Type = AchievementType.EarnCredits, RequiredCount = 10000 },
                CreditReward = 500
            },

            // Social Achievements
            new Achievement
            {
                Id = "gang_member",
                Name = "Gang Member",
                Description = "Entre em uma gang",
                IconEmoji = "🤝",
                Points = 15,
                Category = AchievementCategory.Social,
                Rarity = AchievementRarity.Common,
                Requirement = new() { Type = AchievementType.JoinGang, RequiredCount = 1 },
                CreditReward = 100
            },
            new Achievement
            {
                Id = "gang_leader",
                Name = "Gang Leader",
                Description = "Crie sua propria gang",
                IconEmoji = "👔",
                Points = 50,
                Category = AchievementCategory.Social,
                Rarity = AchievementRarity.Rare,
                Requirement = new() { Type = AchievementType.CreateGang, RequiredCount = 1 },
                CreditReward = 500
            },

            // Special Achievements
            new Achievement
            {
                Id = "veteran",
                Name = "Veteran",
                Description = "Jogue 1000 rodadas",
                IconEmoji = "🎮",
                Points = 150,
                Category = AchievementCategory.Special,
                Rarity = AchievementRarity.Legendary,
                Requirement = new() { Type = AchievementType.PlayRounds, RequiredCount = 1000 },
                CreditReward = 2000
            },
            new Achievement
            {
                Id = "survivor",
                Name = "Survivor",
                Description = "Sobreviva a rodada inteira como T sem ser rebelde",
                IconEmoji = "🛡️",
                Points = 25,
                Category = AchievementCategory.Special,
                Rarity = AchievementRarity.Uncommon,
                Requirement = new() { Type = AchievementType.Survivor, RequiredCount = 1 },
                CreditReward = 150
            }
        };
    }
}
