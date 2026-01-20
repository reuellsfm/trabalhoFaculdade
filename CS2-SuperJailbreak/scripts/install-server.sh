#!/bin/bash

#===============================================================================
# SUPER JAILBREAK - Script de Instalação Completo para CS2
# Baseado na documentação oficial:
# - https://github.com/roflmuffin/CounterStrikeSharp/blob/main/INSTALL.md
# - https://wiki.alliedmods.net/Installing_metamod:source
#===============================================================================

set -e

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m' # No Color

# Configurações
CS2_DIR="/home/cs2server"
STEAM_USER="anonymous"
STEAMCMD_DIR="/home/steamcmd"

echo -e "${PURPLE}"
echo "==============================================================================="
echo "   SUPER JAILBREAK - Instalador Automatico para CS2"
echo "   Versao: 1.0.0"
echo "==============================================================================="
echo -e "${NC}"

#-------------------------------------------------------------------------------
# Função: Verificar se é root
#-------------------------------------------------------------------------------
check_root() {
    if [ "$EUID" -ne 0 ]; then
        echo -e "${RED}[ERRO] Execute como root: sudo bash install-server.sh${NC}"
        exit 1
    fi
}

#-------------------------------------------------------------------------------
# Função: Instalar dependências do sistema
#-------------------------------------------------------------------------------
install_dependencies() {
    echo -e "${BLUE}[1/8] Instalando dependencias do sistema...${NC}"

    apt-get update
    apt-get install -y \
        lib32gcc-s1 \
        lib32stdc++6 \
        libicu-dev \
        curl \
        wget \
        unzip \
        tar \
        screen \
        mysql-server \
        mysql-client \
        git \
        ca-certificates

    echo -e "${GREEN}[OK] Dependencias instaladas!${NC}"
}

#-------------------------------------------------------------------------------
# Função: Instalar SteamCMD
#-------------------------------------------------------------------------------
install_steamcmd() {
    echo -e "${BLUE}[2/8] Instalando SteamCMD...${NC}"

    mkdir -p $STEAMCMD_DIR
    cd $STEAMCMD_DIR

    if [ ! -f "steamcmd.sh" ]; then
        wget -q https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz
        tar -xzf steamcmd_linux.tar.gz
        rm steamcmd_linux.tar.gz
    fi

    echo -e "${GREEN}[OK] SteamCMD instalado!${NC}"
}

#-------------------------------------------------------------------------------
# Função: Instalar CS2 Dedicated Server
#-------------------------------------------------------------------------------
install_cs2() {
    echo -e "${BLUE}[3/8] Instalando CS2 Dedicated Server (pode demorar ~15-20 min)...${NC}"

    mkdir -p $CS2_DIR

    $STEAMCMD_DIR/steamcmd.sh \
        +force_install_dir $CS2_DIR \
        +login $STEAM_USER \
        +app_update 730 validate \
        +quit

    echo -e "${GREEN}[OK] CS2 Server instalado!${NC}"
}

#-------------------------------------------------------------------------------
# Função: Instalar Metamod:Source
# Documentação: https://wiki.alliedmods.net/Installing_metamod:source
#-------------------------------------------------------------------------------
install_metamod() {
    echo -e "${BLUE}[4/8] Instalando Metamod:Source...${NC}"

    # Baixar versão mais recente do Metamod para CS2
    METAMOD_URL="https://mms.alliedmods.net/mmsdrop/2.0/mmsource-2.0.0-git1313-linux.tar.gz"

    cd /tmp
    wget -q $METAMOD_URL -O metamod.tar.gz

    # Extrair para o diretório do CS2
    mkdir -p $CS2_DIR/game/csgo/addons
    tar -xzf metamod.tar.gz -C $CS2_DIR/game/csgo/
    rm metamod.tar.gz

    # IMPORTANTE: Editar gameinfo.gi para carregar Metamod
    # Baseado na documentação oficial
    GAMEINFO_FILE="$CS2_DIR/game/csgo/gameinfo.gi"

    if [ -f "$GAMEINFO_FILE" ]; then
        # Fazer backup
        cp $GAMEINFO_FILE ${GAMEINFO_FILE}.backup

        # Adicionar linha do Metamod após Game_LowViolence
        # A linha deve ser: Game csgo/addons/metamod
        if ! grep -q "csgo/addons/metamod" "$GAMEINFO_FILE"; then
            sed -i '/Game_LowViolence.*csgo_lv/a\            Game    csgo/addons/metamod' $GAMEINFO_FILE
            echo -e "${GREEN}[OK] gameinfo.gi configurado!${NC}"
        else
            echo -e "${YELLOW}[INFO] Metamod ja configurado no gameinfo.gi${NC}"
        fi
    else
        echo -e "${RED}[ERRO] gameinfo.gi nao encontrado! Instale o CS2 primeiro.${NC}"
        exit 1
    fi

    echo -e "${GREEN}[OK] Metamod:Source instalado!${NC}"
}

