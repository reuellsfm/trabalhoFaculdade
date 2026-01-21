using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SuperJailbreak.Core;

namespace SuperJailbreak.Services;

/// <summary>
/// Servico de localizacao/traducao
/// Suporta multiplos idiomas para mensagens do plugin
/// </summary>
public class LocalizerService
{
    private readonly SuperJailbreakPlugin _plugin;
    private Dictionary<string, string> _translations = new();
    private readonly string _language;

    public LocalizerService(SuperJailbreakPlugin plugin, string language)
    {
        _plugin = plugin;
        _language = language;
        LoadTranslations();
    }

    private void LoadTranslations()
    {
        var langPath = Path.Combine(_plugin.ModulePath, "..", "lang", $"{_language}.json");

        if (File.Exists(langPath))
        {
            try
            {
                var json = File.ReadAllText(langPath);
                _translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new();
            }
            catch (Exception ex)
            {
                _plugin.Logger.LogError($"[Localizer] Erro ao carregar traducoes: {ex.Message}");
                LoadDefaultTranslations();
            }
        }
        else
        {
            LoadDefaultTranslations();
        }
    }

    private void LoadDefaultTranslations()
    {
        // Traducoes padrao em Portugues
        _translations = new Dictionary<string, string>
        {
            // General
            ["prefix"] = "[SuperJailbreak]",
            ["error_not_alive"] = "Voce precisa estar vivo!",
            ["error_not_ct"] = "Apenas CTs podem usar este comando!",
            ["error_not_t"] = "Apenas Terroristas podem usar este comando!",
            ["error_not_warden"] = "Apenas o Warden pode usar este comando!",
            ["error_already_warden"] = "Ja existe um Warden!",
            ["error_not_enough_credits"] = "Creditos insuficientes!",

            // Warden
            ["warden_became"] = "{0} e agora o WARDEN!",
            ["warden_left"] = "{0} nao e mais o Warden!",
            ["warden_died"] = "O Warden {0} MORREU!",
            ["warden_cells_opened"] = "Celas abertas pelo Warden!",
            ["warden_block_on"] = "Colisao entre jogadores ATIVADA!",
            ["warden_block_off"] = "Colisao entre jogadores DESATIVADA!",

            // Rebel
            ["rebel_marked"] = "{0} e agora um REBELDE!",
            ["rebel_pardoned"] = "{0} foi perdoado!",

            // Last Request
            ["lr_available"] = "=== LAST REQUEST DISPONIVEL ===",
            ["lr_started"] = "=== LAST REQUEST ===",
            ["lr_winner"] = "Vencedor: {0}",
            ["lr_timeout"] = "Tempo para LR expirou!",

            // Special Days
            ["sd_started"] = "=== SPECIAL DAY ===",
            ["sd_ended"] = "Special Day finalizado!",
            ["sd_freeday"] = "FREEDAY! Prisioneiros livres!",
            ["sd_warday"] = "WARDAY! Guardas em posicao!",
            ["sd_warday_expand"] = "WARDAY EXPANDIDO! Guardas podem sair!",
            ["sd_hns_hide"] = "Terroristas tem 60 segundos para se esconder!",
            ["sd_hns_seek"] = "CTs liberados! CACADA COMECOU!",

            // Economy
            ["credits_added"] = "+{0} creditos ({1}) | Total: {2}",
            ["credits_removed"] = "-{0} creditos | Total: {1}",
            ["shop_purchased"] = "Voce comprou {0}!",

            // Gangs
            ["gang_created"] = "[{0}] {1} foi criada por {2}!",
            ["gang_joined"] = "{0} entrou na gang!",
            ["gang_left"] = "Voce saiu da gang!",
            ["gang_invite"] = "Voce foi convidado para a gang [{0}] {1}!",
            ["gang_donated"] = "Voce doou {0} creditos para a gang!",

            // Achievements
            ["achievement_unlocked"] = "[ACHIEVEMENT] {0} desbloqueou: {1}",

            // Misc
            ["freeday_given"] = "{0} recebeu FREEDAY!",
            ["all_guards_dead"] = "Todos os guardas morreram! FREEDAY!"
        };
    }

    public string Get(string key, params object[] args)
    {
        if (_translations.TryGetValue(key, out var translation))
        {
            return args.Length > 0 ? string.Format(translation, args) : translation;
        }

        return key;
    }

    public string this[string key] => Get(key);

    public void SetTranslation(string key, string value)
    {
        _translations[key] = value;
    }
}
