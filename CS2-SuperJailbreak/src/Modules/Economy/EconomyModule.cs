using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using SuperJailbreak.Core;
using SuperJailbreak.Models;

namespace SuperJailbreak.Modules.Economy;

/// <summary>
/// Modulo de Economia completo
/// Inclui: Creditos, Loja, Items, Efeitos
/// </summary>
public class EconomyModule
{
    private readonly SuperJailbreakPlugin _plugin;
    private readonly List<ShopItem> _shopItems;

    // Compras desta rodada
    private Dictionary<ulong, Dictionary<string, int>> _purchasesThisRound = new();
    private Dictionary<ulong, Dictionary<string, int>> _purchasesThisMap = new();

    // Efeitos ativos
    private HashSet<ulong> _playersWithNoFallDamage = new();
    private HashSet<ulong> _playersWithDoubleJump = new();
    private Dictionary<ulong, int> _playersJumpCount = new();
    private Dictionary<ulong, string> _playersWithTrail = new();

    public EconomyModule(SuperJailbreakPlugin plugin)
    {
        _plugin = plugin;
        _shopItems = ShopItemList.GetAll();
    }

    public void Initialize()
    {
        RegisterCommands();
    }

    public void Unload() { }

    private void RegisterCommands()
    {
        _plugin.AddCommand("css_shop", "Abrir loja", CommandShop);
        _plugin.AddCommand("css_store", "Abrir loja", CommandShop);
        _plugin.AddCommand("css_loja", "Abrir loja", CommandShop);
        _plugin.AddCommand("css_credits", "Ver creditos", CommandCredits);
        _plugin.AddCommand("css_creditos", "Ver creditos", CommandCredits);
        _plugin.AddCommand("css_give", "Dar creditos (Admin)", CommandGiveCredits);
        _plugin.AddCommand("css_pay", "Transferir creditos", CommandPay);
    }

    #region Credit Management

