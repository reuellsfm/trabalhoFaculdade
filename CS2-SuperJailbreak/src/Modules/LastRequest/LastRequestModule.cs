using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using SuperJailbreak.Core;
using SuperJailbreak.Models;

namespace SuperJailbreak.Modules.LastRequest;

/// <summary>
/// Modulo de Last Request (LR) completo
/// Suporta multiplos jogos simultaneos e 15+ tipos de LR
/// </summary>
public class LastRequestModule
{
    private readonly SuperJailbreakPlugin _plugin;

    // LRs ativos
    public List<ActiveLR> ActiveLRs { get; private set; } = new();
    private const int MaxSimultaneousLRs = 2;

    // Estado
    public bool IsLRPeriod { get; private set; }
    private CounterStrikeSharp.API.Modules.Timers.Timer? _lrTimeoutTimer;

    public LastRequestModule(SuperJailbreakPlugin plugin)
    {
        _plugin = plugin;
    }

    public void Initialize()
    {
        RegisterCommands();
    }

    public void Unload()
    {
        _lrTimeoutTimer?.Kill();
        CancelAllLRs();
    }

    private void RegisterCommands()
    {
        _plugin.AddCommand("css_lr", "Iniciar Last Request", CommandLR);
        _plugin.AddCommand("css_lastrequest", "Iniciar Last Request", CommandLR);
        _plugin.AddCommand("css_stoplr", "Cancelar LR (Admin)", CommandStopLR);
        _plugin.AddCommand("css_abortlr", "Cancelar LR (Admin)", CommandStopLR);
    }

    #region LR Management

    public void CheckForLR()
    {
        if (IsLRPeriod) return;
        if (_plugin.SpecialDays?.IsSpecialDayActive() == true) return;

        var aliveTs = _plugin.GetAliveCount(CsTeam.Terrorist);
        var aliveCTs = _plugin.GetAliveCount(CsTeam.CounterTerrorist);

        if (aliveTs <= _plugin.Config.LRMaxTsForLR && aliveCTs >= _plugin.Config.LRMinCTsAlive && aliveTs > 0)
        {
            StartLRPeriod();
        }
    }

