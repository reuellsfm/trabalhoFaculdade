using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using SuperJailbreak.Core;
using SuperJailbreak.Models;

namespace SuperJailbreak.Modules.Achievements;

/// <summary>
/// Modulo de Achievements/Conquistas completo
/// Rastreia progresso e desbloqueia recompensas
/// </summary>
public class AchievementsModule
{
    private readonly SuperJailbreakPlugin _plugin;
    private readonly List<Achievement> _achievements;

    public AchievementsModule(SuperJailbreakPlugin plugin)
    {
        _plugin = plugin;
        _achievements = AchievementList.GetAll();
    }

    public void Initialize()
    {
        RegisterCommands();
    }

    public void Unload() { }

    private void RegisterCommands()
    {
        _plugin.AddCommand("css_achievements", "Ver achievements", CommandAchievements);
        _plugin.AddCommand("css_conquistas", "Ver achievements", CommandAchievements);
        _plugin.AddCommand("css_ach", "Ver achievements", CommandAchievements);
    }

    #region Achievement Checking

    public void CheckAchievement(CCSPlayerController player, AchievementType type, int currentValue)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer == null) return;

        // Encontrar achievements deste tipo que ainda nao foram desbloqueados
        var eligibleAchievements = _achievements
            .Where(a => a.Requirement.Type == type &&
                       !jailPlayer.UnlockedAchievements.Contains(a.Id) &&
                       currentValue >= a.Requirement.RequiredCount)
            .ToList();

        foreach (var achievement in eligibleAchievements)
        {
            UnlockAchievement(player, achievement);
        }
    }

    public void OnPlayerKill(CCSPlayerController killer, CCSPlayerController victim, bool headshot, string weapon)
    {
        var jailPlayer = _plugin.GetJailPlayer(killer);
        if (jailPlayer == null) return;

        // Kill guards
        if (victim.Team == CsTeam.CounterTerrorist)
        {
            jailPlayer.GuardsKilled++;
            CheckAchievement(killer, AchievementType.KillGuards, jailPlayer.GuardsKilled);

            // Kill warden as rebel
            if (jailPlayer.IsRebel && _plugin.Warden?.IsWardenPlayer(victim) == true)
            {
                CheckAchievement(killer, AchievementType.KillWardenAsRebel, 1);
            }
        }

        // Kill prisoners
        if (victim.Team == CsTeam.Terrorist)
        {
            jailPlayer.PrisonersKilled++;
            CheckAchievement(killer, AchievementType.KillPrisoners, jailPlayer.PrisonersKilled);
        }

        // Headshots (contador global seria necessario, usando placeholder)
        if (headshot)
        {
            // TODO: Contador global de headshots
            CheckAchievement(killer, AchievementType.Headshots, 1);
        }

        // Knife kills
        if (weapon.Contains("knife"))
        {
            CheckAchievement(killer, AchievementType.KnifeKills, 1);
        }

        // First blood - verificar se e o primeiro kill da rodada
        // TODO: Implementar verificacao de first blood
    }

    private void UnlockAchievement(CCSPlayerController player, Achievement achievement)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer == null) return;

        // Registrar conquista
        jailPlayer.UnlockedAchievements.Add(achievement.Id);
        jailPlayer.AchievementPoints += achievement.Points;

        // Dar recompensa de creditos
        if (achievement.CreditReward > 0)
        {
            _plugin.Economy?.AddCredits(player, achievement.CreditReward, $"Achievement: {achievement.Name}");
        }

        // Anunciar
        if (_plugin.Config.AchievementAnnouncements)
        {
            var rarityColor = achievement.Rarity switch
            {
                AchievementRarity.Common => ChatColors.White,
                AchievementRarity.Uncommon => ChatColors.Green,
                AchievementRarity.Rare => ChatColors.Blue,
                AchievementRarity.Epic => ChatColors.Purple,
                AchievementRarity.Legendary => ChatColors.Gold,
                _ => ChatColors.White
            };

            _plugin.PrintToChatAll($"{ChatColors.Yellow}[ACHIEVEMENT]{ChatColors.White} {player.PlayerName} desbloqueou: {rarityColor}{achievement.IconEmoji} {achievement.Name}");
        }

        // Notificar jogador
        player.PrintToCenter($"ACHIEVEMENT: {achievement.Name}!");

        _plugin.Logger.LogInformation($"[Achievements] {player.PlayerName} desbloqueou: {achievement.Name}");
    }

    #endregion

    #region Progress Tracking

    public float GetAchievementProgress(CCSPlayerController player, Achievement achievement)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        if (jailPlayer == null) return 0;

        // Se ja desbloqueou, 100%
        if (jailPlayer.UnlockedAchievements.Contains(achievement.Id))
            return 1.0f;

        // Calcular progresso baseado no tipo
        int currentValue = achievement.Requirement.Type switch
        {
            AchievementType.KillGuards => jailPlayer.GuardsKilled,
            AchievementType.KillPrisoners => jailPlayer.PrisonersKilled,
            AchievementType.WinLR => jailPlayer.LRWins,
            AchievementType.WinLRStreak => jailPlayer.LRStreak,
            AchievementType.BecomeWarden => jailPlayer.TimesWarden,
            AchievementType.SuccessfulRebellion => jailPlayer.TimesRebelled,
            AchievementType.EarnCredits => jailPlayer.TotalCreditsEarned,
            AchievementType.SpendCredits => jailPlayer.TotalCreditsSpent,
            AchievementType.PlayRounds => jailPlayer.RoundsPlayed,
            _ => 0
        };

        return Math.Min(1.0f, (float)currentValue / achievement.Requirement.RequiredCount);
    }

    public int GetTotalPoints(CCSPlayerController player)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        return jailPlayer?.AchievementPoints ?? 0;
    }

    public int GetUnlockedCount(CCSPlayerController player)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        return jailPlayer?.UnlockedAchievements.Count ?? 0;
    }

    #endregion

    #region Commands

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void CommandAchievements(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        ShowAchievementsMenu(player);
    }

    #endregion

    #region Menus

    public void ShowAchievementsMenu(CCSPlayerController player)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        var menu = new ChatMenu("Achievements");

        var unlockedCount = GetUnlockedCount(player);
        var totalCount = _achievements.Count;
        var points = GetTotalPoints(player);

        menu.AddMenuOption($"Progresso: {unlockedCount}/{totalCount} | Pontos: {points}", (p, o) => { }, true);
        menu.AddMenuOption("Combat", (p, o) => ShowCategoryMenu(p, AchievementCategory.Combat));
        menu.AddMenuOption("Last Request", (p, o) => ShowCategoryMenu(p, AchievementCategory.LastRequest));
        menu.AddMenuOption("Warden", (p, o) => ShowCategoryMenu(p, AchievementCategory.Warden));
        menu.AddMenuOption("Rebel", (p, o) => ShowCategoryMenu(p, AchievementCategory.Rebel));
        menu.AddMenuOption("Economy", (p, o) => ShowCategoryMenu(p, AchievementCategory.Economy));
        menu.AddMenuOption("Social", (p, o) => ShowCategoryMenu(p, AchievementCategory.Social));
        menu.AddMenuOption("Special", (p, o) => ShowCategoryMenu(p, AchievementCategory.Special));

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowCategoryMenu(CCSPlayerController player, AchievementCategory category)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        var menu = new ChatMenu($"Achievements - {GetCategoryName(category)}");

        var categoryAchievements = _achievements
            .Where(a => a.Category == category && (!a.IsSecret || jailPlayer?.UnlockedAchievements.Contains(a.Id) == true))
            .ToList();

        foreach (var achievement in categoryAchievements)
        {
            var unlocked = jailPlayer?.UnlockedAchievements.Contains(achievement.Id) == true;
            var progress = GetAchievementProgress(player, achievement);
            var progressStr = unlocked ? "[OK]" : $"[{(int)(progress * 100)}%]";

            var rarityColor = achievement.Rarity switch
            {
                AchievementRarity.Common => ChatColors.White,
                AchievementRarity.Uncommon => ChatColors.Green,
                AchievementRarity.Rare => ChatColors.Blue,
                AchievementRarity.Epic => ChatColors.Purple,
                AchievementRarity.Legendary => ChatColors.Gold,
                _ => ChatColors.White
            };

            var displayName = unlocked
                ? $"{rarityColor}{achievement.IconEmoji} {achievement.Name} {progressStr}"
                : $"{ChatColors.Grey}{achievement.Name} {progressStr}";

            menu.AddMenuOption(displayName, (p, o) => ShowAchievementDetails(p, achievement));
        }

        menu.AddMenuOption("Voltar", (p, o) => ShowAchievementsMenu(p));

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowAchievementDetails(CCSPlayerController player, Achievement achievement)
    {
        var jailPlayer = _plugin.GetJailPlayer(player);
        var unlocked = jailPlayer?.UnlockedAchievements.Contains(achievement.Id) == true;
        var progress = GetAchievementProgress(player, achievement);

        player.PrintToChat($" {ChatColors.Purple}=== {achievement.Name} ==={ChatColors.White}");
        player.PrintToChat($" {achievement.IconEmoji} {achievement.Description}");
        player.PrintToChat($" Raridade: {GetRarityName(achievement.Rarity)}");
        player.PrintToChat($" Pontos: {achievement.Points} | Recompensa: {achievement.CreditReward}c");

        if (unlocked)
        {
            player.PrintToChat($" {ChatColors.Green}Status: DESBLOQUEADO!");
        }
        else
        {
            player.PrintToChat($" Progresso: {(int)(progress * 100)}%");
        }
    }

    #endregion

    #region Helpers

    private string GetCategoryName(AchievementCategory category)
    {
        return category switch
        {
            AchievementCategory.Combat => "Combate",
            AchievementCategory.LastRequest => "Last Request",
            AchievementCategory.Warden => "Warden",
            AchievementCategory.Rebel => "Rebelde",
            AchievementCategory.Economy => "Economia",
            AchievementCategory.Social => "Social",
            AchievementCategory.Special => "Especial",
            AchievementCategory.Seasonal => "Sazonal",
            _ => category.ToString()
        };
    }

    private string GetRarityName(AchievementRarity rarity)
    {
        return rarity switch
        {
            AchievementRarity.Common => $"{ChatColors.White}Comum",
            AchievementRarity.Uncommon => $"{ChatColors.Green}Incomum",
            AchievementRarity.Rare => $"{ChatColors.Blue}Raro",
            AchievementRarity.Epic => $"{ChatColors.Purple}Epico",
            AchievementRarity.Legendary => $"{ChatColors.Gold}Lendario",
            _ => rarity.ToString()
        };
    }

    #endregion
}
