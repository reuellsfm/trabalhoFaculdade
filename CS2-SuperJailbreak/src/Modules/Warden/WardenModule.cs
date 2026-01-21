using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using SuperJailbreak.Core;
using SuperJailbreak.Models;
using System.Drawing;

namespace SuperJailbreak.Modules.Warden;

/// <summary>
/// Modulo completo de Warden para o Super Jailbreak
/// Inclui: Laser, Paint, Block/Unblock, Warday, Comandos especiais
/// </summary>
public class WardenModule
{
    private readonly SuperJailbreakPlugin _plugin;

    // Estado do Warden
    public CCSPlayerController? CurrentWarden { get; private set; }
    public int WardenSlot { get; private set; } = -1;
    public DateTime? WardenStartTime { get; private set; }

    // Warday
    public bool IsWardayActive { get; private set; }
    public Vector? WardayLocation { get; private set; }
    public bool IsWardayExpanded { get; private set; }
    private CounterStrikeSharp.API.Modules.Timers.Timer? _wardayExpandTimer;

    // Block/Unblock
    public bool IsBlocked { get; private set; } = true;

    // Laser
    public bool IsLaserActive { get; private set; }
    private Vector? _lastLaserPosition;

    // Paint Markers
    public List<PaintMarker> PaintMarkers { get; private set; } = new();
    private const int MaxPaintMarkers = 50;

    // Cooldowns
    private Dictionary<ulong, DateTime> _wardenCooldowns = new();

    public WardenModule(SuperJailbreakPlugin plugin)
    {
        _plugin = plugin;
    }

    public void Initialize()
    {
        RegisterCommands();
    }

    public void Unload()
    {
        _wardayExpandTimer?.Kill();
        ClearPaintMarkers();
    }

    private void RegisterCommands()
    {
        // Comandos de Warden
        _plugin.AddCommand("css_w", "Se tornar warden", CommandWarden);
        _plugin.AddCommand("css_warden", "Se tornar warden", CommandWarden);
        _plugin.AddCommand("css_uw", "Deixar de ser warden", CommandUnwarden);
        _plugin.AddCommand("css_unwarden", "Deixar de ser warden", CommandUnwarden);

        // Comandos do Warden
        _plugin.AddCommand("css_open", "Abrir celas", CommandOpenCells);
        _plugin.AddCommand("css_close", "Fechar celas", CommandCloseCells);
        _plugin.AddCommand("css_wb", "Bloquear (noblock off)", CommandBlock);
        _plugin.AddCommand("css_wub", "Desbloquear (noblock on)", CommandUnblock);
        _plugin.AddCommand("css_wd", "Iniciar warday", CommandWarday);
        _plugin.AddCommand("css_warday", "Iniciar warday", CommandWarday);
        _plugin.AddCommand("css_fd", "Dar freeday a um jogador", CommandFreeday);
        _plugin.AddCommand("css_freeday", "Dar freeday a um jogador", CommandFreeday);
        _plugin.AddCommand("css_pardon", "Perdoar rebelde", CommandPardon);
        _plugin.AddCommand("css_laser", "Toggle laser do warden", CommandLaser);
        _plugin.AddCommand("css_paint", "Pintar marcador no chao", CommandPaint);
        _plugin.AddCommand("css_clearpaint", "Limpar marcadores", CommandClearPaint);
        _plugin.AddCommand("css_guns", "Menu de armas para CTs", CommandGuns);

        // Comandos de Simon Says
        _plugin.AddCommand("css_simon", "Ativar modo Simon Says", CommandSimonSays);
    }

    #region Warden Management

    public void SetWarden(CCSPlayerController player)
    {
        if (CurrentWarden != null)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Ja existe um Warden!");
            return;
        }

