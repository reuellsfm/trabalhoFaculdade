using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using SuperJailbreak.Core;
using SuperJailbreak.Models;
using System.Drawing;

namespace SuperJailbreak.Modules.SpecialDays;

/// <summary>
/// Modulo de Special Days completo
/// Inclui: Freeday, Warday, Hide and Seek, Zombie, Gun Game, Battle Royale, etc.
/// </summary>
public class SpecialDaysModule
{
    private readonly SuperJailbreakPlugin _plugin;

    // Estado atual
    public SpecialDayType CurrentSpecialDay { get; private set; } = SpecialDayType.None;
    public SpecialDayConfig? CurrentConfig { get; private set; }
    public CCSPlayerController? Initiator { get; private set; }
    public DateTime? StartTime { get; private set; }

    // Timers
    private CounterStrikeSharp.API.Modules.Timers.Timer? _freezeTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _expandTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _durationTimer;

    // Gun Game
    private Dictionary<int, int> _gunGameLevels = new();
    private static readonly List<string> GunGameWeapons = new()
    {
        "weapon_glock", "weapon_usp_silencer", "weapon_p250", "weapon_fiveseven",
        "weapon_deagle", "weapon_mac10", "weapon_mp9", "weapon_ump45",
        "weapon_p90", "weapon_famas", "weapon_galilar", "weapon_m4a1",
        "weapon_ak47", "weapon_sg556", "weapon_aug", "weapon_awp",
        "weapon_ssg08", "weapon_nova", "weapon_xm1014", "weapon_knife"
    };

    // Zombie
    private List<CCSPlayerController> _zombies = new();
    private List<CCSPlayerController> _humans = new();

    // Battle Royale
    private float _zoneRadius = 5000f;
    private Vector? _zoneCenter;

    // One in the Chamber
    private Dictionary<int, int> _chamberAmmo = new();

    // Hide and Seek blindness
    private HashSet<int> _blindedPlayers = new();
    private CounterStrikeSharp.API.Modules.Timers.Timer? _blindTimer;

    public SpecialDaysModule(SuperJailbreakPlugin plugin)
    {
        _plugin = plugin;
    }

    public void Initialize()
    {
        RegisterCommands();
    }

    public void Unload()
    {
        EndSpecialDay();
    }

    private void RegisterCommands()
    {
        _plugin.AddCommand("css_sd", "Menu de Special Days", CommandSpecialDay);
        _plugin.AddCommand("css_specialday", "Menu de Special Days", CommandSpecialDay);
        _plugin.AddCommand("css_fd", "Iniciar Freeday", CommandFreeday);
        _plugin.AddCommand("css_freeday", "Iniciar Freeday", CommandFreeday);
        _plugin.AddCommand("css_hns", "Iniciar Hide and Seek", CommandHideAndSeek);
        _plugin.AddCommand("css_zombie", "Iniciar Zombie Day", CommandZombie);
        _plugin.AddCommand("css_gg", "Iniciar Gun Game", CommandGunGame);
        _plugin.AddCommand("css_br", "Iniciar Battle Royale", CommandBattleRoyale);
    }

    #region Special Day Management

    public bool IsSpecialDayActive() => CurrentSpecialDay != SpecialDayType.None;

