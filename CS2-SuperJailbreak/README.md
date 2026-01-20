# Super Jailbreak - Plugin Revolucionario para CS2

Plugin completo e revolucionario de Jailbreak para Counter-Strike 2, desenvolvido com **CounterStrikeSharp**.

## Funcionalidades

### Sistema de Warden
- Tornar-se Warden com comandos (!w, !warden)
- Laser pointer configuravel
- Sistema de Paint/Marcadores
- Block/Unblock de jogadores
- Abertura/Fechamento de celas
- Warday com timer de expansao
- Freeday individual para jogadores
- Perdao de rebeldes
- Menu completo de armas para CTs
- Glow visual para identificacao

### Sistema de Last Request (15+ Jogos)
- **Knife Fight** - Luta de facas classica
- **No Scope** - AWP sem mira
- **Shot 4 Shot** - Um tiro de cada vez
- **Mag 4 Mag** - Um pente de cada vez
- **Dodgeball** - Queimada com decoys
- **Gun Toss** - Lance a arma mais longe
- **Grenade Battle** - Batalha de granadas
- **Russian Roulette** - Roleta russa com chance real
- **Headshot Only** - Apenas headshots causam dano
- **Scout + Knife** - Scout sem scope e faca
- **Shotgun War** - Batalha de shotguns
- **Deagle Duel** - Duelo de Deagles
- **Race** - Corrida ate o objetivo
- **Math Challenge** - Desafio matematico
- **Hot Potato** - Batata quente com timer
- **Rebellion** - Rebeliao armada

### Special Days (16+ Eventos)
- **Freeday** - Dia livre para prisioneiros
- **Gravity Freeday** - Freeday com gravidade baixa
- **Warday** - Dia de guerra com localizacao
- **Hide and Seek** - Esconde-esconde
- **Zombie Day** - Apocalipse zumbi
- **Gun Game** - Mate para avancar de arma
- **Dodgeball** - Queimada em equipe
- **Headshot Only** - Apenas headshots
- **Knife Fight** - Luta de facas geral
- **No Scope** - AWP sem mira
- **Battle Royale** - Todos contra todos com zona
- **One in the Chamber** - Uma bala, uma chance
- **Golden Knife** - Faca dourada que mata em 1 hit
- **Simon Says** - Siga as ordens do Simon
- **Hot Potato** - Batata quente em grupo
- **Freeze Tag** - Pique-congela

### Sistema de Economia
- Creditos por rodada, kills e LR wins
- Loja completa com categorias:
  - **Armas** - Deagle, granadas, etc
  - **Equipamentos** - HP, Armor, etc
  - **Habilidades** - Speed, Invisibilidade, etc
  - **Cosmeticos** - Glows, Trails
  - **Especiais** - Passe de Liberdade, Respawn
- Transferencia de creditos entre jogadores
- Limite de compras por rodada/mapa

### Sistema de Gangs
- Criacao de gangs com nome e tag
- Sistema de ranks (Membro, Oficial, Co-Lider, Lider)
- Banco da gang para acumular creditos
- Sistema de niveis e experiencia
- Perks desbloqueaveis:
  - Bonus de creditos
  - Bonus de HP/Armor
  - Bonus de velocidade
  - Granada/Flash ao spawnar
- Chat privado da gang
- Top 10 gangs

### Sistema de Achievements
- 20+ conquistas em 7 categorias
- Sistema de raridade (Comum a Lendario)
- Recompensas em creditos
- Progresso rastreavel
- Anuncios globais ao desbloquear

### Sistema de Rebeldes
- Marcacao automatica ao atacar guardas
- Marcacao ao pegar armas
- Glow visual vermelho
- Perdao pelo Warden
- Estatisticas de rebeliao

## Comandos

### Warden
| Comando | Descricao |
|---------|-----------|
| !w, !warden | Tornar-se Warden |
| !uw, !unwarden | Deixar de ser Warden |
| !open | Abrir celas |
| !close | Fechar celas |
| !wb | Ativar colisao (block) |
| !wub | Desativar colisao (noblock) |
| !wd, !warday | Iniciar Warday |
| !fd, !freeday | Dar freeday a jogador |
| !pardon | Perdoar rebelde |
| !laser | Toggle laser |
| !paint | Adicionar marcador |
| !clearpaint | Limpar marcadores |
| !guns | Menu de armas (CT) |

### Last Request
| Comando | Descricao |
|---------|-----------|
| !lr, !lastrequest | Menu de Last Request |
| !stoplr | Cancelar LRs (Admin) |

### Special Days
| Comando | Descricao |
|---------|-----------|
| !sd, !specialday | Menu de Special Days |
| !fd | Iniciar Freeday |
| !hns | Iniciar Hide and Seek |
| !zombie | Iniciar Zombie Day |
| !gg | Iniciar Gun Game |
| !br | Iniciar Battle Royale |

### Economia
| Comando | Descricao |
|---------|-----------|
| !shop, !loja | Abrir loja |
| !credits, !creditos | Ver creditos |
| !pay | Transferir creditos |

### Gangs
| Comando | Descricao |
|---------|-----------|
| !gang, !gangs | Menu de gangs |
| !gangcreate | Criar gang |
| !ganginvite | Convidar jogador |
| !gangaccept | Aceitar convite |
| !gangleave | Sair da gang |
| !gc, !gangchat | Chat da gang |
| !gangtop | Top 10 gangs |