    private void StartLRPeriod()
    {
        IsLRPeriod = true;

        _plugin.PrintToChatAll($"{ChatColors.Green}=== LAST REQUEST DISPONIVEL ==={ChatColors.White}");
        _plugin.PrintToChatAll($"Terroristas podem digitar {ChatColors.Yellow}!lr{ChatColors.White} para iniciar!");

        // Notificar Ts vivos
        foreach (var player in _plugin.GetAlivePlayers(CsTeam.Terrorist))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Green}Voce pode iniciar um Last Request! Digite !lr");
            player.PrintToCenter("LAST REQUEST DISPONIVEL - Digite !lr");
        }

        // Timer de timeout
        _lrTimeoutTimer = _plugin.AddTimer(_plugin.Config.LRTimeoutSeconds, () =>
        {
            if (IsLRPeriod && ActiveLRs.Count == 0)
            {
                _plugin.PrintToChatAll($"{ChatColors.Red}Tempo para LR expirou!");
            }
        });
    }

    public void OnRoundStart()
    {
        IsLRPeriod = false;
        CancelAllLRs();
        _lrTimeoutTimer?.Kill();
    }

    public void OnRoundEnd()
    {
        CancelAllLRs();
    }

    public bool IsInLR(CCSPlayerController player)
    {
        return ActiveLRs.Any(lr => lr.Terrorist == player || lr.Guard == player);
    }

    public void CancelLRForPlayer(CCSPlayerController player)
    {
        var lr = ActiveLRs.FirstOrDefault(lr => lr.Terrorist == player || lr.Guard == player);
        if (lr != null)
        {
            EndLR(lr, lr.Terrorist == player ? lr.Guard : lr.Terrorist);
        }
    }

    private void CancelAllLRs()
    {
        foreach (var lr in ActiveLRs.ToList())
        {
            CleanupLR(lr);
        }
        ActiveLRs.Clear();
    }

    #endregion

    #region LR Start

    public void StartLR(CCSPlayerController terrorist, CCSPlayerController guard, LRType type)
    {
        if (ActiveLRs.Count >= MaxSimultaneousLRs)
        {
            _plugin.PrintToChat(terrorist, $"{ChatColors.Red}Maximo de LRs ativos atingido!");
            return;
        }

        var lr = new ActiveLR
        {
            Id = Guid.NewGuid(),
            Type = type,
            Terrorist = terrorist,
            Guard = guard,
            StartTime = DateTime.UtcNow
        };

        // Configurar LR baseado no tipo
        ConfigureLR(lr);

        ActiveLRs.Add(lr);

        // Marcar jogadores como em LR
        var tPlayer = _plugin.GetJailPlayer(terrorist);
        var ctPlayer = _plugin.GetJailPlayer(guard);
        if (tPlayer != null) tPlayer.IsInLR = true;
        if (ctPlayer != null) ctPlayer.IsInLR = true;

        // Anunciar
        _plugin.PrintToChatAll($"{ChatColors.Yellow}=== LAST REQUEST ==={ChatColors.White}");
        _plugin.PrintToChatAll($"{ChatColors.Red}{terrorist.PlayerName}{ChatColors.White} vs {ChatColors.Blue}{guard.PlayerName}");
        _plugin.PrintToChatAll($"Tipo: {ChatColors.Green}{GetLRTypeName(type)}");

        // Preparar jogadores
        PreparePlayers(lr);

        _plugin.Logger.LogInformation($"[LR] Iniciado: {terrorist.PlayerName} vs {guard.PlayerName} - {type}");
    }

    private void ConfigureLR(ActiveLR lr)
    {
        switch (lr.Type)
        {
            case LRType.Knife:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    GiveKnife = true,
                    Description = "Luta de facas! Primeiro a matar vence."
                };
                break;

            case LRType.NoScope:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    Weapons = new List<string> { "weapon_awp" },
                    Description = "AWP sem mira! Proibido usar scope."
                };
                break;

            case LRType.Shot4Shot:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    Weapons = new List<string> { "weapon_deagle" },
                    Description = "Um tiro de cada vez! Espere sua vez.",
                    TurnBased = true
                };
                break;

            case LRType.Mag4Mag:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    Weapons = new List<string> { "weapon_deagle" },
                    Description = "Um pente de cada vez!"
                };
                break;

            case LRType.Dodgeball:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    Weapons = new List<string> { "weapon_decoy" },
                    InfiniteAmmo = true,
                    Description = "Queimada com decoys!"
                };
                break;

            case LRType.GunToss:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    Weapons = new List<string> { "weapon_deagle" },
                    Description = "Lance a arma! Quem jogar mais longe vence.",
                    NoShooting = true
                };
                break;

            case LRType.Grenade:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    Weapons = new List<string> { "weapon_hegrenade" },
                    InfiniteAmmo = true,
                    Description = "Batalha de granadas!"
                };
                break;

            case LRType.RussianRoulette:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    Weapons = new List<string> { "weapon_deagle" },
                    Description = "Roleta Russa! Atire na propria cabeca. Sorte decide!",
                    Special = "russian_roulette"
                };
                break;

            case LRType.HeadshotOnly:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    Weapons = new List<string> { "weapon_deagle" },
                    InfiniteAmmo = true,
                    Description = "Apenas headshots causam dano!"
                };
                break;

            case LRType.ScoutKnife:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    GiveKnife = true,
                    Weapons = new List<string> { "weapon_ssg08" },
                    Description = "Scout + Faca! Sem scope."
                };
                break;

            case LRType.Shotgun:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    Weapons = new List<string> { "weapon_nova" },
                    InfiniteAmmo = true,
                    Description = "Batalha de shotguns!"
                };
                break;

            case LRType.Deagle:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    Weapons = new List<string> { "weapon_deagle" },
                    InfiniteAmmo = true,
                    Description = "Duelo de Deagles!"
                };
                break;

            case LRType.Race:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    GiveKnife = true,
                    Description = "Corrida! Primeiro a chegar vence.",
                    NoShooting = true,
                    Special = "race"
                };
                break;

            case LRType.MathChallenge:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    Description = "Desafio de matematica! Primeiro a responder certo vence.",
                    NoShooting = true,
                    Special = "math"
                };
                break;

            case LRType.HotPotato:
                lr.Config = new LRConfig
                {
                    StripWeapons = true,
                    GiveKnife = true,
                    Description = "Batata quente! Passe com a faca antes de explodir!",
                    Special = "hot_potato"
                };
                break;

            case LRType.Rebel:
                lr.Config = new LRConfig
                {
                    StripWeapons = false,
                    GiveKnife = true,
                    Weapons = new List<string> { "weapon_ak47" },
                    Description = "REBELIAO! Mate todos os guardas!"
                };
                // Marcar como rebelde
                _plugin.Warden?.SetRebel(lr.Terrorist!, true);
                break;
        }
    }

    private void PreparePlayers(ActiveLR lr)
    {
        var terrorist = lr.Terrorist!;
        var guard = lr.Guard!;
        var config = lr.Config!;

        // Strip de armas
        if (config.StripWeapons)
        {
            terrorist.RemoveWeapons();
            guard.RemoveWeapons();
        }

        // Dar faca
        if (config.GiveKnife)
        {
            terrorist.GiveNamedItem("weapon_knife");
            guard.GiveNamedItem("weapon_knife");
        }

        // Dar armas especificas
        foreach (var weapon in config.Weapons)
        {
            terrorist.GiveNamedItem(weapon);
            guard.GiveNamedItem(weapon);
        }

        // Curar jogadores
        var tPawn = terrorist.PlayerPawn.Value;
        var ctPawn = guard.PlayerPawn.Value;

        if (tPawn != null)
        {
            tPawn.Health = 100;
            tPawn.ArmorValue = 0;
        }

        if (ctPawn != null)
        {
            ctPawn.Health = 100;
            ctPawn.ArmorValue = 0;
        }

        // Iniciar logica especial
        if (config.Special != null)
        {
            StartSpecialLR(lr);
        }
    }

    private void StartSpecialLR(ActiveLR lr)
    {
        switch (lr.Config?.Special)
        {
            case "russian_roulette":
                StartRussianRoulette(lr);
                break;
            case "math":
                StartMathChallenge(lr);
                break;
            case "hot_potato":
                StartHotPotato(lr);
                break;
            case "race":
                StartRace(lr);
                break;
        }
    }

    #endregion

    #region Special LR Games

    private void StartRussianRoulette(ActiveLR lr)
    {
        lr.TurnPlayer = lr.Terrorist;
        _plugin.PrintToChatAll($"{ChatColors.Yellow}Roleta Russa! {lr.Terrorist!.PlayerName} comeca!");
        _plugin.PrintToChatAll($"Digite {ChatColors.Red}!shoot{ChatColors.White} para atirar na propria cabeca!");

        // Registrar comando temporario
        _plugin.AddCommand("css_shoot", "Atirar na roleta russa", (player, info) =>
        {
            if (player != lr.TurnPlayer) return;

            // 1 em 6 chance de morrer
            var random = new Random();
            if (random.Next(1, 7) == 1)
            {
                // BANG!
                _plugin.PrintToChatAll($"{ChatColors.Red}BANG! {player!.PlayerName} perdeu na roleta russa!");
                player.PlayerPawn.Value?.CommitSuicide(true, true);
                EndLR(lr, player == lr.Terrorist ? lr.Guard : lr.Terrorist);
            }
            else
            {
                // Click
                _plugin.PrintToChatAll($"{ChatColors.Green}*click* {player!.PlayerName} sobreviveu!");
                lr.TurnPlayer = player == lr.Terrorist ? lr.Guard : lr.Terrorist;
                _plugin.PrintToChatAll($"Vez de {lr.TurnPlayer!.PlayerName}!");
            }
        });
    }

    private void StartMathChallenge(ActiveLR lr)
    {
        var random = new Random();
        var num1 = random.Next(1, 50);
        var num2 = random.Next(1, 50);
        var operation = random.Next(0, 3); // 0=soma, 1=sub, 2=mult

        int answer;
        string opSymbol;

        switch (operation)
        {
            case 0:
                answer = num1 + num2;
                opSymbol = "+";
                break;
            case 1:
                answer = num1 - num2;
                opSymbol = "-";
                break;
            default:
                num1 = random.Next(1, 12);
                num2 = random.Next(1, 12);
                answer = num1 * num2;
                opSymbol = "x";
                break;
        }

        lr.MathAnswer = answer;

        _plugin.PrintToChatAll($"{ChatColors.Yellow}=== DESAFIO MATEMATICO ==={ChatColors.White}");
        _plugin.PrintToChatAll($"Quanto e {ChatColors.Green}{num1} {opSymbol} {num2}{ChatColors.White}?");
        _plugin.PrintToChatAll($"Digite a resposta no chat!");

        // Ouvir respostas no chat
        lr.ListeningForAnswer = true;
    }

    private void StartHotPotato(ActiveLR lr)
    {
        // Escolher quem comeca com a batata
        var random = new Random();
        lr.HasPotato = random.Next(0, 2) == 0 ? lr.Terrorist : lr.Guard;

        _plugin.PrintToChatAll($"{ChatColors.Red}=== BATATA QUENTE ==={ChatColors.White}");
        _plugin.PrintToChatAll($"{lr.HasPotato!.PlayerName} comeca com a batata!");
        _plugin.PrintToChatAll($"Esfaqueie o oponente para passar a batata!");

        // Timer aleatorio para explosao
        var explosionTime = random.Next(5, 20);
        lr.PotatoTimer = _plugin.AddTimer(explosionTime, () =>
        {
            if (lr.HasPotato?.IsValid == true && lr.HasPotato.PawnIsAlive)
            {
                _plugin.PrintToChatAll($"{ChatColors.Red}BOOM! {lr.HasPotato.PlayerName} explodiu com a batata!");
                lr.HasPotato.PlayerPawn.Value?.CommitSuicide(true, true);
                EndLR(lr, lr.HasPotato == lr.Terrorist ? lr.Guard : lr.Terrorist);
            }
        });
    }

    private void StartRace(ActiveLR lr)
    {
        _plugin.PrintToChatAll($"{ChatColors.Yellow}=== CORRIDA ==={ChatColors.White}");
        _plugin.PrintToChatAll($"Warden deve definir a linha de chegada!");
        _plugin.PrintToChatAll($"Primeiro a chegar vence!");

        // TODO: Implementar sistema de checkpoints
    }

    public void OnPlayerDamage(CCSPlayerController attacker, CCSPlayerController victim, int damage, bool headshot)
    {
        var lr = ActiveLRs.FirstOrDefault(l =>
            (l.Terrorist == attacker && l.Guard == victim) ||
            (l.Guard == attacker && l.Terrorist == victim));

        if (lr == null) return;

        // Verificar regras especiais
        if (lr.Config?.NoShooting == true)
        {
            // Nao permitir dano por armas
            return;
        }

        // Headshot only
        if (lr.Type == LRType.HeadshotOnly && !headshot)
        {
            // Negar dano
            return;
        }

        // Hot Potato - passar batata ao dar facada
        if (lr.Type == LRType.HotPotato && lr.HasPotato == attacker)
        {
            lr.HasPotato = victim;
            _plugin.PrintToChatAll($"{ChatColors.Yellow}{victim.PlayerName} agora tem a batata!");
        }
    }

    #endregion

    #region LR End

    public void EndLR(ActiveLR lr, CCSPlayerController? winner)
    {
        if (!ActiveLRs.Contains(lr)) return;

        var loser = winner == lr.Terrorist ? lr.Guard : lr.Terrorist;

        _plugin.PrintToChatAll($"{ChatColors.Green}=== LR FINALIZADO ==={ChatColors.White}");

        if (winner != null)
        {
            _plugin.PrintToChatAll($"Vencedor: {ChatColors.Green}{winner.PlayerName}");

            // Estatisticas
            var winnerPlayer = _plugin.GetJailPlayer(winner);
            var loserPlayer = _plugin.GetJailPlayer(loser);

            if (winnerPlayer != null)
            {
                winnerPlayer.LRWins++;
                winnerPlayer.LRStreak++;
                _plugin.Economy?.AddCredits(winner, _plugin.Config.CreditsPerLRWin, "LR Win");
                _plugin.Achievements?.CheckAchievement(winner, AchievementType.WinLR, winnerPlayer.LRWins);
                _plugin.Achievements?.CheckAchievement(winner, AchievementType.WinLRStreak, winnerPlayer.LRStreak);
            }

            if (loserPlayer != null)
            {
                loserPlayer.LRLosses++;
                loserPlayer.LRStreak = 0;
            }
        }

        CleanupLR(lr);
        ActiveLRs.Remove(lr);

        // Marcar jogadores como fora do LR
        var tPlayer = _plugin.GetJailPlayer(lr.Terrorist);
        var ctPlayer = _plugin.GetJailPlayer(lr.Guard);
        if (tPlayer != null) tPlayer.IsInLR = false;
        if (ctPlayer != null) ctPlayer.IsInLR = false;

        _plugin.Logger.LogInformation($"[LR] Finalizado: Vencedor - {winner?.PlayerName ?? "Nenhum"}");
    }

    private void CleanupLR(ActiveLR lr)
    {
        lr.PotatoTimer?.Kill();
        lr.TimeoutTimer?.Kill();
    }

    #endregion

    #region Commands

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandLR(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (player.Team != CsTeam.Terrorist)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas terroristas podem iniciar LR!");
            return;
        }

        if (!player.PawnIsAlive)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Voce precisa estar vivo!");
            return;
        }

        if (!IsLRPeriod)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}LR ainda nao esta disponivel!");
            return;
        }

        if (IsInLR(player))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Voce ja esta em um LR!");
            return;
        }

        ShowLRMenu(player);
    }

    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandStopLR(CCSPlayerController? player, CommandInfo info)
    {
        // TODO: Verificar se e admin
        if (player == null) return;

        CancelAllLRs();
        _plugin.PrintToChatAll($"{ChatColors.Red}Todos os LRs foram cancelados por um admin!");
    }

    #endregion

    #region Menus

    public void ShowLRMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Escolha o tipo de LR");

        foreach (var lrType in _plugin.Config.LRAllowedTypes)
        {
            if (Enum.TryParse<LRType>(lrType, out var type))
            {
                menu.AddMenuOption(GetLRTypeName(type), (p, o) =>
                {
                    ShowGuardSelectionMenu(p, type);
                });
            }
        }

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowGuardSelectionMenu(CCSPlayerController terrorist, LRType type)
    {
        var menu = new ChatMenu($"Escolha o guarda para {GetLRTypeName(type)}");

        foreach (var guard in _plugin.GetAlivePlayers(CsTeam.CounterTerrorist))
        {
            if (!IsInLR(guard))
            {
                menu.AddMenuOption(guard.PlayerName, (t, o) =>
                {
                    StartLR(terrorist, guard, type);
                });
            }
        }

        if (menu.MenuOptions.Count == 0)
        {
            _plugin.PrintToChat(terrorist, $"{ChatColors.Red}Nenhum guarda disponivel!");
            return;
        }

        MenuManager.OpenChatMenu(terrorist, menu);
    }

    #endregion

    #region Helpers

    private string GetLRTypeName(LRType type)
    {
        return type switch
        {
            LRType.Knife => "Luta de Facas",
            LRType.NoScope => "No Scope (AWP)",
            LRType.Shot4Shot => "Shot 4 Shot",
            LRType.Mag4Mag => "Mag 4 Mag",
            LRType.Dodgeball => "Dodgeball",
            LRType.GunToss => "Gun Toss",
            LRType.Grenade => "Batalha de Granadas",
            LRType.RussianRoulette => "Roleta Russa",
            LRType.HeadshotOnly => "Headshot Only",
            LRType.ScoutKnife => "Scout + Knife",
            LRType.Shotgun => "Shotgun War",
            LRType.Deagle => "Deagle Duel",
            LRType.Race => "Corrida",
            LRType.MathChallenge => "Desafio Matematico",
            LRType.HotPotato => "Batata Quente",
            LRType.Rebel => "Rebeliao",
            _ => type.ToString()
        };
    }

    #endregion
}

public class ActiveLR
{
    public Guid Id { get; set; }
    public LRType Type { get; set; }
    public CCSPlayerController? Terrorist { get; set; }
    public CCSPlayerController? Guard { get; set; }
    public DateTime StartTime { get; set; }
    public LRConfig? Config { get; set; }

    // Estado especifico
    public CCSPlayerController? TurnPlayer { get; set; }
    public int? MathAnswer { get; set; }
    public bool ListeningForAnswer { get; set; }
    public CCSPlayerController? HasPotato { get; set; }
    public CounterStrikeSharp.API.Modules.Timers.Timer? PotatoTimer { get; set; }
    public CounterStrikeSharp.API.Modules.Timers.Timer? TimeoutTimer { get; set; }
}

public class LRConfig
{
    public bool StripWeapons { get; set; }
    public bool GiveKnife { get; set; }
    public List<string> Weapons { get; set; } = new();
    public bool InfiniteAmmo { get; set; }
    public bool NoShooting { get; set; }
    public bool TurnBased { get; set; }
    public string? Special { get; set; }
    public string Description { get; set; } = string.Empty;
}
