#!/bin/bash

#===============================================================================
# Script para corrigir gameinfo.gi após updates do CS2
# IMPORTANTE: O CS2 sobrescreve este arquivo em cada update!
#
# Documentação oficial:
# https://wiki.alliedmods.net/Installing_metamod:source
#===============================================================================

CS2_DIR="${1:-/home/cs2server}"
GAMEINFO_FILE="$CS2_DIR/game/csgo/gameinfo.gi"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo "========================================"
echo " Corretor de gameinfo.gi para Metamod"
echo "========================================"

# Verificar se arquivo existe
if [ ! -f "$GAMEINFO_FILE" ]; then
    echo -e "${RED}[ERRO] Arquivo nao encontrado: $GAMEINFO_FILE${NC}"
    echo "Use: ./fix-gameinfo.sh /caminho/para/cs2server"
    exit 1
fi

# Fazer backup
cp "$GAMEINFO_FILE" "${GAMEINFO_FILE}.backup.$(date +%Y%m%d_%H%M%S)"
echo -e "${GREEN}[OK] Backup criado${NC}"

# Verificar se já tem Metamod configurado
if grep -q "csgo/addons/metamod" "$GAMEINFO_FILE"; then
    echo -e "${YELLOW}[INFO] Metamod ja esta configurado no gameinfo.gi${NC}"
    exit 0
fi

# Adicionar linha do Metamod
# Deve ser adicionado LOGO APÓS "Game_LowViolence csgo_lv" na seção SearchPaths
# E deve ser o PRIMEIRO na lista de SearchPaths

# Método: Adicionar após a linha Game_LowViolence
sed -i '/Game_LowViolence.*csgo_lv/a\			Game	csgo/addons/metamod' "$GAMEINFO_FILE"

# Verificar se foi adicionado
if grep -q "csgo/addons/metamod" "$GAMEINFO_FILE"; then
    echo -e "${GREEN}[OK] Metamod adicionado ao gameinfo.gi${NC}"
    echo ""
    echo "Linha adicionada:"
    grep -n "metamod" "$GAMEINFO_FILE"
else
    echo -e "${RED}[ERRO] Falha ao adicionar Metamod${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}Pronto! Reinicie o servidor CS2.${NC}"