### Outros
| Comando | Descricao |
|---------|-----------|
| !jb, !jailbreak | Menu principal |
| !rules | Ver regras |
| !help | Ajuda |
| !achievements | Ver conquistas |
| !stats | Ver estatisticas |

## Instalacao

### Requisitos
- Counter-Strike 2 Dedicated Server
- [Metamod:Source 2.x](https://www.sourcemm.net/downloads.php/?branch=master)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.300+
- MySQL/MariaDB (opcional, para persistencia)
- libicu / icu-libs (Linux)

### Estrutura de Pastas Correta
```
cs2server/
└── game/
    └── csgo/
        ├── gameinfo.gi          <-- EDITAR ESTE ARQUIVO!
        └── addons/
            ├── metamod/         <-- Metamod:Source
            │   └── metaplugins.ini
            └── counterstrikesharp/
                ├── api/
                ├── configs/
                └── plugins/
                    └── SuperJailbreak/    <-- NOSSO PLUGIN
                        ├── SuperJailbreak.dll
                        ├── config.json
                        └── lang/
```

### Passo 1: Instalar Metamod:Source
[Documentação oficial](https://wiki.alliedmods.net/Installing_metamod:source)

1. Baixe o Metamod 2.x para CS2: https://www.sourcemm.net/downloads.php/?branch=master
2. Extraia a pasta `addons` para `game/csgo/`
3. **IMPORTANTE:** Edite o arquivo `game/csgo/gameinfo.gi`
4. Adicione esta linha **LOGO APÓS** `Game_LowViolence csgo_lv`:
```
			Game	csgo/addons/metamod
```

**Exemplo do gameinfo.gi:**
```
SearchPaths
{
    Game_LowViolence	csgo_lv
    Game	csgo/addons/metamod    <-- ADICIONAR ESTA LINHA
    Game	csgo
    ...
}
```

> ⚠️ **AVISO:** O CS2 sobrescreve este arquivo em cada update! Use o script `scripts/fix-gameinfo.sh`

### Passo 2: Instalar CounterStrikeSharp
[Documentação oficial](https://github.com/roflmuffin/CounterStrikeSharp/blob/main/INSTALL.md)

1. Baixe a versão `with-runtime`: https://github.com/roflmuffin/CounterStrikeSharp/releases
2. Extraia para `game/csgo/` (vai fazer merge com a pasta addons existente)
3. No Linux, instale a dependência: `apt install libicu-dev`
4. Ou configure: `export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true`

### Passo 3: Instalar Super Jailbreak

1. Baixe a release mais recente
2. Extraia para `game/csgo/addons/counterstrikesharp/plugins/SuperJailbreak/`
3. Configure `config.json` com suas preferências
4. Reinicie o servidor

### Passo 4: Verificar Instalação

No console do servidor, digite:
```
meta list
```
Deve mostrar: `CounterStrikeSharp`

```
css_plugins list
```
Deve mostrar: `SuperJailbreak`

### Script de Instalação Automática (Linux)

```bash
# Baixar e executar o instalador
curl -sSL https://raw.githubusercontent.com/seu-repo/CS2-SuperJailbreak/main/scripts/install-server.sh | sudo bash
```

Ou manualmente:
```bash
cd CS2-SuperJailbreak/scripts
chmod +x install-server.sh
sudo ./install-server.sh
```

### Configuracao do Banco de Dados (Opcional)
```sql
CREATE DATABASE superjailbreak;
CREATE USER 'jailbreak'@'localhost' IDENTIFIED BY 'senha_segura';
GRANT ALL PRIVILEGES ON superjailbreak.* TO 'jailbreak'@'localhost';
FLUSH PRIVILEGES;
```

## Configuracao

O arquivo de configuracao e gerado automaticamente em:
`configs/plugins/CS2-SuperJailbreak/CS2-SuperJailbreak.json`

### Principais Opcoes
```json
{
  "DatabaseEnabled": true,
  "WardenEnabled": true,
  "WardenLaserEnabled": true,
  "LREnabled": true,
  "SpecialDaysEnabled": true,
  "EconomyEnabled": true,
  "GangsEnabled": true,
  "AchievementsEnabled": true,
  "Language": "pt-BR"
}
```

## Idiomas Suportados
- Portugues (pt-BR)
- English (en)

## Desenvolvimento

### Compilar
```bash
dotnet build
```

### Publicar
```bash
dotnet publish -c Release
```

## Creditos

Este plugin foi inspirado e baseado nos melhores plugins de Jailbreak existentes:
- [Cs2Jailbreak by destoer](https://github.com/destoer/Cs2Jailbreak)
- [cs2-jailbreak by edgegamers](https://github.com/edgegamers/cs2-jailbreak)
- [SM_Hosties](https://github.com/dataviruset/sm-hosties)
- [MyJailbreak](https://github.com/shanapu/MyJailbreak)
- [Better-Warden](https://github.com/jonteohr/Better-Warden)

## Licenca

GPL-3.0 License

## Suporte

Para bugs e sugestoes, abra uma issue no repositorio.

---

**Super Jailbreak** - O plugin de Jailbreak mais completo para CS2!