    public void StartSpecialDay(SpecialDayType type, CCSPlayerController? initiator = null)
    {
        if (IsSpecialDayActive())
        {
            if (initiator != null)
                _plugin.PrintToChat(initiator, $"{ChatColors.Red}Ja existe um Special Day ativo!");
            return;
        }

        if (_plugin.SpecialDaysUsedThisMap >= _plugin.Config.MaxSpecialDaysPerMap)
        {
            if (initiator != null)
                _plugin.PrintToChat(initiator, $"{ChatColors.Red}Limite de Special Days atingido neste mapa!");
            return;
        }

        var configs = SpecialDayConfigs.GetAll();
        if (!configs.TryGetValue(type, out var config))
        {
            _plugin.Logger.LogError($"[SpecialDays] Configuracao nao encontrada para {type}");
            return;
        }

        CurrentSpecialDay = type;
        CurrentConfig = config;
        Initiator = initiator;
        StartTime = DateTime.UtcNow;
        _plugin.SpecialDaysUsedThisMap++;

        // Anunciar
        _plugin.PrintToChatAll($"{ChatColors.Purple}=== SPECIAL DAY ==={ChatColors.White}");
        _plugin.PrintToChatAll($"Tipo: {ChatColors.Green}{config.Name}");
        _plugin.PrintToChatAll($"Descricao: {config.Description}");
        _plugin.PrintToChatAll($"Regras: {ChatColors.Yellow}{config.Rules}");
        _plugin.PrintToCenterAll($"SPECIAL DAY: {config.Name}");

        // Aplicar configuracoes
        ApplySpecialDayConfig(config);

        // Iniciar logica especifica
        switch (type)
        {
            case SpecialDayType.Freeday:
                StartFreeday();
                break;
            case SpecialDayType.GravityFreeday:
                StartGravityFreeday();
                break;
            case SpecialDayType.Warday:
                StartWarday();
                break;
            case SpecialDayType.HideAndSeek:
                StartHideAndSeek();
                break;
            case SpecialDayType.Zombie:
                StartZombie();
                break;
            case SpecialDayType.GunGame:
                StartGunGame();
                break;
            case SpecialDayType.Dodgeball:
                StartDodgeball();
                break;
            case SpecialDayType.HeadshotOnly:
                StartHeadshotOnly();
                break;
            case SpecialDayType.KnifeFight:
                StartKnifeFight();
                break;
            case SpecialDayType.NoScope:
                StartNoScope();
                break;
            case SpecialDayType.BattleRoyale:
                StartBattleRoyale();
                break;
            case SpecialDayType.OneInTheChamber:
                StartOneInTheChamber();
                break;
            case SpecialDayType.GoldenKnife:
                StartGoldenKnife();
                break;
            case SpecialDayType.SimonSays:
                StartSimonSays();
                break;
            case SpecialDayType.HotPotato:
                StartHotPotato();
                break;
            case SpecialDayType.FreezeTag:
                StartFreezeTag();
                break;
            case SpecialDayType.SumoWrestling:
                StartSumoWrestling();
                break;
            case SpecialDayType.DeathRun:
                StartDeathRun();
                break;
        }

        _plugin.Logger.LogInformation($"[SpecialDays] Iniciado: {type} por {initiator?.PlayerName ?? "Sistema"}");
    }

    public void OnRoundStart()
    {
        // Se o primeiro round, dar freeday
        if (_plugin.RoundNumber == 1)
        {
            _plugin.AddTimer(1.0f, () => StartSpecialDay(SpecialDayType.Freeday));
        }
    }

    public void OnRoundEnd()
    {
        EndSpecialDay();
    }

    public void EndSpecialDay()
    {
        if (!IsSpecialDayActive()) return;

        _plugin.PrintToChatAll($"{ChatColors.Purple}Special Day {CurrentConfig?.Name} finalizado!{ChatColors.White}");

        // Limpar timers
        _freezeTimer?.Kill();
        _expandTimer?.Kill();
        _durationTimer?.Kill();

        // Reset de variaveis
        _gunGameLevels.Clear();
        _zombies.Clear();
        _humans.Clear();
        _chamberAmmo.Clear();

        // Reset de convars
        Server.ExecuteCommand("sv_gravity 800");
        Server.ExecuteCommand("mp_friendlyfire 0");
        Server.ExecuteCommand("sv_infinite_ammo 0");

        CurrentSpecialDay = SpecialDayType.None;
        CurrentConfig = null;
        Initiator = null;
        StartTime = null;

        _plugin.Logger.LogInformation("[SpecialDays] Special Day finalizado");
    }

