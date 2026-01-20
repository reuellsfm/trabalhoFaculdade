using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using SuperJailbreak.Core;
using SuperJailbreak.Models;

namespace SuperJailbreak.Modules.Gangs;

/// <summary>
/// Modulo de Gangs/Clas completo
/// Inclui: Criacao, membros, ranks, perks, bank
/// </summary>
public class GangsModule
{
    private readonly SuperJailbreakPlugin _plugin;

    // Gangs carregadas
    public Dictionary<int, Gang> LoadedGangs { get; private set; } = new();
    private Dictionary<ulong, int?> _playerGangs = new(); // SteamId -> GangId

    // Gang Perks disponiveis
    private readonly List<GangPerk> _availablePerks = new()
    {
        new GangPerk { Id = "credit_bonus_5", Name = "+5% Creditos", Description = "Bonus de 5% em creditos ganhos", Cost = 1000, RequiredLevel = 1, Type = PerkType.CreditBonus, Value = 0.05f },
        new GangPerk { Id = "credit_bonus_10", Name = "+10% Creditos", Description = "Bonus de 10% em creditos ganhos", Cost = 3000, RequiredLevel = 5, Type = PerkType.CreditBonus, Value = 0.10f },
        new GangPerk { Id = "health_bonus_10", Name = "+10 HP", Description = "+10 HP ao spawnar", Cost = 2000, RequiredLevel = 3, Type = PerkType.HealthBonus, Value = 10 },
        new GangPerk { Id = "health_bonus_25", Name = "+25 HP", Description = "+25 HP ao spawnar", Cost = 5000, RequiredLevel = 8, Type = PerkType.HealthBonus, Value = 25 },
        new GangPerk { Id = "armor_bonus", Name = "+25 Armor", Description = "+25 Armor ao spawnar", Cost = 2500, RequiredLevel = 4, Type = PerkType.ArmorBonus, Value = 25 },
        new GangPerk { Id = "speed_bonus", Name = "+5% Velocidade", Description = "+5% de velocidade", Cost = 3000, RequiredLevel = 5, Type = PerkType.SpeedBonus, Value = 0.05f },
        new GangPerk { Id = "knife_speed", Name = "+10% Velocidade c/ Faca", Description = "Mais rapido com faca", Cost = 1500, RequiredLevel = 2, Type = PerkType.KnifeSpeed, Value = 0.10f },
        new GangPerk { Id = "lr_bonus", Name = "+10% LR Bonus", Description = "Mais creditos em LR wins", Cost = 4000, RequiredLevel = 6, Type = PerkType.LRWinBonus, Value = 0.10f },
        new GangPerk { Id = "grenade_spawn", Name = "Granada ao Spawnar", Description = "Recebe 1 HE ao spawnar (T)", Cost = 5000, RequiredLevel = 7, Type = PerkType.GrenadeOnSpawn, Value = 1 },
        new GangPerk { Id = "flash_spawn", Name = "Flash ao Spawnar", Description = "Recebe 1 Flash ao spawnar (T)", Cost = 3000, RequiredLevel = 4, Type = PerkType.FlashOnSpawn, Value = 1 }
    };

    public GangsModule(SuperJailbreakPlugin plugin)
    {
        _plugin = plugin;
    }

    public void Initialize()
    {
        RegisterCommands();
        LoadGangsFromDatabase();
    }

    public void Unload()
    {
        SaveAllGangs();
    }

    private void RegisterCommands()
    {
        _plugin.AddCommand("css_gang", "Menu de Gangs", CommandGang);
        _plugin.AddCommand("css_gangs", "Menu de Gangs", CommandGang);
        _plugin.AddCommand("css_gangcreate", "Criar uma gang", CommandGangCreate);
        _plugin.AddCommand("css_ganginvite", "Convidar para gang", CommandGangInvite);
        _plugin.AddCommand("css_gangleave", "Sair da gang", CommandGangLeave);
        _plugin.AddCommand("css_gangchat", "Chat da gang", CommandGangChat);
        _plugin.AddCommand("css_gc", "Chat da gang", CommandGangChat);
        _plugin.AddCommand("css_gangtop", "Top gangs", CommandGangTop);
    }

    private void LoadGangsFromDatabase()
    {
        // Carregar gangs do banco de dados
        if (_plugin.Database != null)
        {
            Task.Run(async () =>
            {
                var gangs = await _plugin.Database.LoadGangsAsync();
                foreach (var gang in gangs)
                {
                    LoadedGangs[gang.Id] = gang;
                }
                _plugin.Logger.LogInformation($"[Gangs] {gangs.Count} gangs carregadas");
            });
        }
    }

