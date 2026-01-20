using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using SuperJailbreak.Models;
using SuperJailbreak.Modules.Warden;
using SuperJailbreak.Modules.LastRequest;
using SuperJailbreak.Modules.SpecialDays;
using SuperJailbreak.Modules.Economy;
using SuperJailbreak.Modules.Gangs;
using SuperJailbreak.Modules.Achievements;
using SuperJailbreak.Services;

namespace SuperJailbreak.Core;

/// <summary>
/// Plugin Principal do Super Jailbreak para CS2
/// Versao: 1.0.0
/// Compativel com CounterStrikeSharp 1.0.355+
/// </summary>
[MinimumApiVersion(300)]
public partial class SuperJailbreakPlugin : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "Super Jailbreak";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "SuperJailbreak Team";
    public override string ModuleDescription => "Plugin revolucionario de Jailbreak para CS2";

    // Configuracao
    public PluginConfig Config { get; set; } = new();

    // Jogadores
    public Dictionary<int, JailPlayer> Players { get; private set; } = new();
    public const int MaxPlayers = 64;

    // Modulos
    public WardenModule? Warden { get; private set; }
    public LastRequestModule? LastRequest { get; private set; }
    public SpecialDaysModule? SpecialDays { get; private set; }
    public EconomyModule? Economy { get; private set; }
    public GangsModule? Gangs { get; private set; }
    public AchievementsModule? Achievements { get; private set; }

    // Services
    public DatabaseService? Database { get; private set; }
    public LocalizerService? Localizer { get; private set; }

    // Estado do jogo
    public bool IsWarmup { get; private set; }
    public bool IsFreezetime { get; private set; }
    public int RoundNumber { get; private set; }
    public int SpecialDaysUsedThisMap { get; private set; }

    // Timers
    private CounterStrikeSharp.API.Modules.Timers.Timer? _laserTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _cellsTimer;

    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
        Logger.LogInformation($"[SuperJailbreak] Configuracao carregada - Versao {config.Version}");
    }

    public override void Load(bool hotReload)
    {
        Logger.LogInformation("========================================");
        Logger.LogInformation("  Super Jailbreak Plugin v1.0.0");
        Logger.LogInformation("  Carregando modulos...");
        Logger.LogInformation("========================================");

        // Inicializar jogadores
        for (int i = 0; i < MaxPlayers; i++)
        {
            Players[i] = new JailPlayer { Slot = i };
        }

        // Inicializar servicos
        InitializeServices();

        // Inicializar modulos
        InitializeModules();

        // Registrar comandos
        RegisterCommands();

        // Registrar eventos
        RegisterEventHandlers();

        // Iniciar timers
        StartTimers();

        if (hotReload)
        {
            Logger.LogInformation("[SuperJailbreak] Hot reload detectado - sincronizando jogadores...");
            SyncPlayersOnHotReload();
        }

        Logger.LogInformation("[SuperJailbreak] Plugin carregado com sucesso!");
    }

    public override void Unload(bool hotReload)
    {
        Logger.LogInformation("[SuperJailbreak] Descarregando plugin...");

        // Parar timers
        _laserTimer?.Kill();
        _cellsTimer?.Kill();

        // Descarregar modulos
        Warden?.Unload();
        LastRequest?.Unload();
        SpecialDays?.Unload();
        Economy?.Unload();
        Gangs?.Unload();
        Achievements?.Unload();

        // Fechar conexao com banco de dados
        Database?.Dispose();

        Logger.LogInformation("[SuperJailbreak] Plugin descarregado!");
    }

    private void InitializeServices()
    {
        // Localizer
        Localizer = new LocalizerService(this, Config.Language);

        // Database
        if (Config.EnableDatabase)
        {
            Database = new DatabaseService(
                Config.DatabaseHost,
                Config.DatabasePort,
                Config.DatabaseName,
                Config.DatabaseUser,
                Config.DatabasePassword,
                Logger
            );
            Database.InitializeAsync().GetAwaiter().GetResult();
        }
    }

    private void InitializeModules()
    {
        // Warden
        if (Config.WardenEnabled)
        {
            Warden = new WardenModule(this);
            Warden.Initialize();
            Logger.LogInformation("[SuperJailbreak] Modulo Warden carregado");
        }

        // Last Request
        if (Config.LREnabled)
        {
            LastRequest = new LastRequestModule(this);
            LastRequest.Initialize();
            Logger.LogInformation("[SuperJailbreak] Modulo Last Request carregado");
        }

        // Special Days
        if (Config.SpecialDaysEnabled)
        {
            SpecialDays = new SpecialDaysModule(this);
            SpecialDays.Initialize();
            Logger.LogInformation("[SuperJailbreak] Modulo Special Days carregado");
        }

        // Economy
        if (Config.EconomyEnabled)
        {
            Economy = new EconomyModule(this);
            Economy.Initialize();
            Logger.LogInformation("[SuperJailbreak] Modulo Economy carregado");
        }

        // Gangs
        if (Config.GangsEnabled)
        {
            Gangs = new GangsModule(this);
            Gangs.Initialize();
            Logger.LogInformation("[SuperJailbreak] Modulo Gangs carregado");
        }

        // Achievements
        if (Config.AchievementsEnabled)
        {
            Achievements = new AchievementsModule(this);
            Achievements.Initialize();
            Logger.LogInformation("[SuperJailbreak] Modulo Achievements carregado");
        }
    }

    private void RegisterCommands()
    {
        // Os comandos sao registrados em cada modulo
        // Comandos gerais aqui
        AddCommand("css_jb", "Menu principal do Jailbreak", CommandJailbreakMenu);
        AddCommand("css_jailbreak", "Menu principal do Jailbreak", CommandJailbreakMenu);
        AddCommand("css_rules", "Ver regras do servidor", CommandRules);
        AddCommand("css_help", "Ajuda do Jailbreak", CommandHelp);
    }

    private void RegisterEventHandlers()
    {
        // Player Events
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);

        // Round Events
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);

        // Weapon Events
        RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
        RegisterEventHandler<EventItemPickup>(OnItemPickup);

        // Game Events
        RegisterEventHandler<EventWarmupEnd>(OnWarmupEnd);

        // Listeners
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
    }

    private void StartTimers()
    {
        // Timer para atualizar laser do warden
        if (Config.WardenLaserEnabled)
        {
            _laserTimer = AddTimer(0.1f, () => Warden?.UpdateLaser(), TimerFlags.REPEAT);
        }
    }

    private void SyncPlayersOnHotReload()
    {
        var players = Utilities.GetPlayers();
        foreach (var player in players)
        {
            if (player?.IsValid == true && !player.IsBot)
            {
                OnPlayerConnected(player);
            }
        }
    }

    #region Event Handlers

    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player?.IsValid != true || player.IsBot) return HookResult.Continue;

        OnPlayerConnected(player);
        return HookResult.Continue;
    }

    private void OnPlayerConnected(CCSPlayerController player)
    {
        var slot = player.Slot;
        Players[slot] = new JailPlayer
        {
            Slot = slot,
            SteamId = player.SteamID,
            Name = player.PlayerName,
            Controller = player,
            Credits = Config.StartingCredits
        };

        // Carregar dados do banco de dados
        if (Database != null)
        {
            Task.Run(async () =>
            {
                await Database.LoadPlayerAsync(Players[slot]);
            });
        }

        Logger.LogInformation($"[SuperJailbreak] Jogador conectado: {player.PlayerName} (Slot: {slot})");
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player?.IsValid != true) return HookResult.Continue;

        var slot = player.Slot;
        var jailPlayer = Players[slot];

        // Salvar dados no banco de dados
        if (Database != null && jailPlayer.SteamId != 0)
        {
            Task.Run(async () =>
            {
                await Database.SavePlayerAsync(jailPlayer);
            });
        }

        // Se era warden, remover
        Warden?.RemoveWardenIfPlayer(player);

        // Se estava em LR, cancelar
        LastRequest?.CancelLRForPlayer(player);

        // Reset do jogador
        Players[slot] = new JailPlayer { Slot = slot };

        return HookResult.Continue;
    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player?.IsValid != true) return HookResult.Continue;

        var jailPlayer = GetJailPlayer(player);
        if (jailPlayer == null) return HookResult.Continue;

        jailPlayer.IsAlive = true;
        jailPlayer.ResetRound();
        jailPlayer.Controller = player;

        // Aplicar configuracoes iniciais baseado no time
        AddTimer(0.1f, () =>
        {
            if (player?.IsValid != true) return;

            var team = player.Team;

            // Strip de armas para Ts
            if (team == CsTeam.Terrorist && !SpecialDays?.IsSpecialDayActive() == true)
            {
                player.RemoveWeapons();
                player.GiveNamedItem("weapon_knife");
            }

            // Aplicar glow para freeday players
            if (jailPlayer.IsFreedayPlayer)
            {
                // TODO: Aplicar glow verde
            }
        });

        return HookResult.Continue;
    }

    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var victim = @event.Userid;
        var attacker = @event.Attacker;

        if (victim?.IsValid != true) return HookResult.Continue;

        var jailVictim = GetJailPlayer(victim);
        if (jailVictim != null)
        {
            jailVictim.IsAlive = false;

            // Se era warden, remover
            if (jailVictim.IsWarden)
            {
                Warden?.OnWardenDeath(victim);
            }
        }

        // Processar kill para estatisticas
        if (attacker?.IsValid == true && attacker != victim)
        {
            var jailAttacker = GetJailPlayer(attacker);
            if (jailAttacker != null)
            {
                // Creditos por kill
                if (victim.Team != attacker.Team)
                {
                    int credits = Config.CreditsPerKill;
                    if (jailAttacker.IsRebel)
                    {
                        credits = Config.CreditsPerRebellionKill;
                        jailAttacker.GuardsKilled++;
                    }
                    Economy?.AddCredits(attacker, credits, "Kill");
                }

                // Achievements
                Achievements?.OnPlayerKill(attacker, victim, @event.Headshot, @event.Weapon);
            }
        }

        // Verificar LR
        LastRequest?.CheckForLR();

        return HookResult.Continue;
    }

    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var victim = @event.Userid;
        var attacker = @event.Attacker;

        if (victim?.IsValid != true || attacker?.IsValid != true) return HookResult.Continue;
        if (victim == attacker) return HookResult.Continue;

        // T atacou CT - marcar como rebelde
        if (attacker.Team == CsTeam.Terrorist && victim.Team == CsTeam.CounterTerrorist)
        {
            if (Config.RebelOnDamageToGuard && !LastRequest?.IsInLR(attacker) == true)
            {
                Warden?.SetRebel(attacker, true);
            }
        }

        return HookResult.Continue;
    }

    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player?.IsValid != true) return HookResult.Continue;

        // Se mudou de CT para outro time e era warden, remover
        if (@event.Oldteam == (int)CsTeam.CounterTerrorist)
        {
            Warden?.RemoveWardenIfPlayer(player);
        }

        return HookResult.Continue;
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        RoundNumber++;
        IsFreezetime = true;

        Logger.LogInformation($"[SuperJailbreak] Rodada {RoundNumber} iniciada");

        // Reset de jogadores
        foreach (var jailPlayer in Players.Values)
        {
            jailPlayer.ResetRound();
        }

        // Reset do warden
        Warden?.OnRoundStart();

        // Reset do LR
        LastRequest?.OnRoundStart();

        // Verificar Special Day
        SpecialDays?.OnRoundStart();

        // Creditos por rodada
        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid == true && !player.IsBot)
            {
                Economy?.AddCredits(player, Config.CreditsPerRound, "Round Start");
            }
        }

        // Timer para mutar Ts
        if (Config.MuteTsOnRoundStart)
        {
            MuteTeam(CsTeam.Terrorist, true);
            AddTimer(Config.MuteTsDurationSeconds, () => MuteTeam(CsTeam.Terrorist, false));
        }

        // Timer para abrir celas
        if (Config.OpenCellsOnRoundStart && Config.OpenCellsDelaySeconds > 0)
        {
            _cellsTimer = AddTimer(Config.OpenCellsDelaySeconds, () =>
            {
                if (Warden?.CurrentWarden == null)
                {
                    OpenCells();
                    PrintToChatAll($"{Config.PluginPrefix} Celas abertas automaticamente!");
                }
            });
        }

        return HookResult.Continue;
    }

    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        Logger.LogInformation($"[SuperJailbreak] Rodada {RoundNumber} finalizada");

        // Cancelar LRs ativos
        LastRequest?.OnRoundEnd();

        // Finalizar Special Day
        SpecialDays?.OnRoundEnd();

        // Estatisticas de warden
        if (Warden?.CurrentWarden != null)
        {
            var wardenPlayer = GetJailPlayer(Warden.CurrentWarden);
            if (wardenPlayer != null)
            {
                Economy?.AddCredits(Warden.CurrentWarden, Config.CreditsPerWardenRound, "Warden Round");
                wardenPlayer.TimesWarden++;
            }
        }

        // Cancelar timer de celas
        _cellsTimer?.Kill();
        _cellsTimer = null;

        return HookResult.Continue;
    }

    public HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
    {
        IsFreezetime = false;
        return HookResult.Continue;
    }

    public HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player?.IsValid != true) return HookResult.Continue;

        // T disparou arma (nao faca) - verificar se deve marcar como rebelde
        if (player.Team == CsTeam.Terrorist && @event.Weapon != "weapon_knife")
        {
            if (!LastRequest?.IsInLR(player) == true && !SpecialDays?.IsSpecialDayActive() == true)
            {
                Warden?.SetRebel(player, true);
            }
        }

        return HookResult.Continue;
    }

    public HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player?.IsValid != true) return HookResult.Continue;

        // T pegou arma - verificar se deve marcar como rebelde
        if (player.Team == CsTeam.Terrorist && Config.RebelOnWeaponPickup)
        {
            var item = @event.Item;
            if (item != "knife" && !LastRequest?.IsInLR(player) == true && !SpecialDays?.IsSpecialDayActive() == true)
            {
                var jailPlayer = GetJailPlayer(player);
                if (jailPlayer != null && !jailPlayer.IsFreedayPlayer)
                {
                    Warden?.SetRebel(player, true);
                }
            }
        }

        return HookResult.Continue;
    }

    public HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
    {
        IsWarmup = false;
        Logger.LogInformation("[SuperJailbreak] Warmup finalizado");
        return HookResult.Continue;
    }

    private void OnTick()
    {
        // Atualizacoes por tick podem ser feitas aqui se necessario
    }

    private void OnMapStart(string mapName)
    {
        Logger.LogInformation($"[SuperJailbreak] Mapa iniciado: {mapName}");
        RoundNumber = 0;
        SpecialDaysUsedThisMap = 0;
        IsWarmup = true;

        // Precache de sons
        PrecacheSounds();
    }

    private void OnMapEnd()
    {
        Logger.LogInformation("[SuperJailbreak] Mapa finalizado");

        // Salvar todos os jogadores
        if (Database != null)
        {
            foreach (var jailPlayer in Players.Values)
            {
                if (jailPlayer.SteamId != 0)
                {
                    Task.Run(async () =>
                    {
                        await Database.SavePlayerAsync(jailPlayer);
                    });
                }
            }
        }
    }

    #endregion

    #region Commands

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandJailbreakMenu(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        var menu = new ChatMenu("Super Jailbreak - Menu Principal");

        if (Config.WardenEnabled)
            menu.AddMenuOption("Warden", (p, o) => Warden?.ShowWardenMenu(p));

        if (Config.LREnabled)
            menu.AddMenuOption("Last Request", (p, o) => LastRequest?.ShowLRMenu(p));

        if (Config.SpecialDaysEnabled && player.Team == CsTeam.CounterTerrorist)
            menu.AddMenuOption("Special Days", (p, o) => SpecialDays?.ShowSpecialDayMenu(p));

        if (Config.EconomyEnabled)
            menu.AddMenuOption("Loja", (p, o) => Economy?.ShowShopMenu(p));

        if (Config.GangsEnabled)
            menu.AddMenuOption("Gangs", (p, o) => Gangs?.ShowGangMenu(p));

        if (Config.AchievementsEnabled)
            menu.AddMenuOption("Achievements", (p, o) => Achievements?.ShowAchievementsMenu(p));

        menu.AddMenuOption("Regras", (p, o) => ShowRules(p));
        menu.AddMenuOption("Estatisticas", (p, o) => ShowStats(p));

        MenuManager.OpenChatMenu(player, menu);
    }

    public void CommandRules(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        ShowRules(player);
    }

    public void CommandHelp(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        player.PrintToChat($" {ChatColors.Purple}=== Super Jailbreak - Comandos ===");
        player.PrintToChat($" {ChatColors.Green}!w / !warden{ChatColors.White} - Se tornar warden");
        player.PrintToChat($" {ChatColors.Green}!uw / !unwarden{ChatColors.White} - Deixar de ser warden");
        player.PrintToChat($" {ChatColors.Green}!lr{ChatColors.White} - Iniciar Last Request");
        player.PrintToChat($" {ChatColors.Green}!shop{ChatColors.White} - Abrir loja");
        player.PrintToChat($" {ChatColors.Green}!gang{ChatColors.White} - Menu de gangs");
        player.PrintToChat($" {ChatColors.Green}!credits{ChatColors.White} - Ver seus creditos");
        player.PrintToChat($" {ChatColors.Green}!stats{ChatColors.White} - Ver estatisticas");
        player.PrintToChat($" {ChatColors.Green}!rules{ChatColors.White} - Ver regras");
        player.PrintToChat($" {ChatColors.Purple}================================");
    }

    #endregion

    #region Utility Methods

    public JailPlayer? GetJailPlayer(CCSPlayerController? player)
    {
        if (player?.IsValid != true) return null;
        return Players.TryGetValue(player.Slot, out var jailPlayer) ? jailPlayer : null;
    }

    public JailPlayer? GetJailPlayer(int slot)
    {
        return Players.TryGetValue(slot, out var jailPlayer) ? jailPlayer : null;
    }

    public void PrintToChatAll(string message)
    {
        Server.PrintToChatAll($" {ChatColors.Purple}{Config.PluginPrefix}{ChatColors.White} {message}");
    }

    public void PrintToChat(CCSPlayerController player, string message)
    {
        player.PrintToChat($" {ChatColors.Purple}{Config.PluginPrefix}{ChatColors.White} {message}");
    }

    public void PrintToCenterAll(string message)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid == true)
            {
                player.PrintToCenter(message);
            }
        }
    }

    public void MuteTeam(CsTeam team, bool mute)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid == true && player.Team == team)
            {
                var jailPlayer = GetJailPlayer(player);
                if (jailPlayer != null)
                {
                    jailPlayer.IsMuted = mute;
                    // TODO: Implementar mute real
                }
            }
        }
    }

    public void OpenCells()
    {
        // Encontrar e ativar entidades de porta de cela
        var doors = Utilities.FindAllEntitiesByDesignerName<CBasePropDoor>("func_door");
        foreach (var door in doors)
        {
            if (door?.IsValid == true)
            {
                door.AcceptInput("Open");
            }
        }

        // Tambem tentar func_door_rotating
        var rotatingDoors = Utilities.FindAllEntitiesByDesignerName<CBasePropDoor>("func_door_rotating");
        foreach (var door in rotatingDoors)
        {
            if (door?.IsValid == true)
            {
                door.AcceptInput("Open");
            }
        }

        Logger.LogInformation("[SuperJailbreak] Celas abertas");
    }

    public void ShowRules(CCSPlayerController player)
    {
        player.PrintToChat($" {ChatColors.Purple}=== Regras do Jailbreak ===");
        player.PrintToChat($" {ChatColors.Yellow}1.{ChatColors.White} Siga as ordens do Warden");
        player.PrintToChat($" {ChatColors.Yellow}2.{ChatColors.White} Nao pegue armas sem permissao (sera marcado rebelde)");
        player.PrintToChat($" {ChatColors.Yellow}3.{ChatColors.White} Nao ataque guardas (sera marcado rebelde)");
        player.PrintToChat($" {ChatColors.Yellow}4.{ChatColors.White} LR disponivel quando restar poucos Ts");
        player.PrintToChat($" {ChatColors.Yellow}5.{ChatColors.White} Respeite os outros jogadores");
        player.PrintToChat($" {ChatColors.Purple}==========================");
    }

    public void ShowStats(CCSPlayerController player)
    {
        var jailPlayer = GetJailPlayer(player);
        if (jailPlayer == null) return;

        player.PrintToChat($" {ChatColors.Purple}=== Suas Estatisticas ===");
        player.PrintToChat($" {ChatColors.Green}Creditos:{ChatColors.White} {jailPlayer.Credits}");
        player.PrintToChat($" {ChatColors.Green}LR Wins:{ChatColors.White} {jailPlayer.LRWins} | LR Losses: {jailPlayer.LRLosses}");
        player.PrintToChat($" {ChatColors.Green}Vezes Warden:{ChatColors.White} {jailPlayer.TimesWarden}");
        player.PrintToChat($" {ChatColors.Green}Rodadas:{ChatColors.White} {jailPlayer.RoundsPlayed}");
        player.PrintToChat($" {ChatColors.Green}Achievements:{ChatColors.White} {jailPlayer.UnlockedAchievements.Count}");

        if (jailPlayer.GangName != null)
        {
            player.PrintToChat($" {ChatColors.Green}Gang:{ChatColors.White} {jailPlayer.GangName}");
        }

        player.PrintToChat($" {ChatColors.Purple}========================");
    }

    private void PrecacheSounds()
    {
        if (!Config.SoundsEnabled) return;

        // TODO: Precache sons personalizados
    }

    public int GetAliveCount(CsTeam team)
    {
        return Utilities.GetPlayers()
            .Count(p => p?.IsValid == true && p.PawnIsAlive && p.Team == team);
    }

    public List<CCSPlayerController> GetAlivePlayers(CsTeam team)
    {
        return Utilities.GetPlayers()
            .Where(p => p?.IsValid == true && p.PawnIsAlive && p.Team == team)
            .ToList();
    }

    #endregion
}