    private void ApplySpecialDayConfig(SpecialDayConfig config)
    {
        // Gravidade
        if (config.Gravity != 1.0f)
        {
            Server.ExecuteCommand($"sv_gravity {(int)(800 * config.Gravity)}");
        }

        // Friendly Fire
        if (config.FriendlyFire)
        {
            Server.ExecuteCommand("mp_friendlyfire 1");
        }

        // Municao infinita
        if (config.InfiniteAmmo)
        {
            Server.ExecuteCommand("sv_infinite_ammo 2");
        }

        // Strip de armas e dar equipamento
        if (config.StripWeapons)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player?.IsValid == true && player.PawnIsAlive)
                {
                    player.RemoveWeapons();

                    if (config.GiveKnife)
                        player.GiveNamedItem("weapon_knife");

                    foreach (var weapon in config.AllowedWeapons)
                    {
                        player.GiveNamedItem(weapon);
                    }
                }
            }
        }

        // Configurar HP e Armor
        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid == true && player.PawnIsAlive)
            {
                var pawn = player.PlayerPawn.Value;
                if (pawn != null)
                {
                    if (config.Health != 100)
                        pawn.Health = config.Health;

                    if (config.Armor > 0)
                        pawn.ArmorValue = config.Armor;

                    // Cores
                    if (player.Team == CsTeam.CounterTerrorist && config.CTColor.HasValue)
                    {
                        pawn.RenderMode = RenderMode_t.kRenderTransColor;
                        var color = config.CTColor.Value;
                        pawn.Render = System.Drawing.Color.FromArgb(255, color.R, color.G, color.B);
                        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                    }
                    else if (player.Team == CsTeam.Terrorist && config.TColor.HasValue)
                    {
                        pawn.RenderMode = RenderMode_t.kRenderTransColor;
                        var color = config.TColor.Value;
                        pawn.Render = System.Drawing.Color.FromArgb(255, color.R, color.G, color.B);
                        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                    }
                }
            }
        }
    }

    #endregion

    #region Special Day Implementations

    private void StartFreeday()
    {
        _plugin.OpenCells();

        foreach (var player in _plugin.GetAlivePlayers(CsTeam.Terrorist))
        {
            var jailPlayer = _plugin.GetJailPlayer(player);
            if (jailPlayer != null)
            {
                jailPlayer.IsFreedayPlayer = true;
            }

            // Aplicar glow verde
            var pawn = player.PlayerPawn.Value;
            if (pawn != null)
            {
                pawn.RenderMode = RenderMode_t.kRenderTransColor;
                pawn.Render = System.Drawing.Color.FromArgb(255, 0, 255, 0);
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            }
        }
    }

    private void StartGravityFreeday()
    {
        StartFreeday();
        Server.ExecuteCommand("sv_gravity 250");
    }

    private void StartWarday()
    {
        // O warden ja define a localizacao pelo comando !wd
        _plugin.PrintToChatAll($"{ChatColors.Red}WARDAY! Guardas em posicao!");
        _plugin.PrintToChatAll($"Expande em 2 minutos!");

        _expandTimer = _plugin.AddTimer(120.0f, () =>
        {
            _plugin.PrintToChatAll($"{ChatColors.Yellow}WARDAY EXPANDIDO! Guardas podem sair!");
        });
    }

    private void StartHideAndSeek()
    {
        // Abrir celas
        _plugin.OpenCells();

        // Lista de CTs para manter cegos
        _blindedPlayers.Clear();

        // Congelar e cegar CTs
        foreach (var ct in _plugin.GetAlivePlayers(CsTeam.CounterTerrorist))
        {
            var pawn = ct.PlayerPawn.Value;
            if (pawn != null)
            {
                // Congelar CT
                pawn.MoveType = MoveType_t.MOVETYPE_NONE;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");

                // Adicionar a lista de cegos
                _blindedPlayers.Add(ct.Slot);

                // Aplicar flash inicial
                ApplyBlindness(ct);
            }
        }

        // Timer para reaplicar cegueira a cada 2 segundos (flash dura ~3s)
        _blindTimer = _plugin.AddTimer(2.0f, () =>
        {
            foreach (var ct in _plugin.GetAlivePlayers(CsTeam.CounterTerrorist))
            {
                if (_blindedPlayers.Contains(ct.Slot))
                {
                    ApplyBlindness(ct);
                }
            }
        }, TimerFlags.REPEAT);

        _plugin.PrintToChatAll($"{ChatColors.Green}Terroristas tem 60 segundos para se esconder!");
        _plugin.PrintToChatAll($"{ChatColors.Yellow}CTs estao CONGELADOS e CEGOS!");

        // Timer para liberar CTs
        _freezeTimer = _plugin.AddTimer(60.0f, () =>
        {
            _plugin.PrintToChatAll($"{ChatColors.Red}CTs liberados! CACADA COMECOU!");

            // Parar timer de cegueira
            _blindTimer?.Kill();
            _blindTimer = null;
            _blindedPlayers.Clear();

            foreach (var ct in _plugin.GetAlivePlayers(CsTeam.CounterTerrorist))
            {
                var pawn = ct.PlayerPawn.Value;
                if (pawn != null)
                {
                    // Descongelar
                    pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
                }
            }
        });
    }

    private void ApplyBlindness(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        // Criar flashbang na posicao do jogador para cega-lo
        var flash = Utilities.CreateEntityByName<CFlashbangProjectile>("flashbang_projectile");
        if (flash != null)
        {
            var pos = pawn.AbsOrigin;
            if (pos != null)
            {
                flash.Teleport(new Vector(pos.X, pos.Y, pos.Z + 64), new QAngle(0, 0, 0), new Vector(0, 0, 0));
                flash.DispatchSpawn();
                flash.AcceptInput("InitializeSpawnFromWorld");
                // Detonar imediatamente
                _plugin.AddTimer(0.1f, () =>
                {
                    if (flash.IsValid)
                        flash.AcceptInput("Detonate");
                });
            }
        }
    }

    private void StartZombie()
    {
        _zombies.Clear();
        _humans.Clear();

        // Escolher um CT aleatorio para ser o primeiro zumbi
        var cts = _plugin.GetAlivePlayers(CsTeam.CounterTerrorist);
        if (cts.Count == 0) return;

        var random = new Random();
        var firstZombie = cts[random.Next(cts.Count)];

        // Trocar para T
        firstZombie.SwitchTeam(CsTeam.Terrorist);
        _zombies.Add(firstZombie);

        // Configurar zumbi
        var pawn = firstZombie.PlayerPawn.Value;
        if (pawn != null)
        {
            pawn.Health = CurrentConfig?.Health ?? 2000;
            pawn.RenderMode = RenderMode_t.kRenderTransColor;
            pawn.Render = System.Drawing.Color.FromArgb(255, 0, 255, 0);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }

        firstZombie.RemoveWeapons();
        firstZombie.GiveNamedItem("weapon_knife");

        // Todos os outros sao humanos
        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid == true && player.PawnIsAlive && player != firstZombie)
            {
                _humans.Add(player);
                // Dar armas aos humanos
                if (player.Team == CsTeam.Terrorist)
                {
                    player.GiveNamedItem("weapon_m4a1");
                    player.GiveNamedItem("weapon_deagle");
                }
            }
        }

        _plugin.PrintToChatAll($"{ChatColors.Green}{firstZombie.PlayerName}{ChatColors.White} e o primeiro {ChatColors.Red}ZUMBI{ChatColors.White}!");
    }

    public void OnZombieKill(CCSPlayerController zombie, CCSPlayerController victim)
    {
        if (CurrentSpecialDay != SpecialDayType.Zombie) return;

        // Transformar vitima em zumbi
        _humans.Remove(victim);
        _zombies.Add(victim);

        victim.SwitchTeam(CsTeam.Terrorist);

        var pawn = victim.PlayerPawn.Value;
        if (pawn != null)
        {
            pawn.Health = (CurrentConfig?.Health ?? 2000) / 2;
            pawn.RenderMode = RenderMode_t.kRenderTransColor;
            pawn.Render = System.Drawing.Color.FromArgb(255, 0, 255, 0);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }

        victim.RemoveWeapons();
        victim.GiveNamedItem("weapon_knife");

        _plugin.PrintToChatAll($"{ChatColors.Red}{victim.PlayerName} foi infectado!");

        // Verificar se ainda ha humanos
        if (_humans.Count == 0)
        {
            _plugin.PrintToChatAll($"{ChatColors.Red}ZUMBIS VENCERAM!");
            EndSpecialDay();
        }
        else if (_humans.Count == 1)
        {
            _plugin.PrintToChatAll($"{ChatColors.Green}{_humans[0].PlayerName} e o ultimo sobrevivente!");
        }
    }

    private void StartGunGame()
    {
        _gunGameLevels.Clear();

        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid == true && player.PawnIsAlive)
            {
                _gunGameLevels[player.Slot] = 0;
                GiveGunGameWeapon(player, 0);
            }
        }

        _plugin.PrintToChatAll($"{ChatColors.Yellow}Gun Game! Mate para avancar de arma!");
        _plugin.PrintToChatAll($"Ultimo nivel: FACA!");
    }

    public void OnGunGameKill(CCSPlayerController killer)
    {
        if (CurrentSpecialDay != SpecialDayType.GunGame) return;

        if (!_gunGameLevels.ContainsKey(killer.Slot))
            _gunGameLevels[killer.Slot] = 0;

        _gunGameLevels[killer.Slot]++;
        var level = _gunGameLevels[killer.Slot];

        if (level >= GunGameWeapons.Count)
        {
            // Venceu!
            _plugin.PrintToChatAll($"{ChatColors.Green}{killer.PlayerName} VENCEU O GUN GAME!");
            EndSpecialDay();
            return;
        }

        GiveGunGameWeapon(killer, level);
        _plugin.PrintToChat(killer, $"{ChatColors.Green}Nivel {level + 1}/{GunGameWeapons.Count}!");
    }

    private void GiveGunGameWeapon(CCSPlayerController player, int level)
    {
        player.RemoveWeapons();

        if (level < GunGameWeapons.Count)
        {
            player.GiveNamedItem(GunGameWeapons[level]);

            // Sempre dar faca
            if (!GunGameWeapons[level].Contains("knife"))
            {
                player.GiveNamedItem("weapon_knife");
            }
        }
    }

    private void StartDodgeball()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid == true && player.PawnIsAlive)
            {
                player.RemoveWeapons();
                player.GiveNamedItem("weapon_decoy");
                player.GiveNamedItem("weapon_decoy");
                player.GiveNamedItem("weapon_decoy");
            }
        }
    }

    private void StartHeadshotOnly()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid == true && player.PawnIsAlive)
            {
                player.RemoveWeapons();
                player.GiveNamedItem("weapon_knife");
                player.GiveNamedItem("weapon_deagle");
            }
        }
    }

    private void StartKnifeFight()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid == true && player.PawnIsAlive)
            {
                player.RemoveWeapons();
                player.GiveNamedItem("weapon_knife");
            }
        }

        Server.ExecuteCommand("sv_gravity 700"); // Gravidade levemente reduzida
    }

    private void StartNoScope()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid == true && player.PawnIsAlive)
            {
                player.RemoveWeapons();
                player.GiveNamedItem("weapon_knife");
                player.GiveNamedItem("weapon_awp");
            }
        }
    }

    private void StartBattleRoyale()
    {
        // Encontrar centro do mapa
        var players = Utilities.GetPlayers().Where(p => p?.IsValid == true && p.PawnIsAlive).ToList();
        if (players.Count == 0) return;

        // Calcular centro medio
        float sumX = 0, sumY = 0, sumZ = 0;
        foreach (var player in players)
        {
            var pos = player.PlayerPawn.Value?.AbsOrigin;
            if (pos != null)
            {
                sumX += pos.X;
                sumY += pos.Y;
                sumZ += pos.Z;
            }
        }

        _zoneCenter = new Vector(sumX / players.Count, sumY / players.Count, sumZ / players.Count);
        _zoneRadius = 5000f;

        // Todos comecam so com faca
        foreach (var player in players)
        {
            player.RemoveWeapons();
            player.GiveNamedItem("weapon_knife");
        }

        Server.ExecuteCommand("mp_friendlyfire 1");

        // Timer para fechar zona
        _plugin.AddTimer(30.0f, ShrinkZone, TimerFlags.REPEAT);

        _plugin.PrintToChatAll($"{ChatColors.Red}BATTLE ROYALE! Encontre armas pelo mapa!");
        _plugin.PrintToChatAll($"A zona comeca a fechar em 30 segundos!");
    }

    private void ShrinkZone()
    {
        if (CurrentSpecialDay != SpecialDayType.BattleRoyale) return;

        _zoneRadius -= 200f;

        if (_zoneRadius <= 100f)
        {
            _plugin.PrintToChatAll($"{ChatColors.Red}ZONA FINAL!");
            return;
        }

        _plugin.PrintToChatAll($"{ChatColors.Yellow}Zona fechando! Raio: {_zoneRadius:F0}");

        // Danificar jogadores fora da zona
        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid != true || !player.PawnIsAlive) continue;

            var pos = player.PlayerPawn.Value?.AbsOrigin;
            if (pos == null || _zoneCenter == null) continue;

            var distance = Math.Sqrt(
                Math.Pow(pos.X - _zoneCenter.X, 2) +
                Math.Pow(pos.Y - _zoneCenter.Y, 2)
            );

            if (distance > _zoneRadius)
            {
                var pawn = player.PlayerPawn.Value;
                if (pawn != null)
                {
                    pawn.Health -= 10;
                    if (pawn.Health <= 0)
                    {
                        pawn.CommitSuicide(true, true);
                    }
                }

                _plugin.PrintToChat(player, $"{ChatColors.Red}VOCE ESTA FORA DA ZONA! Volte rapido!");
            }
        }
    }

    private void StartOneInTheChamber()
    {
        _chamberAmmo.Clear();

        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid == true && player.PawnIsAlive)
            {
                player.RemoveWeapons();
                player.GiveNamedItem("weapon_knife");
                player.GiveNamedItem("weapon_deagle");

                _chamberAmmo[player.Slot] = 1;

                // TODO: Configurar arma com apenas 1 bala
            }
        }

        Server.ExecuteCommand("mp_friendlyfire 1");

        _plugin.PrintToChatAll($"{ChatColors.Yellow}ONE IN THE CHAMBER!");
        _plugin.PrintToChatAll($"1 bala. Mate para ganhar mais. Faca sempre disponivel!");
    }

    public void OnOneInTheChamberKill(CCSPlayerController killer)
    {
        if (CurrentSpecialDay != SpecialDayType.OneInTheChamber) return;

        if (!_chamberAmmo.ContainsKey(killer.Slot))
            _chamberAmmo[killer.Slot] = 0;

        _chamberAmmo[killer.Slot]++;

        // TODO: Dar +1 bala
        _plugin.PrintToChat(killer, $"{ChatColors.Green}+1 bala!");
    }

    private void StartGoldenKnife()
    {
        // Escolher jogador aleatorio para ter a faca dourada
        var players = Utilities.GetPlayers().Where(p => p?.IsValid == true && p.PawnIsAlive).ToList();
        if (players.Count == 0) return;

        var random = new Random();
        var goldenPlayer = players[random.Next(players.Count)];

        foreach (var player in players)
        {
            player.RemoveWeapons();
            player.GiveNamedItem("weapon_knife");
        }

        // Marcar jogador com faca dourada
        var pawn = goldenPlayer.PlayerPawn.Value;
        if (pawn != null)
        {
            pawn.RenderMode = RenderMode_t.kRenderTransColor;
            pawn.Render = System.Drawing.Color.FromArgb(255, 255, 215, 0); // Gold
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }

        Server.ExecuteCommand("mp_friendlyfire 1");

        _plugin.PrintToChatAll($"{ChatColors.Yellow}{goldenPlayer.PlayerName} tem a FACA DOURADA!");
        _plugin.PrintToChatAll($"Mate-o para pegar a faca! So ela mata em 1 hit!");
    }

    private void StartSimonSays()
    {
        _plugin.PrintToChatAll($"{ChatColors.Purple}=== SIMON SAYS ==={ChatColors.White}");
        _plugin.PrintToChatAll($"O Warden e Simon! So obedeca quando comecar com 'Simon Says'!");

        // O warden controla este modo manualmente
    }

    private void StartHotPotato()
    {
        var players = Utilities.GetPlayers().Where(p => p?.IsValid == true && p.PawnIsAlive).ToList();
        if (players.Count < 2) return;

        foreach (var player in players)
        {
            player.RemoveWeapons();
            player.GiveNamedItem("weapon_knife");
        }

        // Escolher quem comeca com a batata
        var random = new Random();
        var hotPlayer = players[random.Next(players.Count)];

        var pawn = hotPlayer.PlayerPawn.Value;
        if (pawn != null)
        {
            pawn.RenderMode = RenderMode_t.kRenderTransColor;
            pawn.Render = System.Drawing.Color.FromArgb(255, 255, 0, 0);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }

        _plugin.PrintToChatAll($"{ChatColors.Red}{hotPlayer.PlayerName} comeca com a BATATA QUENTE!");
        _plugin.PrintToChatAll($"Esfaqueie alguem para passar!");

        // Timer para explosao
        var explosionTime = random.Next(10, 30);
        _durationTimer = _plugin.AddTimer(explosionTime, () =>
        {
            // Encontrar quem tem a batata (cor vermelha) e matar
            foreach (var p in Utilities.GetPlayers())
            {
                if (p?.IsValid != true || !p.PawnIsAlive) continue;

                var pw = p.PlayerPawn.Value;
                if (pw?.Render.R == 255 && pw.Render.G == 0)
                {
                    _plugin.PrintToChatAll($"{ChatColors.Red}BOOM! {p.PlayerName} explodiu!");
                    pw.CommitSuicide(true, true);
                    break;
                }
            }

            // Resetar e continuar se houver mais de 1 vivo
            var alive = Utilities.GetPlayers().Where(p => p?.IsValid == true && p.PawnIsAlive).ToList();
            if (alive.Count > 1)
            {
                StartHotPotato();
            }
            else if (alive.Count == 1)
            {
                _plugin.PrintToChatAll($"{ChatColors.Green}{alive[0].PlayerName} VENCEU!");
                EndSpecialDay();
            }
        });
    }

    private void StartFreezeTag()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid == true && player.PawnIsAlive)
            {
                player.RemoveWeapons();
                player.GiveNamedItem("weapon_knife");
            }
        }

        _durationTimer = _plugin.AddTimer(CurrentConfig?.Duration ?? 180, () =>
        {
            // Contar Ts nao congelados
            var unfrozenTs = 0;
            foreach (var p in _plugin.GetAlivePlayers(CsTeam.Terrorist))
            {
                var pawn = p.PlayerPawn.Value;
                if (pawn?.MoveType != MoveType_t.MOVETYPE_NONE)
                {
                    unfrozenTs++;
                }
            }

            if (unfrozenTs > 0)
            {
                _plugin.PrintToChatAll($"{ChatColors.Green}Terroristas venceram! {unfrozenTs} sobreviveram!");
            }
            else
            {
                _plugin.PrintToChatAll($"{ChatColors.Blue}Guardas venceram! Todos congelados!");
            }

            EndSpecialDay();
        });

        _plugin.PrintToChatAll($"{ChatColors.LightBlue}FREEZE TAG! CTs congelam, Ts descongelam aliados!");
    }

    private void StartSumoWrestling()
    {
        // Abrir celas
        _plugin.OpenCells();

        // Remover todas as armas e dar apenas facas
        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid == true && player.PawnIsAlive)
            {
                player.RemoveWeapons();
                player.GiveNamedItem("weapon_knife");

                // Aplicar configuracoes de sumo
                var pawn = player.PlayerPawn.Value;
                if (pawn != null)
                {
                    // Alta vida para aguentar mais hits
                    pawn.Health = CurrentConfig?.Health ?? 500;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

                    // Knockback aumentado (velocidade maior ao levar hit)
                    // Aplicar cor baseada no time
                    if (player.Team == CsTeam.Terrorist)
                    {
                        pawn.RenderMode = RenderMode_t.kRenderTransColor;
                        pawn.Render = System.Drawing.Color.Orange;
                    }
                    else
                    {
                        pawn.RenderMode = RenderMode_t.kRenderTransColor;
                        pawn.Render = System.Drawing.Color.Cyan;
                    }
                    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                }
            }
        }

        _plugin.PrintToChatAll($"{ChatColors.Yellow}SUMO WRESTLING! Empurre os inimigos para fora da arena!");
        _plugin.PrintToChatAll($"{ChatColors.Yellow}Apenas facas - ultimo time de pe vence!");
    }

    private void StartDeathRun()
    {
        // Abrir celas
        _plugin.OpenCells();

        // Ts sao os runners - sem armas
        foreach (var t in _plugin.GetAlivePlayers(CsTeam.Terrorist))
        {
            t.RemoveWeapons();
            t.GiveNamedItem("weapon_knife");

            var pawn = t.PlayerPawn.Value;
            if (pawn != null)
            {
                // Runners tem mais velocidade
                pawn.VelocityModifier = 1.2f;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

                // Glow verde para runners
                pawn.RenderMode = RenderMode_t.kRenderTransColor;
                pawn.Render = System.Drawing.Color.LightGreen;
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            }
        }

        // CTs sao os trappers - podem ativar armadilhas
        foreach (var ct in _plugin.GetAlivePlayers(CsTeam.CounterTerrorist))
        {
            ct.RemoveWeapons();
            // CTs nao tem armas, apenas ativam armadilhas do mapa

            var pawn = ct.PlayerPawn.Value;
            if (pawn != null)
            {
                // Trappers ficam parados
                pawn.MoveType = MoveType_t.MOVETYPE_NONE;

                // Glow vermelho para trappers
                pawn.RenderMode = RenderMode_t.kRenderTransColor;
                pawn.Render = System.Drawing.Color.Red;
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            }
        }

        // Timer para liberar CTs depois de um tempo (opcional)
        _plugin.AddTimer(60.0f, () =>
        {
            if (CurrentSpecialDay != SpecialDayType.DeathRun) return;

            _plugin.PrintToChatAll($"{ChatColors.Red}CTs liberados para cacar!");
            foreach (var ct in _plugin.GetAlivePlayers(CsTeam.CounterTerrorist))
            {
                var pawn = ct.PlayerPawn.Value;
                if (pawn != null)
                {
                    pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                }
                ct.GiveNamedItem("weapon_knife");
            }
        });

        _plugin.PrintToChatAll($"{ChatColors.Green}DEATH RUN! Terroristas devem completar o percurso!");
        _plugin.PrintToChatAll($"{ChatColors.Red}CTs controlam as armadilhas. Sobreviva ate o fim!");
    }

    #endregion

    #region Commands

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandSpecialDay(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (player.Team != CsTeam.CounterTerrorist)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas CTs podem iniciar Special Days!");
            return;
        }

        // Verificar se e warden ou admin
        if (_plugin.Warden?.CurrentWarden != player)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas o Warden pode iniciar Special Days!");
            return;
        }

        ShowSpecialDayMenu(player);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandFreeday(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        if (!CanStartSpecialDay(player)) return;
        StartSpecialDay(SpecialDayType.Freeday, player);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandHideAndSeek(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        if (!CanStartSpecialDay(player)) return;
        StartSpecialDay(SpecialDayType.HideAndSeek, player);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandZombie(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        if (!CanStartSpecialDay(player)) return;
        StartSpecialDay(SpecialDayType.Zombie, player);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandGunGame(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        if (!CanStartSpecialDay(player)) return;
        StartSpecialDay(SpecialDayType.GunGame, player);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandBattleRoyale(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        if (!CanStartSpecialDay(player)) return;
        StartSpecialDay(SpecialDayType.BattleRoyale, player);
    }

    private bool CanStartSpecialDay(CCSPlayerController player)
    {
        if (player.Team != CsTeam.CounterTerrorist)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas CTs podem iniciar Special Days!");
            return false;
        }

        if (_plugin.Warden?.CurrentWarden != player)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas o Warden pode iniciar Special Days!");
            return false;
        }

        return true;
    }

    #endregion

    #region Menus

    public void ShowSpecialDayMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Special Days");

        foreach (var sdName in _plugin.Config.AllowedSpecialDays)
        {
            if (Enum.TryParse<SpecialDayType>(sdName, out var type))
            {
                var configs = SpecialDayConfigs.GetAll();
                if (configs.TryGetValue(type, out var config))
                {
                    menu.AddMenuOption(config.Name, (p, o) =>
                    {
                        StartSpecialDay(type, p);
                    });
                }
            }
        }

        MenuManager.OpenChatMenu(player, menu);
    }

    #endregion
}
