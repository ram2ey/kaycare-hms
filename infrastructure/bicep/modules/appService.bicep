// ── App Service Plan (B1) + API Web App ───────────────────────
// B1 Basic: 1 core, 1.75GB RAM, custom domains, SSL, always-on
// ~$13/month in South Africa North

param appPlanName        string
param apiAppName         string
param location           string
param kvName             string
param sqlServerName      string
param sqlDbName          string
param storageAccountName string
param environment        string

var kvBaseUri = 'https://${kvName}${az.environment().suffixes.keyvaultDns}/secrets'

// App Service Plan — B1 Basic
resource appPlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name:     appPlanName
  location: location
  sku: {
    name:     'B1'
    tier:     'Basic'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true    // required for Linux
  }
}

// API Web App
resource apiApp 'Microsoft.Web/sites@2023-01-01' = {
  name:     apiAppName
  location: location
  kind:     'app,linux'
  identity: {
    type: 'SystemAssigned'   // managed identity → reads Key Vault secrets
  }
  properties: {
    serverFarmId: appPlan.id
    httpsOnly:    true
    siteConfig: {
      linuxFxVersion:  'DOTNETCORE|8.0'
      alwaysOn:        true             // B1 supports always-on (no cold starts)
      ftpsState:       'Disabled'
      minTlsVersion:   '1.2'
      appSettings: [
        {
          name:  'ASPNETCORE_ENVIRONMENT'
          value: environment == 'prod' ? 'Production' : 'Development'
        }
        {
          name:  'Jwt__Key'
          value: '@Microsoft.KeyVault(SecretUri=${kvBaseUri}/JwtKey/)'
        }
        {
          name:  'Jwt__Issuer'
          value: 'KayCare'
        }
        {
          name:  'Jwt__Audience'
          value: 'KayCare'
        }
        {
          name:  'Jwt__ExpiryHours'
          value: '8'
        }
        {
          name:  'BlobStorage__ConnectionString'
          value: '@Microsoft.KeyVault(SecretUri=${kvBaseUri}/BlobStorageConnection/)'
        }
        {
          name:  'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
      connectionStrings: [
        {
          name:             'DefaultConnection'
          connectionString: '@Microsoft.KeyVault(SecretUri=${kvBaseUri}/DefaultConnection/)'
          type:             'SQLAzure'
        }
      ]
    }
  }
}

// Grant App Service managed identity the "Key Vault Secrets User" role
// so it can read secrets via Key Vault references
var kvSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource kv 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: kvName
}

resource kvRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name:  guid(kv.id, apiApp.id, kvSecretsUserRoleId)
  scope: kv
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId:      apiApp.identity.principalId
    principalType:    'ServicePrincipal'
  }
}

output apiAppUrl          string = 'https://${apiApp.properties.defaultHostName}'
output apiAppPrincipalId  string = apiApp.identity.principalId