    private void SaveAllGangs()
    {
        if (_plugin.Database != null)
        {
            foreach (var gang in LoadedGangs.Values)
            {
                Task.Run(async () =>
                {
                    await _plugin.Database.SaveGangAsync(gang);
                });
            }
        }
    }

    #region Gang Management

    public Gang? GetPlayerGang(CCSPlayerController player)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer?.GangId == null) return null;
        return LoadedGangs.TryGetValue(jailPlayer.GangId.Value, out var gang) ? gang : null;
    }

    public Gang? GetGangById(int id)
    {
        return LoadedGangs.TryGetValue(id, out var gang) ? gang : null;
    }

    public void CreateGang(CCSPlayerController player, string name, string tag)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer == null) return;

        // Verificar se ja tem gang
        if (jailPlayer.GangId != null)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Voce ja esta em uma gang!");
            return;
        }

        // Verificar creditos
        if (jailPlayer.Credits < _plugin.Config.GangCreationCost)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Voce precisa de {_plugin.Config.GangCreationCost} creditos!");
            return;
        }

        // Verificar nome unico
        if (LoadedGangs.Values.Any(g => g.Name.ToLower() == name.ToLower()))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Ja existe uma gang com esse nome!");
            return;
        }

        // Verificar tag
        if (tag.Length > _plugin.Config.GangTagMaxLength)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Tag muito longa! Max: {_plugin.Config.GangTagMaxLength}");
            return;
        }

        // Remover creditos
        _plugin.Economy?.RemoveCredits(player, _plugin.Config.GangCreationCost);

        // Criar gang
        var gangId = LoadedGangs.Count > 0 ? LoadedGangs.Keys.Max() + 1 : 1;
        var gang = new Gang
        {
            Id = gangId,
            Name = name,
            Tag = tag,
            LeaderSteamId = player.SteamID,
            CreatedAt = DateTime.UtcNow,
            MaxMembers = _plugin.Config.GangMaxMembers
        };

        // Adicionar lider como membro
        gang.Members.Add(new GangMember
        {
            SteamId = player.SteamID,
            Name = player.PlayerName,
            Rank = GangRank.Leader,
            JoinedAt = DateTime.UtcNow
        });

        LoadedGangs[gangId] = gang;

        // Atualizar jogador
        jailPlayer.GangId = gangId;
        jailPlayer.GangName = name;
        jailPlayer.GangRank = GangRank.Leader;

        _plugin.PrintToChatAll($"{ChatColors.Purple}[{tag}] {name}{ChatColors.White} foi criada por {ChatColors.Green}{player.PlayerName}!");
        _plugin.Achievements?.CheckAchievement(player, AchievementType.CreateGang, 1);

        // Salvar no banco de dados
        if (_plugin.Database != null)
        {
            Task.Run(async () => await _plugin.Database.SaveGangAsync(gang));
        }
    }

    public void InviteToGang(CCSPlayerController inviter, CCSPlayerController target)
    {
        var gang = GetPlayerGang(inviter);
        if (gang == null)
        {
            _plugin.PrintToChat(inviter, $"{ChatColors.Red}Voce nao esta em uma gang!");
            return;
        }

        var inviterJail = _plugin.GetJailPlayer(inviter);
        if (inviterJail?.GangRank < GangRank.Officer)
        {
            _plugin.PrintToChat(inviter, $"{ChatColors.Red}Voce nao tem permissao para convidar!");
            return;
        }

        var targetJail = _plugin.GetJailPlayer(target);
        if (targetJail?.GangId != null)
        {
            _plugin.PrintToChat(inviter, $"{ChatColors.Red}{target.PlayerName} ja esta em uma gang!");
            return;
        }

        if (!gang.CanAddMember())
        {
            _plugin.PrintToChat(inviter, $"{ChatColors.Red}Gang esta cheia! ({gang.GetMemberCount()}/{gang.MaxMembers})");
            return;
        }

        // Enviar convite
        _plugin.PrintToChat(target, $"{ChatColors.Green}Voce foi convidado para a gang [{gang.Tag}] {gang.Name}!");
        _plugin.PrintToChat(target, $"Digite {ChatColors.Yellow}!gangaccept{ChatColors.White} para aceitar");

        // Armazenar convite temporario
        _pendingInvites[target.SteamID] = gang.Id;

        _plugin.PrintToChat(inviter, $"{ChatColors.Green}Convite enviado para {target.PlayerName}!");
    }

    private Dictionary<ulong, int> _pendingInvites = new();

    public void AcceptGangInvite(CCSPlayerController player)
    {
        if (!_pendingInvites.TryGetValue(player.SteamID, out var gangId))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Voce nao tem convites pendentes!");
            return;
        }

        var gang = GetGangById(gangId);
        if (gang == null)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Gang nao encontrada!");
            _pendingInvites.Remove(player.SteamID);
            return;
        }

        // Adicionar membro
        gang.Members.Add(new GangMember
        {
            SteamId = player.SteamID,
            Name = player.PlayerName,
            Rank = GangRank.Member,
            JoinedAt = DateTime.UtcNow
        });

        // Atualizar jogador
        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer != null)
        {
            jailPlayer.GangId = gangId;
            jailPlayer.GangName = gang.Name;
            jailPlayer.GangRank = GangRank.Member;
        }

        _pendingInvites.Remove(player.SteamID);

        // Notificar membros online
        foreach (var p in Utilities.GetPlayers())
        {
            if (p?.IsValid == true)
            {
                var jp = _plugin.GetJailPlayer(p);
                if (jp?.GangId == gangId)
                {
                    _plugin.PrintToChat(p, $"{ChatColors.Green}{player.PlayerName} entrou na gang!");
                }
            }
        }

        _plugin.Achievements?.CheckAchievement(player, AchievementType.JoinGang, 1);
    }

    public void LeaveGang(CCSPlayerController player)
    {
        var gang = GetPlayerGang(player);
        if (gang == null)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Voce nao esta em uma gang!");
            return;
        }

        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer == null) return;

        // Se e lider, transferir ou deletar gang
        if (jailPlayer.GangRank == GangRank.Leader)
        {
            if (gang.Members.Count > 1)
            {
                // Transferir lideranca para proximo rank mais alto
                var newLeader = gang.Members
                    .Where(m => m.SteamId != player.SteamID)
                    .OrderByDescending(m => m.Rank)
                    .FirstOrDefault();

                if (newLeader != null)
                {
                    newLeader.Rank = GangRank.Leader;
                    gang.LeaderSteamId = newLeader.SteamId;
                    _plugin.PrintToChatAll($"{ChatColors.Yellow}{newLeader.Name} e o novo lider de [{gang.Tag}]!");
                }
            }
            else
            {
                // Deletar gang
                LoadedGangs.Remove(gang.Id);
                _plugin.PrintToChatAll($"{ChatColors.Red}Gang [{gang.Tag}] {gang.Name} foi dissolvida!");
            }
        }

        // Remover membro
        gang.Members.RemoveAll(m => m.SteamId == player.SteamID);

        // Limpar jogador
        jailPlayer.GangId = null;
        jailPlayer.GangName = null;
        jailPlayer.GangRank = GangRank.Member;

        _plugin.PrintToChat(player, $"{ChatColors.Yellow}Voce saiu da gang!");
    }

    public void SendGangChat(CCSPlayerController sender, string message)
    {
        var gang = GetPlayerGang(sender);
        if (gang == null)
        {
            _plugin.PrintToChat(sender, $"{ChatColors.Red}Voce nao esta em uma gang!");
            return;
        }

        // Enviar para todos os membros online
        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid == true)
            {
                var jailPlayer = _plugin.GetJailPlayer(player);
                if (jailPlayer?.GangId == gang.Id)
                {
                    player.PrintToChat($" {ChatColors.Purple}[{gang.Tag}] {sender.PlayerName}:{ChatColors.White} {message}");
                }
            }
        }
    }

    #endregion

    #region Gang Perks

    public void ApplyGangPerks(CCSPlayerController player)
    {
        var gang = GetPlayerGang(player);
        if (gang == null) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        foreach (var perk in gang.UnlockedPerks)
        {
            switch (perk.Type)
            {
                case PerkType.HealthBonus:
                    pawn.Health += (int)perk.Value;
                    break;

                case PerkType.ArmorBonus:
                    pawn.ArmorValue += (int)perk.Value;
                    break;

                case PerkType.SpeedBonus:
                    pawn.VelocityModifier *= (1 + perk.Value);
                    break;

                case PerkType.GrenadeOnSpawn:
                    if (player.Team == CsTeam.Terrorist)
                        player.GiveNamedItem("weapon_hegrenade");
                    break;

                case PerkType.FlashOnSpawn:
                    if (player.Team == CsTeam.Terrorist)
                        player.GiveNamedItem("weapon_flashbang");
                    break;
            }
        }
    }

    public float GetCreditMultiplier(CCSPlayerController player)
    {
        var gang = GetPlayerGang(player);
        if (gang == null) return 1.0f;

        float bonus = 1.0f;
        foreach (var perk in gang.UnlockedPerks.Where(p => p.Type == PerkType.CreditBonus))
        {
            bonus += perk.Value;
        }
        return bonus;
    }

    public void PurchasePerk(CCSPlayerController player, string perkId)
    {
        var gang = GetPlayerGang(player);
        if (gang == null) return;

        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer?.GangRank < GangRank.CoLeader)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Apenas Co-Lideres ou Lideres podem comprar perks!");
            return;
        }

        var perk = _availablePerks.FirstOrDefault(p => p.Id == perkId);
        if (perk == null)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Perk nao encontrado!");
            return;
        }

        if (gang.UnlockedPerks.Any(p => p.Id == perkId))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Perk ja desbloqueado!");
            return;
        }

        if (gang.Level < perk.RequiredLevel)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Gang precisa ser nivel {perk.RequiredLevel}!");
            return;
        }

        if (gang.TotalCredits < perk.Cost)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Banco da gang insuficiente! Precisa: {perk.Cost}");
            return;
        }

        // Comprar
        gang.TotalCredits -= perk.Cost;
        gang.UnlockedPerks.Add(perk);

        // Notificar membros
        foreach (var p in Utilities.GetPlayers())
        {
            var jp = _plugin.GetJailPlayer(p);
            if (jp?.GangId == gang.Id)
            {
                _plugin.PrintToChat(p, $"{ChatColors.Green}Gang desbloqueou: {perk.Name}!");
            }
        }
    }

    public void DonateToGang(CCSPlayerController player, int amount)
    {
        var gang = GetPlayerGang(player);
        if (gang == null) return;

        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer == null || jailPlayer.Credits < amount)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Creditos insuficientes!");
            return;
        }

        _plugin.Economy?.RemoveCredits(player, amount);
        gang.TotalCredits += amount;

        // Atualizar contribuicao do membro
        var member = gang.Members.FirstOrDefault(m => m.SteamId == player.SteamID);
        if (member != null)
        {
            member.ContributedCredits += amount;
        }

        // Dar XP para gang
        gang.AddExperience(amount / 10);

        _plugin.PrintToChat(player, $"{ChatColors.Green}Voce doou {amount} creditos para a gang!");
        _plugin.PrintToChat(player, $"Banco da Gang: {gang.TotalCredits}");
    }

    #endregion

    #region Commands

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandGang(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        ShowGangMenu(player);
    }

    [CommandHelper(minArgs: 2, usage: "<nome> <tag>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandGangCreate(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        var name = info.GetArg(1);
        var tag = info.GetArg(2);

        CreateGang(player, name, tag);
    }

    [CommandHelper(minArgs: 1, usage: "<jogador>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandGangInvite(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        var targetName = info.GetArg(1);
        var target = Utilities.GetPlayers().FirstOrDefault(p =>
            p?.IsValid == true && p.PlayerName.ToLower().Contains(targetName.ToLower()));

        if (target == null)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Jogador nao encontrado!");
            return;
        }

        InviteToGang(player, target);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandGangLeave(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        LeaveGang(player);
    }

    [CommandHelper(minArgs: 1, usage: "<mensagem>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandGangChat(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        var message = string.Join(" ", Enumerable.Range(1, info.ArgCount - 1).Select(i => info.GetArg(i)));
        SendGangChat(player, message);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandGangTop(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        var topGangs = LoadedGangs.Values
            .OrderByDescending(g => g.Level)
            .ThenByDescending(g => g.TotalCredits)
            .Take(10)
            .ToList();

        player.PrintToChat($" {ChatColors.Purple}=== TOP 10 GANGS ==={ChatColors.White}");

        for (int i = 0; i < topGangs.Count; i++)
        {
            var gang = topGangs[i];
            player.PrintToChat($" {i + 1}. [{gang.Tag}] {gang.Name} - Lvl {gang.Level} ({gang.GetMemberCount()} membros)");
        }
    }

    #endregion

    #region Menus

    public void ShowGangMenu(CCSPlayerController player)
    {
        var gang = GetPlayerGang(player);
        var jailPlayer = _plugin.GetJailPlayer(player);

        var menu = new ChatMenu("Menu de Gangs");

        if (gang == null)
        {
            menu.AddMenuOption("Criar Gang", (p, o) => ShowCreateGangMenu(p));
            menu.AddMenuOption("Aceitar Convite", (p, o) => AcceptGangInvite(p));
            menu.AddMenuOption("Top Gangs", (p, o) => CommandGangTop(p, null!));
        }
        else
        {
            menu.AddMenuOption($"Gang: [{gang.Tag}] {gang.Name}", (p, o) => { }, true);
            menu.AddMenuOption($"Nivel: {gang.Level} | Banco: {gang.TotalCredits}", (p, o) => { }, true);
            menu.AddMenuOption("Ver Membros", (p, o) => ShowMembersMenu(p, gang));
            menu.AddMenuOption("Doar Creditos", (p, o) => ShowDonateMenu(p));

            if (jailPlayer?.GangRank >= GangRank.Officer)
            {
                menu.AddMenuOption("Convidar Jogador", (p, o) => ShowInviteMenu(p));
            }

            if (jailPlayer?.GangRank >= GangRank.CoLeader)
            {
                menu.AddMenuOption("Perks da Gang", (p, o) => ShowPerksMenu(p, gang));
            }

            menu.AddMenuOption($"{ChatColors.Red}Sair da Gang", (p, o) => LeaveGang(p));
        }

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowCreateGangMenu(CCSPlayerController player)
    {
        _plugin.PrintToChat(player, $"{ChatColors.Yellow}Para criar uma gang, use:");
        _plugin.PrintToChat(player, $"!gangcreate <nome> <tag>");
        _plugin.PrintToChat(player, $"Custo: {ChatColors.Green}{_plugin.Config.GangCreationCost} creditos");
    }

    private void ShowMembersMenu(CCSPlayerController player, Gang gang)
    {
        var menu = new ChatMenu($"Membros de [{gang.Tag}]");

        foreach (var member in gang.Members.OrderByDescending(m => m.Rank))
        {
            var rankName = member.Rank switch
            {
                GangRank.Leader => "[Lider]",
                GangRank.CoLeader => "[Co-Lider]",
                GangRank.Officer => "[Oficial]",
                _ => "[Membro]"
            };
            menu.AddMenuOption($"{rankName} {member.Name}", (p, o) => { }, true);
        }

        menu.AddMenuOption("Voltar", (p, o) => ShowGangMenu(p));

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowInviteMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Convidar Jogador");

        foreach (var target in Utilities.GetPlayers())
        {
            if (target?.IsValid == true && target != player)
            {
                var jp = _plugin.GetJailPlayer(target);
                if (jp?.GangId == null)
                {
                    menu.AddMenuOption(target.PlayerName, (p, o) => InviteToGang(p, target));
                }
            }
        }

        menu.AddMenuOption("Voltar", (p, o) => ShowGangMenu(p));

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowDonateMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Doar Creditos");
        var jailPlayer = _plugin.GetJailPlayer(player);

        var amounts = new[] { 100, 500, 1000, 5000 };
        foreach (var amount in amounts)
        {
            var canAfford = (jailPlayer?.Credits ?? 0) >= amount;
            menu.AddMenuOption($"{amount} creditos", (p, o) => DonateToGang(p, amount), !canAfford);
        }

        menu.AddMenuOption("Voltar", (p, o) => ShowGangMenu(p));

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowPerksMenu(CCSPlayerController player, Gang gang)
    {
        var menu = new ChatMenu("Perks da Gang");

        foreach (var perk in _availablePerks)
        {
            var unlocked = gang.UnlockedPerks.Any(p => p.Id == perk.Id);
            var canAfford = gang.TotalCredits >= perk.Cost && gang.Level >= perk.RequiredLevel;

            var status = unlocked ? "[OK]" : $"[{perk.Cost}c]";
            var color = unlocked ? ChatColors.Green : (canAfford ? ChatColors.Yellow : ChatColors.Red);

            menu.AddMenuOption($"{color}{status} {perk.Name}", (p, o) =>
            {
                if (!unlocked)
                    PurchasePerk(p, perk.Id);
            }, unlocked || !canAfford);
        }

        menu.AddMenuOption("Voltar", (p, o) => ShowGangMenu(p));

        MenuManager.OpenChatMenu(player, menu);
    }

    #endregion
}