#-------------------------------------------------------------------------------
# Função: Instalar CounterStrikeSharp
# Documentação: https://github.com/roflmuffin/CounterStrikeSharp/blob/main/INSTALL.md
#-------------------------------------------------------------------------------
install_counterstrikesharp() {
    echo -e "${BLUE}[5/8] Instalando CounterStrikeSharp...${NC}"

    # Baixar versão com runtime (necessário na primeira instalação)
    CSS_VERSION="v300"
    CSS_URL="https://github.com/roflmuffin/CounterStrikeSharp/releases/download/${CSS_VERSION}/counterstrikesharp-with-runtime-build-${CSS_VERSION}-linux.zip"

    cd /tmp
    wget -q $CSS_URL -O counterstrikesharp.zip

    # Extrair para o diretório do CS2 (merge com addons existente)
    unzip -o counterstrikesharp.zip -d $CS2_DIR/game/csgo/
    rm counterstrikesharp.zip

    # Configurar variável de ambiente para .NET (caso necessário)
    echo 'export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false' >> /etc/environment

    echo -e "${GREEN}[OK] CounterStrikeSharp instalado!${NC}"
}

#-------------------------------------------------------------------------------
# Função: Compilar e instalar plugin SuperJailbreak
#-------------------------------------------------------------------------------
install_superjailbreak() {
    echo -e "${BLUE}[6/8] Instalando plugin SuperJailbreak...${NC}"

    PLUGIN_DIR="$CS2_DIR/game/csgo/addons/counterstrikesharp/plugins/SuperJailbreak"
    mkdir -p $PLUGIN_DIR

    # Copiar arquivos do plugin (assumindo que estão no diretório atual)
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    SOURCE_DIR="$(dirname "$SCRIPT_DIR")"

    if [ -d "$SOURCE_DIR/src" ]; then
        echo -e "${YELLOW}[INFO] Compilando plugin...${NC}"

        # Instalar .NET SDK se necessário
        if ! command -v dotnet &> /dev/null; then
            wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
            chmod +x dotnet-install.sh
            ./dotnet-install.sh --channel 8.0
            export PATH="$PATH:$HOME/.dotnet"
        fi

        cd $SOURCE_DIR
        dotnet restore
        dotnet publish -c Release -o $PLUGIN_DIR

        # Copiar configs e traduções
        cp -r configs/* $PLUGIN_DIR/ 2>/dev/null || true
        cp -r lang $PLUGIN_DIR/ 2>/dev/null || true
    else
        echo -e "${YELLOW}[INFO] Codigo fonte nao encontrado. Copie manualmente o plugin compilado.${NC}"
    fi

    echo -e "${GREEN}[OK] Plugin SuperJailbreak instalado!${NC}"
}

#-------------------------------------------------------------------------------
# Função: Configurar MySQL
#-------------------------------------------------------------------------------
setup_mysql() {
    echo -e "${BLUE}[7/8] Configurando MySQL...${NC}"

    # Iniciar MySQL
    systemctl start mysql
    systemctl enable mysql

    # Criar banco de dados e usuário
    DB_NAME="superjailbreak"
    DB_USER="jailbreak"
    DB_PASS="$(openssl rand -base64 12)"

    mysql -e "CREATE DATABASE IF NOT EXISTS $DB_NAME;"
    mysql -e "CREATE USER IF NOT EXISTS '$DB_USER'@'localhost' IDENTIFIED BY '$DB_PASS';"
    mysql -e "GRANT ALL PRIVILEGES ON $DB_NAME.* TO '$DB_USER'@'localhost';"
    mysql -e "FLUSH PRIVILEGES;"

    # Salvar credenciais
    echo -e "${YELLOW}[INFO] Credenciais do banco de dados:${NC}"
    echo "  Host: localhost"
    echo "  Database: $DB_NAME"
    echo "  User: $DB_USER"
    echo "  Password: $DB_PASS"

    # Criar arquivo de configuração
    CONFIG_FILE="$CS2_DIR/game/csgo/addons/counterstrikesharp/plugins/SuperJailbreak/config.json"
    if [ -f "${CONFIG_FILE}.example" ]; then
        cp ${CONFIG_FILE}.example $CONFIG_FILE
        sed -i "s/\"DatabaseName\": \".*\"/\"DatabaseName\": \"$DB_NAME\"/" $CONFIG_FILE
        sed -i "s/\"DatabaseUser\": \".*\"/\"DatabaseUser\": \"$DB_USER\"/" $CONFIG_FILE
        sed -i "s/\"DatabasePassword\": \".*\"/\"DatabasePassword\": \"$DB_PASS\"/" $CONFIG_FILE
    fi

    echo -e "${GREEN}[OK] MySQL configurado!${NC}"
}

#-------------------------------------------------------------------------------
# Função: Criar serviço systemd
#-------------------------------------------------------------------------------
create_service() {
    echo -e "${BLUE}[8/8] Criando servico systemd...${NC}"

    cat > /etc/systemd/system/cs2-jailbreak.service << EOF
[Unit]
Description=CS2 Jailbreak Server
After=network.target mysql.service

[Service]
Type=simple
User=root
WorkingDirectory=$CS2_DIR
ExecStart=$CS2_DIR/game/bin/linuxsteamrt64/cs2 -dedicated +game_type 1 +game_mode 0 +map de_prison +maxplayers 24 +sv_setsteamaccount YOUR_GSLT_TOKEN
Restart=on-failure
RestartSec=10

[Install]
WantedBy=multi-user.target
EOF

    systemctl daemon-reload

    echo -e "${GREEN}[OK] Servico criado!${NC}"
    echo -e "${YELLOW}[INFO] Edite /etc/systemd/system/cs2-jailbreak.service e adicione seu GSLT token${NC}"
    echo -e "${YELLOW}[INFO] Obtenha em: https://steamcommunity.com/dev/managegameservers${NC}"
}

#-------------------------------------------------------------------------------
# Função: Configurar firewall
#-------------------------------------------------------------------------------
setup_firewall() {
    echo -e "${BLUE}[BONUS] Configurando firewall...${NC}"

    # UFW
    if command -v ufw &> /dev/null; then
        ufw allow 27015/tcp  # RCON
        ufw allow 27015/udp  # Game
        ufw allow 27020/udp  # SourceTV
        ufw allow 3306/tcp   # MySQL (apenas local)
        echo -e "${GREEN}[OK] Firewall configurado (UFW)${NC}"
    fi
}

#-------------------------------------------------------------------------------
# Função: Mostrar resumo
#-------------------------------------------------------------------------------
show_summary() {
    echo ""
    echo -e "${PURPLE}===============================================================================${NC}"
    echo -e "${GREEN}                    INSTALACAO CONCLUIDA COM SUCESSO!${NC}"
    echo -e "${PURPLE}===============================================================================${NC}"
    echo ""
    echo -e "${YELLOW}Diretórios importantes:${NC}"
    echo "  CS2 Server:    $CS2_DIR"
    echo "  Plugins:       $CS2_DIR/game/csgo/addons/counterstrikesharp/plugins/"
    echo "  Configs:       $CS2_DIR/game/csgo/cfg/"
    echo ""
    echo -e "${YELLOW}Comandos úteis:${NC}"
    echo "  Iniciar:       systemctl start cs2-jailbreak"
    echo "  Parar:         systemctl stop cs2-jailbreak"
    echo "  Status:        systemctl status cs2-jailbreak"
    echo "  Logs:          journalctl -u cs2-jailbreak -f"
    echo ""
    echo -e "${YELLOW}Próximos passos:${NC}"
    echo "  1. Edite o GSLT token em /etc/systemd/system/cs2-jailbreak.service"
    echo "  2. Configure o plugin em $CS2_DIR/game/csgo/addons/counterstrikesharp/plugins/SuperJailbreak/config.json"
    echo "  3. Inicie o servidor: systemctl start cs2-jailbreak"
    echo ""
    echo -e "${RED}IMPORTANTE: Após updates do CS2, edite novamente o gameinfo.gi!${NC}"
    echo ""
}

#-------------------------------------------------------------------------------
# MAIN
#-------------------------------------------------------------------------------
main() {
    check_root
    install_dependencies
    install_steamcmd
    install_cs2
    install_metamod
    install_counterstrikesharp
    install_superjailbreak
    setup_mysql
    create_service
    setup_firewall
    show_summary
}

# Executar
main "$@"
