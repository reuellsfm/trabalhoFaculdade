namespace SuperJailbreak.Models;

/// <summary>
/// Tipos de Special Days disponiveis
/// </summary>
public enum SpecialDayType
{
    None,
    Freeday,
    Warday,
    HideAndSeek,
    Zombie,
    GunGame,
    Dodgeball,
    HeadshotOnly,
    KnifeFight,
    NoScope,
    GravityFreeday,
    Hunger Games,
    TankDay,
    JuggernautDay,
    InfectedDay,
    BattleRoyale,
    TeamDeathmatch,
    OneInTheChamber,
    GoldenKnife,
    SimonSays,
    HotPotato,
    FreezeTag,
    SumoWrestling,
    ParkourRace,
    DeathRun
}

/// <summary>
/// Configuracao de um Special Day
/// </summary>
public class SpecialDayConfig
{
    public SpecialDayType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Rules { get; set; } = string.Empty;

    // Timers
    public int FreezeTime { get; set; } // Tempo que CTs ficam congelados (HnS)
    public int Duration { get; set; } // Duracao do evento
    public int ExpandTime { get; set; } // Tempo para expandir warday

    // Configuracoes de Jogo
    public bool FriendlyFire { get; set; }
    public bool InfiniteAmmo { get; set; }
    public bool NoFallDamage { get; set; }
    public float Gravity { get; set; } = 1.0f;
    public float Speed { get; set; } = 1.0f;
    public int Health { get; set; } = 100;
    public int Armor { get; set; }

    // Armas
    public List<string> AllowedWeapons { get; set; } = new();
    public bool StripWeapons { get; set; } = true;
    public bool GiveKnife { get; set; } = true;

    // Restricoes
    public bool CTsCanDie { get; set; } = true;
    public bool TsCanDie { get; set; } = true;
    public bool AllowLR { get; set; }
    public bool AllowRebel { get; set; }

    // Visual/Audio
    public string? AnnouncementSound { get; set; }
    public Color? CTColor { get; set; }
    public Color? TColor { get; set; }
}

