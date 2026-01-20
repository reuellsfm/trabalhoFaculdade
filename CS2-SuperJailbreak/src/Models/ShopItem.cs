namespace SuperJailbreak.Models;

/// <summary>
/// Itens da loja do Jailbreak
/// </summary>
public class ShopItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Price { get; set; }
    public ShopCategory Category { get; set; }
    public ItemRarity Rarity { get; set; }
    public bool IsOneTimeUse { get; set; }
    public bool RequiresAlive { get; set; } = true;

    // Restricoes
    public TeamRestriction TeamRestriction { get; set; } = TeamRestriction.Both;
    public int RequiredLevel { get; set; }
    public int MaxPurchasesPerRound { get; set; } = 1;
    public int MaxPurchasesPerMap { get; set; }

    // Efeito
    public ItemEffect Effect { get; set; } = new();
}

public class ItemEffect
{
    public EffectType Type { get; set; }
    public float Value { get; set; }
    public int Duration { get; set; } // em segundos
    public string? WeaponName { get; set; }
    public string? CustomEffect { get; set; }
}

public enum ShopCategory
{
    Weapons,
    Equipment,
    Abilities,
    Cosmetics,
    Special
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum TeamRestriction
{
    Both,
    TerroristOnly,
    CTOnly
}

public enum EffectType
{
    GiveWeapon,
    GiveHealth,
    GiveArmor,
    GiveSpeed,
    GiveGravity,
    GiveInvisibility,
    GiveGrenade,
    GiveFlash,
    GiveSmoke,
    GiveMolotov,
    GiveKnife,
    Teleport,
    Respawn,
    FreedomPass, // Sai da prisao
    RebelPass,   // Imunidade de marcacao como rebelde por X segundos
    WardenVeto,  // Veta uma decisao do warden
    DoubleJump,
    Bhop,
    NoFallDamage,
    Glow,
    CustomModel,
    Trail
}

/// <summary>
/// Lista predefinida de itens da loja
/// </summary>
public static class ShopItemList
{
    public static List<ShopItem> GetAll()
    {
        return new List<ShopItem>
        {
            // Weapons - T Only
            new ShopItem
            {
                Id = "deagle",
                Name = "Desert Eagle",
                Description = "Uma poderosa pistola",
                Price = 500,
                Category = ShopCategory.Weapons,
                Rarity = ItemRarity.Rare,
                TeamRestriction = TeamRestriction.TerroristOnly,
                Effect = new() { Type = EffectType.GiveWeapon, WeaponName = "weapon_deagle" }
            },
            new ShopItem
            {
                Id = "knife_gold",
                Name = "Faca Dourada",
                Description = "Uma faca especial que causa mais dano",
                Price = 750,
                Category = ShopCategory.Weapons,
                Rarity = ItemRarity.Epic,
                TeamRestriction = TeamRestriction.TerroristOnly,
                Effect = new() { Type = EffectType.GiveKnife, CustomEffect = "gold_knife" }
            },
            new ShopItem
            {
                Id = "smoke",
                Name = "Granada de Fumaca",
                Description = "Perfeita para fugas",
                Price = 200,
                Category = ShopCategory.Equipment,
                Rarity = ItemRarity.Common,
                TeamRestriction = TeamRestriction.TerroristOnly,
                Effect = new() { Type = EffectType.GiveSmoke }
            },
            new ShopItem
            {
                Id = "flash",
                Name = "Granada Flash",
                Description = "Cegue seus inimigos",
                Price = 150,
                Category = ShopCategory.Equipment,
                Rarity = ItemRarity.Common,
                TeamRestriction = TeamRestriction.TerroristOnly,
                Effect = new() { Type = EffectType.GiveFlash }
            },

            // Equipment
            new ShopItem
            {
                Id = "health_boost",
                Name = "Kit Medico",
                Description = "+50 HP",
                Price = 300,
                Category = ShopCategory.Equipment,
                Rarity = ItemRarity.Uncommon,
                Effect = new() { Type = EffectType.GiveHealth, Value = 50 }
            },
            new ShopItem
            {
                Id = "armor",
                Name = "Colete Kevlar",
                Description = "+100 Armor",
                Price = 400,
                Category = ShopCategory.Equipment,
                Rarity = ItemRarity.Uncommon,
                Effect = new() { Type = EffectType.GiveArmor, Value = 100 }
            },
            new ShopItem
            {
                Id = "full_armor",
                Name = "Colete + Capacete",
                Description = "+100 Armor com protecao na cabeca",
                Price = 600,
                Category = ShopCategory.Equipment,
                Rarity = ItemRarity.Rare,
                Effect = new() { Type = EffectType.GiveArmor, Value = 100, CustomEffect = "helmet" }
            },

            // Abilities - T Only
            new ShopItem
            {
                Id = "speed_boost",
                Name = "Speed Boost",
                Description = "+20% velocidade por 30 segundos",
                Price = 350,
                Category = ShopCategory.Abilities,
                Rarity = ItemRarity.Rare,
                TeamRestriction = TeamRestriction.TerroristOnly,
                Effect = new() { Type = EffectType.GiveSpeed, Value = 1.2f, Duration = 30 }
            },
            new ShopItem
            {
                Id = "low_gravity",
                Name = "Low Gravity",
                Description = "Gravidade reduzida por 20 segundos",
                Price = 400,
                Category = ShopCategory.Abilities,
                Rarity = ItemRarity.Rare,
                TeamRestriction = TeamRestriction.TerroristOnly,
                Effect = new() { Type = EffectType.GiveGravity, Value = 0.5f, Duration = 20 }
            },
            new ShopItem
            {
                Id = "invisibility",
                Name = "Invisibilidade",
                Description = "Fique invisivel por 10 segundos",
                Price = 800,
                Category = ShopCategory.Abilities,
                Rarity = ItemRarity.Epic,
                TeamRestriction = TeamRestriction.TerroristOnly,
                Effect = new() { Type = EffectType.GiveInvisibility, Duration = 10 }
            },
            new ShopItem
            {
                Id = "double_jump",
                Name = "Pulo Duplo",
                Description = "Habilidade de pular duas vezes",
                Price = 500,
                Category = ShopCategory.Abilities,
                Rarity = ItemRarity.Rare,
                Effect = new() { Type = EffectType.DoubleJump }
            },
            new ShopItem
            {
                Id = "no_fall_damage",
                Name = "Botas de Queda",
                Description = "Sem dano de queda pela rodada",
                Price = 250,
                Category = ShopCategory.Abilities,
                Rarity = ItemRarity.Uncommon,
                Effect = new() { Type = EffectType.NoFallDamage }
            },

            // Special Items - T Only
            new ShopItem
            {
                Id = "freedom_pass",
                Name = "Passe de Liberdade",
                Description = "Freeday garantido para voce nesta rodada",
                Price = 1000,
                Category = ShopCategory.Special,
                Rarity = ItemRarity.Epic,
                TeamRestriction = TeamRestriction.TerroristOnly,
                MaxPurchasesPerMap = 1,
                Effect = new() { Type = EffectType.FreedomPass }
            },
            new ShopItem
            {
                Id = "rebel_pass",
                Name = "Passe de Rebelde",
                Description = "Imunidade de marcacao como rebelde por 15 segundos",
                Price = 600,
                Category = ShopCategory.Special,
                Rarity = ItemRarity.Rare,
                TeamRestriction = TeamRestriction.TerroristOnly,
                MaxPurchasesPerRound = 1,
                Effect = new() { Type = EffectType.RebelPass, Duration = 15 }
            },
            new ShopItem
            {
                Id = "respawn",
                Name = "Segunda Chance",
                Description = "Respawn automatico ao morrer (1x por mapa)",
                Price = 2000,
                Category = ShopCategory.Special,
                Rarity = ItemRarity.Legendary,
                MaxPurchasesPerMap = 1,
                Effect = new() { Type = EffectType.Respawn }
            },

            // Cosmetics
            new ShopItem
            {
                Id = "glow_red",
                Name = "Glow Vermelho",
                Description = "Aura vermelha ao redor do jogador",
                Price = 100,
                Category = ShopCategory.Cosmetics,
                Rarity = ItemRarity.Common,
                RequiresAlive = false,
                Effect = new() { Type = EffectType.Glow, CustomEffect = "red" }
            },
            new ShopItem
            {
                Id = "glow_blue",
                Name = "Glow Azul",
                Description = "Aura azul ao redor do jogador",
                Price = 100,
                Category = ShopCategory.Cosmetics,
                Rarity = ItemRarity.Common,
                RequiresAlive = false,
                Effect = new() { Type = EffectType.Glow, CustomEffect = "blue" }
            },
            new ShopItem
            {
                Id = "glow_rainbow",
                Name = "Glow Arco-Iris",
                Description = "Aura multicolorida ao redor do jogador",
                Price = 500,
                Category = ShopCategory.Cosmetics,
                Rarity = ItemRarity.Epic,
                RequiresAlive = false,
                Effect = new() { Type = EffectType.Glow, CustomEffect = "rainbow" }
            },
            new ShopItem
            {
                Id = "trail_fire",
                Name = "Trail de Fogo",
                Description = "Deixa um rastro de fogo ao andar",
                Price = 300,
                Category = ShopCategory.Cosmetics,
                Rarity = ItemRarity.Rare,
                Effect = new() { Type = EffectType.Trail, CustomEffect = "fire" }
            },
            new ShopItem
            {
                Id = "trail_ice",
                Name = "Trail de Gelo",
                Description = "Deixa um rastro de gelo ao andar",
                Price = 300,
                Category = ShopCategory.Cosmetics,
                Rarity = ItemRarity.Rare,
                Effect = new() { Type = EffectType.Trail, CustomEffect = "ice" }
            }
        };
    }
}