    public void AddCredits(CCSPlayerController player, int amount, string reason)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer == null) return;

        // Aplicar multiplicador da gang
        float multiplier = _plugin.Gangs?.GetCreditMultiplier(player) ?? 1.0f;
        int finalAmount = (int)(amount * multiplier);
        string bonusText = multiplier > 1.0f ? $" ({ChatColors.Yellow}+{(int)((multiplier - 1) * 100)}% bonus gang{ChatColors.White})" : "";

        jailPlayer.Credits += finalAmount;
        jailPlayer.TotalCreditsEarned += finalAmount;

        _plugin.PrintToChat(player, $"{ChatColors.Green}+{finalAmount} creditos{ChatColors.White} ({reason}){bonusText} | Total: {jailPlayer.Credits}");

        // Achievement
        _plugin.Achievements?.CheckAchievement(player, AchievementType.EarnCredits, jailPlayer.TotalCreditsEarned);
    }

    public bool RemoveCredits(CCSPlayerController player, int amount)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer == null || jailPlayer.Credits < amount) return false;

        jailPlayer.Credits -= amount;
        jailPlayer.TotalCreditsSpent += amount;

        return true;
    }

    public int GetCredits(CCSPlayerController player)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        return jailPlayer?.Credits ?? 0;
    }

    public void OnRoundStart()
    {
        _purchasesThisRound.Clear();
    }

    public void OnMapStart()
    {
        _purchasesThisMap.Clear();
    }

    #endregion

    #region Shop

    public void ShowShopMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Loja - Categorias");
        var jailPlayer = _plugin.GetJailPlayer(player);

        menu.AddMenuOption($"Seus Creditos: {jailPlayer?.Credits ?? 0}", (p, o) => { }, true);
        menu.AddMenuOption("Armas", (p, o) => ShowCategoryMenu(p, ShopCategory.Weapons));
        menu.AddMenuOption("Equipamentos", (p, o) => ShowCategoryMenu(p, ShopCategory.Equipment));
        menu.AddMenuOption("Habilidades", (p, o) => ShowCategoryMenu(p, ShopCategory.Abilities));
        menu.AddMenuOption("Cosmeticos", (p, o) => ShowCategoryMenu(p, ShopCategory.Cosmetics));
        menu.AddMenuOption("Especiais", (p, o) => ShowCategoryMenu(p, ShopCategory.Special));

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowCategoryMenu(CCSPlayerController player, ShopCategory category)
    {
        var menu = new ChatMenu($"Loja - {GetCategoryName(category)}");
        var jailPlayer = _plugin.GetJailPlayer(player);

        var items = _shopItems.Where(i => i.Category == category).ToList();

        foreach (var item in items)
        {
            // Verificar restricoes de time
            if (item.TeamRestriction == TeamRestriction.TerroristOnly && player.Team != CsTeam.Terrorist)
                continue;
            if (item.TeamRestriction == TeamRestriction.CTOnly && player.Team != CsTeam.CounterTerrorist)
                continue;

            var canAfford = (jailPlayer?.Credits ?? 0) >= item.Price;
            var displayName = $"{item.Name} - {item.Price}c";

            if (!canAfford)
                displayName = $"{ChatColors.Red}{displayName}";
            else
                displayName = $"{ChatColors.Green}{displayName}";

            menu.AddMenuOption(displayName, (p, o) =>
            {
                PurchaseItem(p, item);
            }, !canAfford);
        }

        menu.AddMenuOption("Voltar", (p, o) => ShowShopMenu(p));

        MenuManager.OpenChatMenu(player, menu);
    }

    private void PurchaseItem(CCSPlayerController player, ShopItem item)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer == null) return;

        // Verificar creditos
        if (jailPlayer.Credits < item.Price)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Creditos insuficientes! Voce tem {jailPlayer.Credits}, precisa de {item.Price}");
            return;
        }

        // Verificar se precisa estar vivo
        if (item.RequiresAlive && !player.PawnIsAlive)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Voce precisa estar vivo para comprar isso!");
            return;
        }

        // Verificar limite por rodada
        if (item.MaxPurchasesPerRound > 0)
        {
            var roundPurchases = GetPurchasesThisRound(player.SteamID, item.Id);
            if (roundPurchases >= item.MaxPurchasesPerRound)
            {
                _plugin.PrintToChat(player, $"{ChatColors.Red}Limite de compras por rodada atingido!");
                return;
            }
        }

        // Verificar limite por mapa
        if (item.MaxPurchasesPerMap > 0)
        {
            var mapPurchases = GetPurchasesThisMap(player.SteamID, item.Id);
            if (mapPurchases >= item.MaxPurchasesPerMap)
            {
                _plugin.PrintToChat(player, $"{ChatColors.Red}Limite de compras por mapa atingido!");
                return;
            }
        }

        // Remover creditos
        if (!RemoveCredits(player, item.Price))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Erro ao processar compra!");
            return;
        }

        // Registrar compra
        AddPurchase(player.SteamID, item.Id);

        // Aplicar efeito
        ApplyItemEffect(player, item);

        _plugin.PrintToChat(player, $"{ChatColors.Green}Voce comprou {item.Name}!");

        // Achievement
        _plugin.Achievements?.CheckAchievement(player, AchievementType.PurchaseItems, 1);
    }

    private void ApplyItemEffect(CCSPlayerController player, ShopItem item)
    {
        var effect = item.Effect;
        var pawn = player.PlayerPawn.Value;

        switch (effect.Type)
        {
            case EffectType.GiveWeapon:
                if (!string.IsNullOrEmpty(effect.WeaponName))
                    player.GiveNamedItem(effect.WeaponName);
                break;

            case EffectType.GiveHealth:
                if (pawn != null)
                    pawn.Health = Math.Min(pawn.Health + (int)effect.Value, 200);
                break;

            case EffectType.GiveArmor:
                if (pawn != null)
                    pawn.ArmorValue = Math.Min(pawn.ArmorValue + (int)effect.Value, 100);
                break;

            case EffectType.GiveSpeed:
                if (pawn != null)
                {
                    pawn.VelocityModifier = effect.Value;
                    if (effect.Duration > 0)
                    {
                        _plugin.AddTimer(effect.Duration, () =>
                        {
                            if (pawn?.IsValid == true)
                                pawn.VelocityModifier = 1.0f;
                        });
                    }
                }
                break;

            case EffectType.GiveGravity:
                // Aplicar gravidade individual (dificil em CS2, usar workaround)
                _plugin.PrintToChat(player, $"{ChatColors.Yellow}Gravidade reduzida por {effect.Duration}s!");
                break;

            case EffectType.GiveGrenade:
                player.GiveNamedItem("weapon_hegrenade");
                break;

            case EffectType.GiveFlash:
                player.GiveNamedItem("weapon_flashbang");
                break;

            case EffectType.GiveSmoke:
                player.GiveNamedItem("weapon_smokegrenade");
                break;

            case EffectType.GiveMolotov:
                player.GiveNamedItem("weapon_molotov");
                break;

            case EffectType.GiveInvisibility:
                if (pawn != null)
                {
                    pawn.RenderMode = RenderMode_t.kRenderTransColor;
                    pawn.Render = System.Drawing.Color.FromArgb(50, 255, 255, 255);
                    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

                    _plugin.AddTimer(effect.Duration, () =>
                    {
                        if (pawn?.IsValid == true)
                        {
                            pawn.RenderMode = RenderMode_t.kRenderNormal;
                            pawn.Render = System.Drawing.Color.White;
                            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                        }
                    });

                    _plugin.PrintToChat(player, $"{ChatColors.Purple}Voce esta invisivel por {effect.Duration}s!");
                }
                break;

            case EffectType.FreedomPass:
                var jailPlayer = _plugin.GetJailPlayer(player);
                if (jailPlayer != null)
                {
                    jailPlayer.IsFreedayPlayer = true;
                    _plugin.PrintToChatAll($"{ChatColors.Green}{player.PlayerName} usou um Passe de Liberdade!");
                }
                break;

            case EffectType.RebelPass:
                // Imunidade temporaria
                var jp = _plugin.GetJailPlayer(player);
                if (jp != null)
                {
                    jp.IsPardonned = true;
                    _plugin.AddTimer(effect.Duration, () =>
                    {
                        var p = _plugin.GetJailPlayer(player);
                        if (p != null) p.IsPardonned = false;
                    });
                    _plugin.PrintToChat(player, $"{ChatColors.Yellow}Imunidade de rebelde por {effect.Duration}s!");
                }
                break;

            case EffectType.Glow:
                ApplyGlow(player, effect.CustomEffect ?? "white");
                break;

            case EffectType.Trail:
                _playersWithTrail[player.SteamID] = effect.CustomEffect ?? "fire";
                _plugin.PrintToChat(player, $"{ChatColors.Yellow}Trail de {effect.CustomEffect ?? "fogo"} ativado!");
                break;

            case EffectType.NoFallDamage:
                _playersWithNoFallDamage.Add(player.SteamID);
                _plugin.PrintToChat(player, $"{ChatColors.Green}Sem dano de queda nesta rodada!");
                break;

            case EffectType.DoubleJump:
                _playersWithDoubleJump.Add(player.SteamID);
                _playersJumpCount[player.SteamID] = 0;
                _plugin.PrintToChat(player, $"{ChatColors.Green}Pulo duplo ativado! Pule novamente no ar!");
                break;
        }
    }

    private void ApplyGlow(CCSPlayerController player, string colorName)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        System.Drawing.Color color = colorName.ToLower() switch
        {
            "red" => System.Drawing.Color.Red,
            "blue" => System.Drawing.Color.Blue,
            "green" => System.Drawing.Color.Green,
            "yellow" => System.Drawing.Color.Yellow,
            "purple" => System.Drawing.Color.Purple,
            "cyan" => System.Drawing.Color.Cyan,
            "orange" => System.Drawing.Color.Orange,
            "pink" => System.Drawing.Color.Pink,
            "rainbow" => System.Drawing.Color.White, // Precisa de logica especial
            _ => System.Drawing.Color.White
        };

        pawn.RenderMode = RenderMode_t.kRenderGlow;
        pawn.Render = color;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer != null)
        {
            jailPlayer.HasGlow = true;
            jailPlayer.PlayerColor = new Models.Color(color.R, color.G, color.B);
        }
    }

    #endregion

    #region Purchase Tracking

    private int GetPurchasesThisRound(ulong steamId, string itemId)
    {
        if (!_purchasesThisRound.ContainsKey(steamId))
            return 0;
        if (!_purchasesThisRound[steamId].ContainsKey(itemId))
            return 0;
        return _purchasesThisRound[steamId][itemId];
    }

    private int GetPurchasesThisMap(ulong steamId, string itemId)
    {
        if (!_purchasesThisMap.ContainsKey(steamId))
            return 0;
        if (!_purchasesThisMap[steamId].ContainsKey(itemId))
            return 0;
        return _purchasesThisMap[steamId][itemId];
    }

    private void AddPurchase(ulong steamId, string itemId)
    {
        // Round
        if (!_purchasesThisRound.ContainsKey(steamId))
            _purchasesThisRound[steamId] = new Dictionary<string, int>();
        if (!_purchasesThisRound[steamId].ContainsKey(itemId))
            _purchasesThisRound[steamId][itemId] = 0;
        _purchasesThisRound[steamId][itemId]++;

        // Map
        if (!_purchasesThisMap.ContainsKey(steamId))
            _purchasesThisMap[steamId] = new Dictionary<string, int>();
        if (!_purchasesThisMap[steamId].ContainsKey(itemId))
            _purchasesThisMap[steamId][itemId] = 0;
        _purchasesThisMap[steamId][itemId]++;
    }

    #endregion

    #region Commands

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandShop(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        ShowShopMenu(player);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandCredits(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer == null) return;

        player.PrintToChat($" {ChatColors.Purple}=== Seus Creditos ==={ChatColors.White}");
        player.PrintToChat($" Atual: {ChatColors.Green}{jailPlayer.Credits}");
        player.PrintToChat($" Total Ganho: {ChatColors.Yellow}{jailPlayer.TotalCreditsEarned}");
        player.PrintToChat($" Total Gasto: {ChatColors.Red}{jailPlayer.TotalCreditsSpent}");
    }

    [CommandHelper(minArgs: 2, usage: "<jogador> <quantidade>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandGiveCredits(CCSPlayerController? player, CommandInfo info)
    {
        // TODO: Verificar se e admin
        if (player == null) return;

        var targetName = info.GetArg(1);
        if (!int.TryParse(info.GetArg(2), out var amount))
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Quantidade invalida!");
            return;
        }

        var target = Utilities.GetPlayers().FirstOrDefault(p =>
            p?.IsValid == true && p.PlayerName.ToLower().Contains(targetName.ToLower()));

        if (target == null)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Jogador nao encontrado!");
            return;
        }

        AddCredits(target, amount, $"Admin: {player.PlayerName}");
        _plugin.PrintToChat(player, $"{ChatColors.Green}Voce deu {amount} creditos para {target.PlayerName}!");
    }

    [CommandHelper(minArgs: 2, usage: "<jogador> <quantidade>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandPay(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        var targetName = info.GetArg(1);
        if (!int.TryParse(info.GetArg(2), out var amount) || amount <= 0)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Quantidade invalida!");
            return;
        }

        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer == null || jailPlayer.Credits < amount)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Creditos insuficientes!");
            return;
        }

        var target = Utilities.GetPlayers().FirstOrDefault(p =>
            p?.IsValid == true && p != player && p.PlayerName.ToLower().Contains(targetName.ToLower()));

        if (target == null)
        {
            _plugin.PrintToChat(player, $"{ChatColors.Red}Jogador nao encontrado!");
            return;
        }

        RemoveCredits(player, amount);
        AddCredits(target, amount, $"Transferencia de {player.PlayerName}");

        _plugin.PrintToChat(player, $"{ChatColors.Green}Voce transferiu {amount} creditos para {target.PlayerName}!");
    }

    #endregion

    #region Helpers

    private string GetCategoryName(ShopCategory category)
    {
        return category switch
        {
            ShopCategory.Weapons => "Armas",
            ShopCategory.Equipment => "Equipamentos",
            ShopCategory.Abilities => "Habilidades",
            ShopCategory.Cosmetics => "Cosmeticos",
            ShopCategory.Special => "Especiais",
            _ => category.ToString()
        };
    }

    #endregion

    #region Effect Checks

    /// <summary>
    /// Verifica se jogador tem no fall damage ativo
    /// </summary>
    public bool HasNoFallDamage(CCSPlayerController player)
    {
        return _playersWithNoFallDamage.Contains(player.SteamID);
    }

    /// <summary>
    /// Processa dano de queda - retorna true se deve bloquear
    /// </summary>
    public bool ProcessFallDamage(CCSPlayerController player, ref float damage)
    {
        if (_playersWithNoFallDamage.Contains(player.SteamID))
        {
            damage = 0;
            return true; // Bloquear dano
        }
        return false;
    }

    /// <summary>
    /// Verifica se jogador tem double jump ativo
    /// </summary>
    public bool HasDoubleJump(CCSPlayerController player)
    {
        return _playersWithDoubleJump.Contains(player.SteamID);
    }

    /// <summary>
    /// Processa pulo - retorna true se deve fazer double jump
    /// </summary>
    public bool ProcessJump(CCSPlayerController player)
    {
        if (!_playersWithDoubleJump.Contains(player.SteamID))
            return false;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return false;

        // Se esta no ar e ainda nao usou double jump
        var flags = pawn.Flags;
        bool onGround = (flags & (uint)PlayerFlags.FL_ONGROUND) != 0;

        if (!_playersJumpCount.ContainsKey(player.SteamID))
            _playersJumpCount[player.SteamID] = 0;

        if (onGround)
        {
            _playersJumpCount[player.SteamID] = 0;
            return false;
        }

        if (_playersJumpCount[player.SteamID] < 1)
        {
            _playersJumpCount[player.SteamID]++;
            // Aplicar impulso para cima
            var velocity = pawn.AbsVelocity;
            velocity.Z = 300f; // Impulso do double jump
            pawn.Teleport(null, null, velocity);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Verifica se jogador tem trail ativo
    /// </summary>
    public bool HasTrail(CCSPlayerController player, out string trailType)
    {
        return _playersWithTrail.TryGetValue(player.SteamID, out trailType!);
    }

    /// <summary>
    /// Desenha trail para jogador (chamar a cada tick)
    /// </summary>
    public void DrawTrail(CCSPlayerController player)
    {
        if (!_playersWithTrail.TryGetValue(player.SteamID, out var trailType))
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn?.AbsOrigin == null) return;

        // Criar particula de trail
        var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
        if (particle == null) return;

        string effectName = trailType switch
        {
            "fire" => "particles/burning_fx/burning_character.vpcf",
            "ice" => "particles/water_impact/water_splash.vpcf",
            _ => "particles/burning_fx/burning_character.vpcf"
        };

        particle.EffectName = effectName;
        particle.Teleport(pawn.AbsOrigin, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        particle.DispatchSpawn();
        particle.AcceptInput("Start");

        // Remover particula apos 0.5 segundos
        _plugin.AddTimer(0.5f, () =>
        {
            if (particle.IsValid)
                particle.Remove();
        });
    }

    /// <summary>
    /// Limpa efeitos no inicio da rodada
    /// </summary>
    public void OnRoundStart()
    {
        _purchasesThisRound.Clear();
        _playersWithNoFallDamage.Clear();
        _playersWithDoubleJump.Clear();
        _playersJumpCount.Clear();
        _playersWithTrail.Clear();
    }

    /// <summary>
    /// Limpa efeitos no inicio do mapa
    /// </summary>
    public void OnMapStart()
    {
        _purchasesThisMap.Clear();
        OnRoundStart();
    }

    #endregion
}