/// <summary>
/// Configuracoes predefinidas de Special Days
/// </summary>
public static class SpecialDayConfigs
{
    public static Dictionary<SpecialDayType, SpecialDayConfig> GetAll()
    {
        return new Dictionary<SpecialDayType, SpecialDayConfig>
        {
            [SpecialDayType.Freeday] = new SpecialDayConfig
            {
                Type = SpecialDayType.Freeday,
                Name = "Freeday",
                Description = "Dia livre! Prisioneiros podem fazer o que quiserem.",
                Rules = "CTs nao podem matar Ts a menos que rebelem.",
                AllowRebel = true,
                AllowLR = true
            },

            [SpecialDayType.Warday] = new SpecialDayConfig
            {
                Type = SpecialDayType.Warday,
                Name = "Warday",
                Description = "Dia de guerra! CTs escolhem uma localizacao para se defender.",
                Rules = "CTs ficam em uma area. Ts devem atacar. Expande em 2:00.",
                FriendlyFire = false,
                AllowLR = false,
                ExpandTime = 120
            },

            [SpecialDayType.HideAndSeek] = new SpecialDayConfig
            {
                Type = SpecialDayType.HideAndSeek,
                Name = "Hide and Seek",
                Description = "Esconde-esconde! Ts se escondem, CTs procuram.",
                Rules = "CTs ficam cegos por 60s. Ts so podem usar faca.",
                FreezeTime = 60,
                StripWeapons = true,
                GiveKnife = true,
                CTsCanDie = false
            },

            [SpecialDayType.Zombie] = new SpecialDayConfig
            {
                Type = SpecialDayType.Zombie,
                Name = "Zombie Day",
                Description = "Apocalipse zumbi! Um CT vira zumbi e deve infectar todos.",
                Rules = "Zumbis so usam faca. Humanos tem armas. Ultimo humano vence.",
                Health = 2000,
                Speed = 1.3f,
                StripWeapons = true,
                GiveKnife = true,
                CTColor = Color.Green,
                TColor = Color.Yellow
            },

            [SpecialDayType.GunGame] = new SpecialDayConfig
            {
                Type = SpecialDayType.GunGame,
                Name = "Gun Game",
                Description = "Mata para avancar de arma! Primeiro a completar vence.",
                Rules = "Cada kill avanca sua arma. Ultimo nivel: faca.",
                InfiniteAmmo = true,
                FriendlyFire = true,
                AllowLR = false
            },

            [SpecialDayType.Dodgeball] = new SpecialDayConfig
            {
                Type = SpecialDayType.Dodgeball,
                Name = "Dodgeball",
                Description = "Queimada com decoys!",
                Rules = "Use decoys para eliminar inimigos. Ultimo time vence.",
                StripWeapons = true,
                AllowedWeapons = new List<string> { "weapon_decoy" },
                InfiniteAmmo = true
            },

            [SpecialDayType.HeadshotOnly] = new SpecialDayConfig
            {
                Type = SpecialDayType.HeadshotOnly,
                Name = "Headshot Only",
                Description = "Apenas headshots causam dano!",
                Rules = "Mire na cabeca ou nao causa dano.",
                AllowedWeapons = new List<string> { "weapon_deagle" },
                InfiniteAmmo = true
            },

            [SpecialDayType.KnifeFight] = new SpecialDayConfig
            {
                Type = SpecialDayType.KnifeFight,
                Name = "Knife Fight",
                Description = "Luta de facas! Todos contra todos.",
                Rules = "Apenas facas. Ultimo de pe vence.",
                StripWeapons = true,
                GiveKnife = true,
                FriendlyFire = true,
                Speed = 1.2f
            },

            [SpecialDayType.NoScope] = new SpecialDayConfig
            {
                Type = SpecialDayType.NoScope,
                Name = "No Scope",
                Description = "AWP sem mira!",
                Rules = "Use AWP mas nao pode usar scope.",
                AllowedWeapons = new List<string> { "weapon_awp" },
                InfiniteAmmo = true
            },

            [SpecialDayType.GravityFreeday] = new SpecialDayConfig
            {
                Type = SpecialDayType.GravityFreeday,
                Name = "Gravity Freeday",
                Description = "Freeday com gravidade baixa!",
                Rules = "Dia livre com pulos gigantes.",
                Gravity = 0.3f,
                AllowRebel = true,
                AllowLR = true
            },

            [SpecialDayType.BattleRoyale] = new SpecialDayConfig
            {
                Type = SpecialDayType.BattleRoyale,
                Name = "Battle Royale",
                Description = "Todos contra todos! Ultimo de pe vence.",
                Rules = "Colete armas pelo mapa. Zona de dano fecha com o tempo.",
                FriendlyFire = true,
                StripWeapons = true,
                AllowLR = false
            },

            [SpecialDayType.OneInTheChamber] = new SpecialDayConfig
            {
                Type = SpecialDayType.OneInTheChamber,
                Name = "One in the Chamber",
                Description = "Uma bala, uma chance! Mate para ganhar municao.",
                Rules = "Comeca com 1 bala. Cada kill da +1 bala.",
                AllowedWeapons = new List<string> { "weapon_deagle" },
                FriendlyFire = true,
                GiveKnife = true
            },

            [SpecialDayType.GoldenKnife] = new SpecialDayConfig
            {
                Type = SpecialDayType.GoldenKnife,
                Name = "Golden Knife",
                Description = "Um jogador comeca com a faca dourada. Mate-o para pega-la!",
                Rules = "So a faca dourada mata em 1 hit. Quem tem ela no fim vence.",
                StripWeapons = true,
                GiveKnife = true,
                FriendlyFire = true
            },

            [SpecialDayType.SimonSays] = new SpecialDayConfig
            {
                Type = SpecialDayType.SimonSays,
                Name = "Simon Says",
                Description = "Simon diz... siga as ordens!",
                Rules = "Warden da ordens. So obedeca se comecar com 'Simon Says'.",
                AllowRebel = false,
                AllowLR = true
            },

            [SpecialDayType.HotPotato] = new SpecialDayConfig
            {
                Type = SpecialDayType.HotPotato,
                Name = "Hot Potato",
                Description = "Passe a batata quente! Quem segurar quando explodir morre.",
                Rules = "Esfaqueie alguem para passar a batata. Timer aleatorio.",
                StripWeapons = true,
                GiveKnife = true,
                FriendlyFire = true
            },

            [SpecialDayType.FreezeTag] = new SpecialDayConfig
            {
                Type = SpecialDayType.FreezeTag,
                Name = "Freeze Tag",
                Description = "Pique-congela! CTs congelam, Ts descongelam aliados.",
                Rules = "CTs congelam Ts com faca. Ts podem descongelar aliados.",
                StripWeapons = true,
                GiveKnife = true,
                Duration = 180
            },

            [SpecialDayType.SumoWrestling] = new SpecialDayConfig
            {
                Type = SpecialDayType.SumoWrestling,
                Name = "Sumo Wrestling",
                Description = "Sumo! Empurre seus oponentes para fora da arena.",
                Rules = "Use faca para empurrar. Cair da arena = morte.",
                StripWeapons = true,
                GiveKnife = true,
                NoFallDamage = false,
                FriendlyFire = true
            },

            [SpecialDayType.DeathRun] = new SpecialDayConfig
            {
                Type = SpecialDayType.DeathRun,
                Name = "Death Run",
                Description = "Corrida da morte! CTs ativam armadilhas, Ts tentam sobreviver.",
                Rules = "Ts devem chegar ao fim. CTs controlam as armadilhas.",
                StripWeapons = true,
                GiveKnife = true,
                CTsCanDie = false
            }
        };
    }
}
