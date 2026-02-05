# Convenciones de Nombres en Azure

Proyecto: Verificacion Crediticia - Intcomex
Tenant: RSM Peru Innova S.A. (rsminnova.pe)
Suscripcion: MCPP Subscription - msouga (b667346d-f5e2-4c2c-9081-07db8612b591)

## Patron general

```
{abreviatura}-{proyecto}-{componente}-{ambiente}
```

Donde:
- **abreviatura**: Abreviatura oficial de Azure CAF (ver tabla abajo)
- **proyecto**: `vercred` (Verificacion Crediticia)
- **componente**: Opcional, solo cuando hay multiples instancias del mismo tipo (ej: `api`, `web`, `docs`)
- **ambiente**: `dev`, `qa`, `stg`, `prod`

## Ambientes

| Codigo | Descripcion | Uso |
|--------|-------------|-----|
| `dev` | Desarrollo | Trabajo diario, pruebas locales, mocks |
| `qa` | Calidad | Testing formal, pruebas de integracion |
| `stg` | Staging | Pre-produccion, pruebas con datos reales |
| `prod` | Produccion | Ambiente productivo |

## Abreviaturas oficiales de Azure CAF

Solo las relevantes para este proyecto:

| Recurso | Abreviatura | Scope |
|---------|-------------|-------|
| Resource Group | `rg` | Suscripcion |
| Document Intelligence | `di` | Resource Group |
| Azure OpenAI Service | `oai` | Resource Group |
| Storage Account | `st` | Global (sin guiones) |
| Key Vault | `kv` | Global |
| App Service Plan | `asp` | Resource Group |
| Web App (API) | `app` | Global |
| SQL Database Server | `sql` | Global |
| SQL Database | `sqldb` | Server |
| Application Insights | `appi` | Resource Group |
| Log Analytics Workspace | `log` | Resource Group |
| Managed Identity | `id` | Resource Group |
| Static Web App | `stapp` | Resource Group |
| AI Search | `srch` | Global |
| Azure Cosmos DB | `cosmos` | Global |

## Nombres concretos por ambiente

### Desarrollo (dev)

| Recurso | Nombre |
|---------|--------|
| Resource Group | `rg-vercred-dev` |
| Document Intelligence | `di-vercred-dev` |
| Azure OpenAI | `oai-vercred-dev` |
| Storage Account | `stvercreddev` |
| Key Vault | `kv-vercred-dev` |
| App Service Plan | `asp-vercred-dev` |
| Web App (API .NET) | `app-vercred-api-dev` |
| Web App (Angular) | `app-vercred-web-dev` |
| SQL Server | `sql-vercred-dev` |
| SQL Database | `sqldb-vercred-dev` |
| Application Insights | `appi-vercred-dev` |
| Log Analytics | `log-vercred-dev` |
| Managed Identity | `id-vercred-dev` |

### Produccion (prod)

| Recurso | Nombre |
|---------|--------|
| Resource Group | `rg-vercred-prod` |
| Document Intelligence | `di-vercred-prod` |
| Azure OpenAI | `oai-vercred-prod` |
| Storage Account | `stvercredprod` |
| Key Vault | `kv-vercred-prod` |
| App Service Plan | `asp-vercred-prod` |
| Web App (API .NET) | `app-vercred-api-prod` |
| Web App (Angular) | `app-vercred-web-prod` |
| SQL Server | `sql-vercred-prod` |
| SQL Database | `sqldb-vercred-prod` |
| Application Insights | `appi-vercred-prod` |
| Log Analytics | `log-vercred-prod` |
| Managed Identity | `id-vercred-prod` |

## Reglas importantes

1. **Todo en minusculas** excepto tags
2. **Separador**: guion `-` (excepto Storage Accounts que no los permiten)
3. **Storage Accounts**: Solo letras minusculas y numeros, sin guiones, max 24 caracteres
4. **Key Vault**: 3-24 caracteres, letras, numeros y guiones
5. **SQL Server**: 1-63 caracteres, minusculas, numeros y guiones, no empezar ni terminar con guion
6. **Web Apps**: Nombres globalmente unicos (se usan como subdominio de `.azurewebsites.net`)

## Tags obligatorios

Todos los recursos deben llevar estos tags:

| Tag | Valor | Ejemplo |
|-----|-------|---------|
| `proyecto` | Nombre del proyecto | `verificacion-crediticia` |
| `cliente` | Nombre del cliente | `intcomex` |
| `ambiente` | Ambiente | `dev` / `qa` / `stg` / `prod` |
| `responsable` | Email del responsable | `msouga@email.rsm.pe` |
| `centro-costo` | Centro de costo | (por definir) |

## Region

Region por defecto: **East US 2** (`eastus2`)

Razones:
- Disponibilidad de todos los servicios de AI (Document Intelligence, OpenAI)
- Menor latencia desde Latinoamerica comparado con West US
- Precios competitivos

## Referencia

- [Abreviaturas oficiales Azure CAF](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations)
- [Convenciones de nombres Azure CAF](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming)
- [Reglas y restricciones de nombres](https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules)
