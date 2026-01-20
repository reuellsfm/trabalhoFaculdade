using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using SuperJailbreak.Models;

namespace SuperJailbreak.Services;

/// <summary>
/// Servico de banco de dados MySQL com Dapper
/// Gerencia persistencia de jogadores, gangs e estatisticas
/// </summary>
public class DatabaseService : IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger _logger;
    private MySqlConnection? _connection;

    public DatabaseService(string host, int port, string database, string user, string password, ILogger logger)
    {
        _connectionString = $"Server={host};Port={port};Database={database};User={user};Password={password};";
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _connection = new MySqlConnection(_connectionString);
            await _connection.OpenAsync();

            // Criar tabelas se nao existirem
            await CreateTablesAsync();

            _logger.LogInformation("[Database] Conexao estabelecida com sucesso!");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Database] Erro ao conectar: {ex.Message}");
        }
    }

    private async Task CreateTablesAsync()
    {
        var createPlayersTable = @"
            CREATE TABLE IF NOT EXISTS jb_players (
                steam_id BIGINT UNSIGNED PRIMARY KEY,
                name VARCHAR(64) NOT NULL,
                credits INT DEFAULT 100,
                total_credits_earned INT DEFAULT 0,
                total_credits_spent INT DEFAULT 0,
                lr_wins INT DEFAULT 0,
                lr_losses INT DEFAULT 0,
                times_warden INT DEFAULT 0,
                rounds_played INT DEFAULT 0,
                times_rebelled INT DEFAULT 0,
                guards_killed INT DEFAULT 0,
                prisoners_killed INT DEFAULT 0,
                achievement_points INT DEFAULT 0,
                unlocked_achievements TEXT,
                gang_id INT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                INDEX idx_gang_id (gang_id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ";

        var createGangsTable = @"
            CREATE TABLE IF NOT EXISTS jb_gangs (
                id INT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(64) NOT NULL UNIQUE,
                tag VARCHAR(10) NOT NULL,
                description VARCHAR(255),
                leader_steam_id BIGINT UNSIGNED NOT NULL,
                total_credits INT DEFAULT 0,
                level INT DEFAULT 1,
                experience INT DEFAULT 0,
                max_members INT DEFAULT 10,
                unlocked_perks TEXT,
                gang_color VARCHAR(20),
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                INDEX idx_leader (leader_steam_id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ";

        var createGangMembersTable = @"
            CREATE TABLE IF NOT EXISTS jb_gang_members (
                steam_id BIGINT UNSIGNED PRIMARY KEY,
                gang_id INT NOT NULL,
                name VARCHAR(64) NOT NULL,
                rank TINYINT DEFAULT 0,
                contributed_credits INT DEFAULT 0,
                joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (gang_id) REFERENCES jb_gangs(id) ON DELETE CASCADE,
                INDEX idx_gang_id (gang_id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ";

        var createStatsTable = @"
            CREATE TABLE IF NOT EXISTS jb_statistics (
                id INT AUTO_INCREMENT PRIMARY KEY,
                steam_id BIGINT UNSIGNED NOT NULL,
                stat_type VARCHAR(32) NOT NULL,
                stat_value INT DEFAULT 0,
                recorded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                INDEX idx_steam_stat (steam_id, stat_type)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ";

        if (_connection != null)
        {
            await _connection.ExecuteAsync(createPlayersTable);
            await _connection.ExecuteAsync(createGangsTable);
            await _connection.ExecuteAsync(createGangMembersTable);
            await _connection.ExecuteAsync(createStatsTable);

            _logger.LogInformation("[Database] Tabelas criadas/verificadas com sucesso!");
        }
    }

    #region Player Operations

    public async Task LoadPlayerAsync(JailPlayer player)
    {
        if (_connection == null || player.SteamId == 0) return;

        try
        {
            var data = await _connection.QueryFirstOrDefaultAsync<PlayerData>(
                "SELECT * FROM jb_players WHERE steam_id = @SteamId",
                new { SteamId = player.SteamId }
            );

            if (data != null)
            {
                player.Credits = data.Credits;
                player.TotalCreditsEarned = data.TotalCreditsEarned;
                player.TotalCreditsSpent = data.TotalCreditsSpent;
                player.LRWins = data.LRWins;
                player.LRLosses = data.LRLosses;
                player.TimesWarden = data.TimesWarden;
                player.RoundsPlayed = data.RoundsPlayed;
                player.TimesRebelled = data.TimesRebelled;
                player.GuardsKilled = data.GuardsKilled;
                player.PrisonersKilled = data.PrisonersKilled;
                player.AchievementPoints = data.AchievementPoints;
                player.GangId = data.GangId;

                // Parse achievements
                if (!string.IsNullOrEmpty(data.UnlockedAchievements))
                {
                    player.UnlockedAchievements = data.UnlockedAchievements.Split(',').ToList();
                }

                _logger.LogInformation($"[Database] Jogador carregado: {player.Name} ({player.SteamId})");
            }
            else
            {
                // Criar novo jogador
                await CreatePlayerAsync(player);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Database] Erro ao carregar jogador: {ex.Message}");
        }
    }

    public async Task SavePlayerAsync(JailPlayer player)
    {
        if (_connection == null || player.SteamId == 0) return;

        try
        {
            var achievements = string.Join(",", player.UnlockedAchievements);

            await _connection.ExecuteAsync(@"
                INSERT INTO jb_players (steam_id, name, credits, total_credits_earned, total_credits_spent,
                    lr_wins, lr_losses, times_warden, rounds_played, times_rebelled,
                    guards_killed, prisoners_killed, achievement_points, unlocked_achievements, gang_id)
                VALUES (@SteamId, @Name, @Credits, @TotalEarned, @TotalSpent,
                    @LRWins, @LRLosses, @TimesWarden, @RoundsPlayed, @TimesRebelled,
                    @GuardsKilled, @PrisonersKilled, @AchievementPoints, @Achievements, @GangId)
                ON DUPLICATE KEY UPDATE
                    name = @Name,
                    credits = @Credits,
                    total_credits_earned = @TotalEarned,
                    total_credits_spent = @TotalSpent,
                    lr_wins = @LRWins,
                    lr_losses = @LRLosses,
                    times_warden = @TimesWarden,
                    rounds_played = @RoundsPlayed,
                    times_rebelled = @TimesRebelled,
                    guards_killed = @GuardsKilled,
                    prisoners_killed = @PrisonersKilled,
                    achievement_points = @AchievementPoints,
                    unlocked_achievements = @Achievements,
                    gang_id = @GangId",
                new
                {
                    SteamId = player.SteamId,
                    Name = player.Name,
                    Credits = player.Credits,
                    TotalEarned = player.TotalCreditsEarned,
                    TotalSpent = player.TotalCreditsSpent,
                    LRWins = player.LRWins,
                    LRLosses = player.LRLosses,
                    TimesWarden = player.TimesWarden,
                    RoundsPlayed = player.RoundsPlayed,
                    TimesRebelled = player.TimesRebelled,
                    GuardsKilled = player.GuardsKilled,
                    PrisonersKilled = player.PrisonersKilled,
                    AchievementPoints = player.AchievementPoints,
                    Achievements = achievements,
                    GangId = player.GangId
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Database] Erro ao salvar jogador: {ex.Message}");
        }
    }

    private async Task CreatePlayerAsync(JailPlayer player)
    {
        if (_connection == null) return;

        await _connection.ExecuteAsync(@"
            INSERT INTO jb_players (steam_id, name, credits)
            VALUES (@SteamId, @Name, @Credits)",
            new { SteamId = player.SteamId, Name = player.Name, Credits = player.Credits }
        );

        _logger.LogInformation($"[Database] Novo jogador criado: {player.Name} ({player.SteamId})");
    }

    #endregion

    #region Gang Operations

    public async Task<List<Gang>> LoadGangsAsync()
    {
        var gangs = new List<Gang>();

        if (_connection == null) return gangs;

        try
        {
            var gangData = await _connection.QueryAsync<GangData>("SELECT * FROM jb_gangs");

            foreach (var data in gangData)
            {
                var gang = new Gang
                {
                    Id = data.Id,
                    Name = data.Name,
                    Tag = data.Tag,
                    Description = data.Description ?? string.Empty,
                    LeaderSteamId = data.LeaderSteamId,
                    TotalCredits = data.TotalCredits,
                    Level = data.Level,
                    Experience = data.Experience,
                    MaxMembers = data.MaxMembers,
                    CreatedAt = data.CreatedAt
                };

                // Carregar membros
                var members = await _connection.QueryAsync<GangMemberData>(
                    "SELECT * FROM jb_gang_members WHERE gang_id = @GangId",
                    new { GangId = gang.Id }
                );

                foreach (var member in members)
                {
                    gang.Members.Add(new GangMember
                    {
                        SteamId = member.SteamId,
                        Name = member.Name,
                        Rank = (GangRank)member.Rank,
                        ContributedCredits = member.ContributedCredits,
                        JoinedAt = member.JoinedAt
                    });
                }

                // Parse perks
                if (!string.IsNullOrEmpty(data.UnlockedPerks))
                {
                    // TODO: Parse perks do JSON
                }

                gangs.Add(gang);
            }

            _logger.LogInformation($"[Database] {gangs.Count} gangs carregadas");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Database] Erro ao carregar gangs: {ex.Message}");
        }

        return gangs;
    }

    public async Task SaveGangAsync(Gang gang)
    {
        if (_connection == null) return;

        try
        {
            // Salvar gang
            await _connection.ExecuteAsync(@"
                INSERT INTO jb_gangs (id, name, tag, description, leader_steam_id, total_credits, level, experience, max_members)
                VALUES (@Id, @Name, @Tag, @Description, @LeaderSteamId, @TotalCredits, @Level, @Experience, @MaxMembers)
                ON DUPLICATE KEY UPDATE
                    name = @Name,
                    tag = @Tag,
                    description = @Description,
                    leader_steam_id = @LeaderSteamId,
                    total_credits = @TotalCredits,
                    level = @Level,
                    experience = @Experience,
                    max_members = @MaxMembers",
                new
                {
                    Id = gang.Id,
                    Name = gang.Name,
                    Tag = gang.Tag,
                    Description = gang.Description,
                    LeaderSteamId = gang.LeaderSteamId,
                    TotalCredits = gang.TotalCredits,
                    Level = gang.Level,
                    Experience = gang.Experience,
                    MaxMembers = gang.MaxMembers
                }
            );

            // Salvar membros
            foreach (var member in gang.Members)
            {
                await _connection.ExecuteAsync(@"
                    INSERT INTO jb_gang_members (steam_id, gang_id, name, rank, contributed_credits)
                    VALUES (@SteamId, @GangId, @Name, @Rank, @ContributedCredits)
                    ON DUPLICATE KEY UPDATE
                        gang_id = @GangId,
                        name = @Name,
                        rank = @Rank,
                        contributed_credits = @ContributedCredits",
                    new
                    {
                        SteamId = member.SteamId,
                        GangId = gang.Id,
                        Name = member.Name,
                        Rank = (int)member.Rank,
                        ContributedCredits = member.ContributedCredits
                    }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Database] Erro ao salvar gang: {ex.Message}");
        }
    }

    public async Task DeleteGangAsync(int gangId)
    {
        if (_connection == null) return;

        try
        {
            await _connection.ExecuteAsync("DELETE FROM jb_gangs WHERE id = @Id", new { Id = gangId });
            _logger.LogInformation($"[Database] Gang {gangId} deletada");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Database] Erro ao deletar gang: {ex.Message}");
        }
    }

    #endregion

    #region Statistics

    public async Task RecordStatisticAsync(ulong steamId, string statType, int value)
    {
        if (_connection == null) return;

        try
        {
            await _connection.ExecuteAsync(@"
                INSERT INTO jb_statistics (steam_id, stat_type, stat_value)
                VALUES (@SteamId, @StatType, @Value)",
                new { SteamId = steamId, StatType = statType, Value = value }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Database] Erro ao registrar estatistica: {ex.Message}");
        }
    }

    public async Task<List<TopPlayer>> GetTopPlayersAsync(string statField, int limit = 10)
    {
        var players = new List<TopPlayer>();

        if (_connection == null) return players;

        try
        {
            var query = $"SELECT steam_id, name, {statField} as value FROM jb_players ORDER BY {statField} DESC LIMIT @Limit";
            var results = await _connection.QueryAsync<TopPlayer>(query, new { Limit = limit });
            players = results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Database] Erro ao buscar top players: {ex.Message}");
        }

        return players;
    }

    #endregion

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}

#region Data Classes

public class PlayerData
{
    public ulong SteamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int TotalCreditsEarned { get; set; }
    public int TotalCreditsSpent { get; set; }
    public int LRWins { get; set; }
    public int LRLosses { get; set; }
    public int TimesWarden { get; set; }
    public int RoundsPlayed { get; set; }
    public int TimesRebelled { get; set; }
    public int GuardsKilled { get; set; }
    public int PrisonersKilled { get; set; }
    public int AchievementPoints { get; set; }
    public string? UnlockedAchievements { get; set; }
    public int? GangId { get; set; }
}

public class GangData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ulong LeaderSteamId { get; set; }
    public int TotalCredits { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public int MaxMembers { get; set; }
    public string? UnlockedPerks { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GangMemberData
{
    public ulong SteamId { get; set; }
    public int GangId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int ContributedCredits { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class TopPlayer
{
    public ulong SteamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

#endregion
