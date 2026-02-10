#!/bin/bash
# Poblar Key Vault con secretos para VerificacionCrediticia
# Uso: ./seed-keyvault.sh
#
# Los nombres usan '--' como separador; el Key Vault config provider de .NET lo convierte a ':'
# Requiere: az login previo.
# NUNCA commitear este archivo con secretos reales.

set -euo pipefail

VAULT_NAME="kv-vercred-dev"

echo "Poblando Key Vault: $VAULT_NAME"

# SQL Connection String -> ConnectionStrings:DefaultConnection
read -r -s -p "ConnectionStrings--DefaultConnection (SQL): " SQL_CONN && echo
az keyvault secret set --vault-name "$VAULT_NAME" --name "ConnectionStrings--DefaultConnection" --value "$SQL_CONN" --output none

# Storage Connection String -> AzureStorage:ConnectionString
read -r -s -p "AzureStorage--ConnectionString (Storage): " STORAGE_CONN && echo
az keyvault secret set --vault-name "$VAULT_NAME" --name "AzureStorage--ConnectionString" --value "$STORAGE_CONN" --output none

# Content Understanding API Key -> ContentUnderstanding:ApiKey
read -r -s -p "ContentUnderstanding--ApiKey: " CU_KEY && echo
az keyvault secret set --vault-name "$VAULT_NAME" --name "ContentUnderstanding--ApiKey" --value "$CU_KEY" --output none

# Equifax Client ID -> Equifax:ClientId
read -r -s -p "Equifax--ClientId: " EQ_ID && echo
az keyvault secret set --vault-name "$VAULT_NAME" --name "Equifax--ClientId" --value "$EQ_ID" --output none

# Equifax Client Secret -> Equifax:ClientSecret
read -r -s -p "Equifax--ClientSecret: " EQ_SECRET && echo
az keyvault secret set --vault-name "$VAULT_NAME" --name "Equifax--ClientSecret" --value "$EQ_SECRET" --output none

# Equifax BillTo -> Equifax:BillTo
read -r -p "Equifax--BillTo [011549B001]: " EQ_BILL
EQ_BILL="${EQ_BILL:-011549B001}"
az keyvault secret set --vault-name "$VAULT_NAME" --name "Equifax--BillTo" --value "$EQ_BILL" --output none

# Equifax ShipTo -> Equifax:ShipTo
read -r -p "Equifax--ShipTo [011549B001S0001]: " EQ_SHIP
EQ_SHIP="${EQ_SHIP:-011549B001S0001}"
az keyvault secret set --vault-name "$VAULT_NAME" --name "Equifax--ShipTo" --value "$EQ_SHIP" --output none

echo ""
echo "Key Vault poblado exitosamente."
echo "Verificar con: az keyvault secret list --vault-name $VAULT_NAME --output table"
