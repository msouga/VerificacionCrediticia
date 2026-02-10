@description('Ubicacion de los recursos')
param location string = resourceGroup().location

@description('Ubicacion del Static Web App (no disponible en todas las regiones)')
param swaLocation string = 'westus2'

@description('Sufijo de ambiente')
param envSuffix string = 'dev'

@description('URL del Static Web App para CORS')
param swaUrl string = ''

@description('Connection string de SQL Server')
@secure()
param sqlConnectionString string

@description('Connection string de Storage Account')
@secure()
param storageConnectionString string

@description('API Key de Content Understanding')
@secure()
param contentUnderstandingApiKey string

@description('Equifax Client ID')
@secure()
param equifaxClientId string

@description('Equifax Client Secret')
@secure()
param equifaxClientSecret string

@description('Equifax BillTo')
param equifaxBillTo string = '011549B001'

@description('Equifax ShipTo')
param equifaxShipTo string = '011549B001S0001'

// Nombres de recursos
var appServicePlanName = 'plan-vercred-${envSuffix}'
var appServiceName = 'app-vercred-${envSuffix}'
var keyVaultName = 'kv-vercred-${envSuffix}'
var swaName = 'swa-vercred-${envSuffix}'
var logAnalyticsName = 'log-vercred-${envSuffix}'
var appInsightsName = 'ai-vercred-${envSuffix}'

// Log Analytics Workspace
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// App Service Plan (Linux B1)
resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  properties: {
    reserved: true
  }
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
}

// App Service (.NET 9)
resource appService 'Microsoft.Web/sites@2024-04-01' = {
  name: appServiceName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: true
      minTlsVersion: '1.2'
      // CORS manejado por middleware .NET (no por Azure App Service)
      cors: {
        allowedOrigins: [ '*' ]
      }
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'KeyVaultName'
          value: keyVaultName
        }
        {
          name: 'CorsOrigins__0'
          value: empty(swaUrl) ? 'http://localhost:4200' : swaUrl
        }
        {
          name: 'CorsOrigins__1'
          value: 'http://localhost:4200'
        }
      ]
    }
  }
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: appService.identity.principalId
        permissions: {
          secrets: [ 'get', 'list' ]
        }
      }
    ]
  }
}

// Secretos en Key Vault
// Los nombres usan '--' como separador; el Key Vault config provider de .NET lo convierte a ':'
resource secretSql 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ConnectionStrings--DefaultConnection'
  properties: {
    value: sqlConnectionString
  }
}

resource secretStorage 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'AzureStorage--ConnectionString'
  properties: {
    value: storageConnectionString
  }
}

resource secretCuApiKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ContentUnderstanding--ApiKey'
  properties: {
    value: contentUnderstandingApiKey
  }
}

resource secretEquifaxClientId 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Equifax--ClientId'
  properties: {
    value: equifaxClientId
  }
}

resource secretEquifaxClientSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Equifax--ClientSecret'
  properties: {
    value: equifaxClientSecret
  }
}

resource secretEquifaxBillTo 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Equifax--BillTo'
  properties: {
    value: equifaxBillTo
  }
}

resource secretEquifaxShipTo 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Equifax--ShipTo'
  properties: {
    value: equifaxShipTo
  }
}

// Static Web App (Free) - ubicacion separada porque no todas las regiones lo soportan
resource staticWebApp 'Microsoft.Web/staticSites@2024-04-01' = {
  name: swaName
  location: swaLocation
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {}
}

// Outputs
output appServiceName string = appService.name
output appServiceDefaultHostname string = appService.properties.defaultHostName
output appServicePrincipalId string = appService.identity.principalId
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output staticWebAppName string = staticWebApp.name
output staticWebAppDefaultHostname string = staticWebApp.properties.defaultHostname
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