        if (player.Team != CsTeam.CounterTerrorist)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas CTs podem ser Warden!");
            return;
        }

        if (!player.PawnIsAlive)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Voce precisa estar vivo para ser Warden!");
            return;
        }

        // Verificar cooldown
        if (_wardenCooldowns.TryGetValue(player.SteamID, out var lastWarden))
        {
            var cooldown = TimeSpan.FromSeconds(_plugin.Config.WardenCooldownSeconds);
            if (DateTime.UtcNow - lastWarden < cooldown)
            {
                var remaining = cooldown - (DateTime.UtcNow - lastWarden);
                _plugin.PrintToChat(player, $"{ChatColors.Red}Aguarde {remaining.Seconds} segundos para ser Warden novamente!");
                return;
            }
        }

        CurrentWarden = player;
        WardenSlot = player.Slot;
        WardenStartTime = DateTime.UtcNow;

        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer != null)
        {
            jailPlayer.IsWarden = true;
        }

        // Aplicar visual do warden
        ApplyWardenVisuals(player);

        // Anunciar
        _plugin.PrintToChatAll($"{ChatColors.Blue}{player.PlayerName}{ChatColors.White} e agora o {ChatColors.Blue}WARDEN{ChatColors.White}!");
        _plugin.PrintToCenterAll($"{player.PlayerName} e o WARDEN!");

        // Som
        if (_plugin.Config.SoundsEnabled && !string.IsNullOrEmpty(_plugin.Config.WardenAssignedSound))
        {
            foreach (var p in Utilities.GetPlayers().Where(p => p?.IsValid == true))
            {
                p.ExecuteClientCommand($"play {_plugin.Config.WardenAssignedSound}");
            }
        }

        _plugin.Logger.LogInformation($"[Warden] {player.PlayerName} se tornou Warden");
    }

    public void RemoveWarden(bool announce = true)
    {
        if (CurrentWarden == null) return;

        var wardenName = CurrentWarden.PlayerName;
        var jailPlayer = _plugin.GetJailPlayer(CurrentWarden);

        // Registrar cooldown
        _wardenCooldowns[CurrentWarden.SteamID] = DateTime.UtcNow;

        // Remover visuais
        RemoveWardenVisuals(CurrentWarden);

        if (jailPlayer != null)
        {
            jailPlayer.IsWarden = false;
        }

        CurrentWarden = null;
        WardenSlot = -1;
        WardenStartTime = null;
        IsLaserActive = false;

        if (announce)
        {
            _plugin.PrintToChatAll($"{ChatColors.Red}{wardenName}{ChatColors.White} nao e mais o Warden!");
        }

        _plugin.Logger.LogInformation($"[Warden] {wardenName} nao e mais Warden");
    }

    public void RemoveWardenIfPlayer(CCSPlayerController player)
    {
        if (CurrentWarden == player)
        {
            RemoveWarden();
        }
    }

    public void OnWardenDeath(CCSPlayerController player)
    {
        if (CurrentWarden != player) return;

        _plugin.PrintToChatAll($"{ChatColors.Red}O Warden {player.PlayerName} MORREU!");

        // Som de morte do warden
        if (_plugin.Config.SoundsEnabled && !string.IsNullOrEmpty(_plugin.Config.WardenDiedSound))
        {
            foreach (var p in Utilities.GetPlayers().Where(p => p?.IsValid == true))
            {
                p.ExecuteClientCommand($"play {_plugin.Config.WardenDiedSound}");
            }
        }

        RemoveWarden(false);

        // Se nao houver mais CTs vivos, freeday
        var aliveCTs = _plugin.GetAliveCount(CsTeam.CounterTerrorist);
        if (aliveCTs == 0)
        {
            _plugin.PrintToChatAll($"{ChatColors.Green}Todos os guardas morreram! FREEDAY!");
        }
    }

    public void OnRoundStart()
    {
        RemoveWarden(false);
        IsWardayActive = false;
        WardayLocation = null;
        IsWardayExpanded = false;
        IsBlocked = true;
        ClearPaintMarkers();
        _wardayExpandTimer?.Kill();
    }

    private void ApplyWardenVisuals(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        // Cor azul
        pawn.RenderMode = RenderMode_t.kRenderTransColor;
        pawn.Render = System.Drawing.Color.FromArgb(255, 0, 100, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

        // Glow (se habilitado)
        if (_plugin.Config.WardenGlowEnabled)
        {
            // TODO: Implementar glow
        }
    }

    private void RemoveWardenVisuals(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        pawn.RenderMode = RenderMode_t.kRenderNormal;
        pawn.Render = System.Drawing.Color.White;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    #endregion

    #region Rebel System

    public void SetRebel(CCSPlayerController player, bool isRebel)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer == null) return;

        if (jailPlayer.IsRebel == isRebel) return;

        jailPlayer.IsRebel = isRebel;

        if (isRebel)
        {
            jailPlayer.TimesRebelled++;
            _plugin.PrintToChatAll($"{ChatColors.Red}{player.PlayerName}{ChatColors.White} e agora um {ChatColors.Red}REBELDE{ChatColors.White}!");

            // Aplicar glow vermelho
            ApplyRebelVisuals(player);

            // Achievement
            _plugin.Achievements?.CheckAchievement(player, AchievementType.SuccessfulRebellion, 1);
        }
        else
        {
            _plugin.PrintToChatAll($"{ChatColors.Green}{player.PlayerName}{ChatColors.White} foi perdoado!");
            RemoveRebelVisuals(player);
        }
    }

    private void ApplyRebelVisuals(CCSPlayerController player)
    {
        if (!_plugin.Config.RebelGlowEnabled) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        pawn.RenderMode = RenderMode_t.kRenderTransColor;
        pawn.Render = System.Drawing.Color.FromArgb(255, 255, 0, 0);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    private void RemoveRebelVisuals(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        pawn.RenderMode = RenderMode_t.kRenderNormal;
        pawn.Render = System.Drawing.Color.White;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    #endregion

    #region Warday

    public void StartWarday(CCSPlayerController warden, Vector location)
    {
        if (!IsWardenPlayer(warden)) return;

        IsWardayActive = true;
        WardayLocation = location;
        IsWardayExpanded = false;

        _plugin.PrintToChatAll($"{ChatColors.Red}=== WARDAY INICIADO ==={ChatColors.White}");
        _plugin.PrintToChatAll($"Os guardas estao em posicao! Ts devem atacar!");
        _plugin.PrintToChatAll($"Warday expande em 2 minutos!");

        // Timer para expandir
        _wardayExpandTimer = _plugin.AddTimer(120.0f, () =>
        {
            IsWardayExpanded = true;
            _plugin.PrintToChatAll($"{ChatColors.Yellow}WARDAY EXPANDIDO!{ChatColors.White} Guardas podem sair!");
        });
    }

    #endregion

    #region Laser & Paint

    public void UpdateLaser()
    {
        if (!IsLaserActive || CurrentWarden == null || !CurrentWarden.IsValid) return;

        var pawn = CurrentWarden.PlayerPawn.Value;
        if (pawn == null) return;

        var eyePos = pawn.AbsOrigin;
        if (eyePos == null) return;

        // Calcular posicao do olho
        var viewOffset = new Vector(0, 0, 64); // Altura aproximada dos olhos
        var eyePosition = new Vector(eyePos.X, eyePos.Y, eyePos.Z + 64);

        // Obter angulo de visao
        var eyeAngles = pawn.EyeAngles;

        // Calcular direcao
        var pitch = eyeAngles.X * Math.PI / 180;
        var yaw = eyeAngles.Y * Math.PI / 180;

        var direction = new Vector(
            (float)(Math.Cos(pitch) * Math.Cos(yaw)),
            (float)(Math.Cos(pitch) * Math.Sin(yaw)),
            (float)(-Math.Sin(pitch))
        );

        // Trace para encontrar onde o laser bate
        var endPos = new Vector(
            eyePosition.X + direction.X * 2000,
            eyePosition.Y + direction.Y * 2000,
            eyePosition.Z + direction.Z * 2000
        );

        // Desenhar laser (usando beam temporario)
        DrawLaser(eyePosition, endPos);

        _lastLaserPosition = endPos;
    }

    private void DrawLaser(Vector start, Vector end)
    {
        // Usar CBeam diretamente (funciona no CS2)
        var beam = Utilities.CreateEntityByName<CBeam>("beam");
        if (beam == null) return;

        // Configurar cor do laser
        var colorParts = _plugin.Config.WardenLaserColor.Split(',');
        int r = 0, g = 0, b = 255;
        if (colorParts.Length >= 3)
        {
            int.TryParse(colorParts[0], out r);
            int.TryParse(colorParts[1], out g);
            int.TryParse(colorParts[2], out b);
        }

        // Configurar beam
        beam.SetModel("materials/sprites/laserbeam.vmat");
        beam.Render = System.Drawing.Color.FromArgb(255, r, g, b);
        beam.Width = 2.0f;

        // Posicionar inicio e fim
        beam.Teleport(start, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        beam.EndPos.X = end.X;
        beam.EndPos.Y = end.Y;
        beam.EndPos.Z = end.Z;

        beam.DispatchSpawn();
        beam.AcceptInput("TurnOn");

        // Remover beam apos 0.1 segundos
        _plugin.AddTimer(0.1f, () =>
        {
            if (beam.IsValid)
                beam.Remove();
        });
    }

    public void AddPaintMarker(Vector position, System.Drawing.Color color)
    {
        if (PaintMarkers.Count >= MaxPaintMarkers)
        {
            // Remover marcador mais antigo
            var oldest = PaintMarkers[0];
            oldest.Entity?.Remove();
            PaintMarkers.RemoveAt(0);
        }

        var marker = new PaintMarker
        {
            Position = position,
            Color = color,
            CreatedAt = DateTime.UtcNow
        };

        // Criar entidade visual usando prop_dynamic
        var prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (prop != null)
        {
            prop.SetModel("models/props/cs_office/vending_machine.vmdl"); // Modelo pequeno
            prop.Teleport(new Vector(position.X, position.Y, position.Z - 5), new QAngle(0, 0, 0), new Vector(0, 0, 0));

            // Escala pequena para parecer um marcador
            prop.CBodyComponent!.SceneNode!.GetSkeletonInstance().Scale = 0.1f;

            // Aplicar cor
            prop.RenderMode = RenderMode_t.kRenderTransColor;
            prop.Render = color;
            Utilities.SetStateChanged(prop, "CBaseModelEntity", "m_clrRender");

            prop.DispatchSpawn();
            marker.Entity = prop;
        }

        PaintMarkers.Add(marker);
    }

    public void ClearPaintMarkers()
    {
        foreach (var marker in PaintMarkers)
        {
            marker.Entity?.Remove();
        }
        PaintMarkers.Clear();
    }

    #endregion

    #region Commands

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandWarden(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        SetWarden(player);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandUnwarden(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (!IsWardenPlayer(player))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Voce nao e o Warden!");
            return;
        }

        RemoveWarden();
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandOpenCells(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (!IsWardenPlayer(player) && !IsAdmin(player))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas o Warden pode abrir as celas!");
            return;
        }

        _plugin.OpenCells();
        _plugin.PrintToChatAll($"{ChatColors.Green}Celas abertas pelo Warden!");
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandCloseCells(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (!IsWardenPlayer(player) && !IsAdmin(player))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas o Warden pode fechar as celas!");
            return;
        }

        _plugin.CloseCells();
        _plugin.PrintToChatAll($"{ChatColors.Red}Celas fechadas pelo Warden!");
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandBlock(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (!IsWardenPlayer(player))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas o Warden pode usar este comando!");
            return;
        }

        IsBlocked = true;
        Server.ExecuteCommand("mp_solid_teammates 1");
        _plugin.PrintToChatAll($"{ChatColors.Yellow}Colisao entre jogadores ATIVADA!");
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandUnblock(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (!IsWardenPlayer(player))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas o Warden pode usar este comando!");
            return;
        }

        IsBlocked = false;
        Server.ExecuteCommand("mp_solid_teammates 0");
        _plugin.PrintToChatAll($"{ChatColors.Green}Colisao entre jogadores DESATIVADA!");
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandWarday(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (!IsWardenPlayer(player))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas o Warden pode iniciar Warday!");
            return;
        }

        if (IsWardayActive)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Warday ja esta ativo!");
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn?.AbsOrigin == null) return;

        StartWarday(player, pawn.AbsOrigin);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandFreeday(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (!IsWardenPlayer(player))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas o Warden pode dar freeday!");
            return;
        }

        // Mostrar menu de jogadores T
        var menu = new ChatMenu("Dar Freeday para:");

        foreach (var p in Utilities.GetPlayers())
        {
            if (p?.IsValid == true && p.Team == CsTeam.Terrorist && p.PawnIsAlive)
            {
                menu.AddMenuOption(p.PlayerName, (warden, option) =>
                {
                    GiveFreeday(p);
                });
            }
        }

        MenuManager.OpenChatMenu(player, menu);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandPardon(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (!IsWardenPlayer(player))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas o Warden pode perdoar rebeldes!");
            return;
        }

        // Mostrar menu de rebeldes
        var menu = new ChatMenu("Perdoar Rebelde:");

        foreach (var p in Utilities.GetPlayers())
        {
            if (p?.IsValid == true && p.Team == CsTeam.Terrorist)
            {
                var jailPlayer = _plugin.GetJailPlayer(p);
                if (jailPlayer?.IsRebel == true)
                {
                    menu.AddMenuOption($"{p.PlayerName} (Rebelde)", (warden, option) =>
                    {
                        SetRebel(p, false);
                    });
                }
            }
        }

        if (menu.MenuOptions.Count == 0)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Yellow}Nao ha rebeldes para perdoar!");
            return;
        }

        MenuManager.OpenChatMenu(player, menu);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandLaser(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (!IsWardenPlayer(player))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas o Warden pode usar o laser!");
            return;
        }

        if (!_plugin.Config.WardenLaserEnabled)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Laser esta desabilitado!");
            return;
        }

        IsLaserActive = !IsLaserActive;
        _plugin.PrintToChat(player, IsLaserActive ?
            $"{ChatColors.Green}Laser ATIVADO!" :
            $"{ChatColors.Red}Laser DESATIVADO!");
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandPaint(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (!IsWardenPlayer(player))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas o Warden pode pintar!");
            return;
        }

        if (!_plugin.Config.WardenPaintEnabled)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Paint esta desabilitado!");
            return;
        }

        // Usar posicao onde o jogador esta olhando
        if (_lastLaserPosition != null)
        {
            AddPaintMarker(_lastLaserPosition, System.Drawing.Color.Blue);
            _plugin.PrintToChat(player, $"{ChatColors.Green}Marcador adicionado!");
        }
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandClearPaint(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (!IsWardenPlayer(player))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas o Warden pode limpar marcadores!");
            return;
        }

        ClearPaintMarkers();
        _plugin.PrintToChat(player, $"{ChatColors.Green}Marcadores limpos!");
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandGuns(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (player.Team != CsTeam.CounterTerrorist)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas CTs podem usar este comando!");
            return;
        }

        ShowGunsMenu(player);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandSimonSays(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (!IsWardenPlayer(player))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas o Warden pode iniciar Simon Says!");
            return;
        }

        _plugin.SpecialDays?.StartSpecialDay(SpecialDayType.SimonSays, player);
    }

    #endregion

    #region Menus

    public void ShowWardenMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Menu do Warden");

        if (CurrentWarden == null)
        {
            menu.AddMenuOption("Se tornar Warden", (p, o) => SetWarden(p));
        }
        else if (IsWardenPlayer(player))
        {
            menu.AddMenuOption("Deixar de ser Warden", (p, o) => RemoveWarden());
            menu.AddMenuOption("Abrir Celas", (p, o) => CommandOpenCells(p, null!));
            menu.AddMenuOption("Block/Unblock", (p, o) => ShowBlockMenu(p));
            menu.AddMenuOption("Dar Freeday", (p, o) => CommandFreeday(p, null!));
            menu.AddMenuOption("Perdoar Rebelde", (p, o) => CommandPardon(p, null!));
            menu.AddMenuOption("Iniciar Warday", (p, o) => CommandWarday(p, null!));
            menu.AddMenuOption("Toggle Laser", (p, o) => CommandLaser(p, null!));
            menu.AddMenuOption("Mini-Games", (p, o) => ShowMiniGamesMenu(p));
        }
        else
        {
            menu.AddMenuOption($"Warden atual: {CurrentWarden.PlayerName}", (p, o) => { }, true);
        }

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowBlockMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Block/Unblock");

        menu.AddMenuOption(IsBlocked ? "Desbloquear (Noblock ON)" : "Bloquear (Noblock OFF)",
            (p, o) =>
            {
                if (IsBlocked)
                    CommandUnblock(p, null!);
                else
                    CommandBlock(p, null!);
            });

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowMiniGamesMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Mini-Games do Warden");

        menu.AddMenuOption("Simon Says", (p, o) => _plugin.SpecialDays?.StartSpecialDay(SpecialDayType.SimonSays, p));
        menu.AddMenuOption("Hot Potato", (p, o) => _plugin.SpecialDays?.StartSpecialDay(SpecialDayType.HotPotato, p));
        menu.AddMenuOption("Freeze Tag", (p, o) => _plugin.SpecialDays?.StartSpecialDay(SpecialDayType.FreezeTag, p));
        menu.AddMenuOption("Sumo Wrestling", (p, o) => _plugin.SpecialDays?.StartSpecialDay(SpecialDayType.SumoWrestling, p));
        menu.AddMenuOption("Dodgeball", (p, o) => _plugin.SpecialDays?.StartSpecialDay(SpecialDayType.Dodgeball, p));

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowGunsMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Escolha suas Armas");

        menu.AddMenuOption("M4A4 + Deagle", (p, o) => GiveWeaponSet(p, "weapon_m4a1", "weapon_deagle"));
        menu.AddMenuOption("M4A1-S + Deagle", (p, o) => GiveWeaponSet(p, "weapon_m4a1_silencer", "weapon_deagle"));
        menu.AddMenuOption("AK-47 + Deagle", (p, o) => GiveWeaponSet(p, "weapon_ak47", "weapon_deagle"));
        menu.AddMenuOption("AWP + Deagle", (p, o) => GiveWeaponSet(p, "weapon_awp", "weapon_deagle"));
        menu.AddMenuOption("Shotgun + P250", (p, o) => GiveWeaponSet(p, "weapon_xm1014", "weapon_p250"));
        menu.AddMenuOption("SMG (P90) + Five-Seven", (p, o) => GiveWeaponSet(p, "weapon_p90", "weapon_fiveseven"));

        MenuManager.OpenChatMenu(player, menu);
    }

    private void GiveWeaponSet(CCSPlayerController player, string primary, string secondary)
    {
        if (!player.PawnIsAlive)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Voce precisa estar vivo!");
            return;
        }

        player.RemoveWeapons();
        player.GiveNamedItem("weapon_knife");
        player.GiveNamedItem(primary);
        player.GiveNamedItem(secondary);

        // Dar armadura e capacete
        var pawn = player.PlayerPawn.Value;
        if (pawn != null)
        {
            pawn.ArmorValue = 100;
            var itemServices = pawn.ItemServices as CCSPlayer_ItemServices;
            if (itemServices != null)
            {
                itemServices.HasHelmet = true;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_pItemServices");
            }
        }

        _plugin.PrintToChat(player, $"{ChatColors.Green}Armas recebidas!");
    }

    #endregion

    #region Helpers

    public bool IsWardenPlayer(CCSPlayerController? player)
    {
        return player != null && CurrentWarden == player;
    }

    private bool IsAdmin(CCSPlayerController player)
    {
        // Verificar se jogador tem permissao de admin do CounterStrikeSharp
        return AdminManager.PlayerHasPermissions(player, "@css/generic") ||
               AdminManager.PlayerHasPermissions(player, "@css/root");
    }

    private void GiveFreeday(CCSPlayerController player)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer == null) return;

        jailPlayer.IsFreedayPlayer = true;

        // Aplicar glow verde
        var pawn = player.PlayerPawn.Value;
        if (pawn != null)
        {
            pawn.RenderMode = RenderMode_t.kRenderTransColor;
            pawn.Render = System.Drawing.Color.FromArgb(255, 0, 255, 0);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }

        _plugin.PrintToChatAll($"{ChatColors.Green}{player.PlayerName}{ChatColors.White} recebeu {ChatColors.Green}FREEDAY{ChatColors.White}!");
    }

    #endregion
}

public class PaintMarker
{
    public Vector? Position { get; set; }
    public System.Drawing.Color Color { get; set; }
    public DateTime CreatedAt { get; set; }
    public CBaseEntity? Entity { get; set; }
}
