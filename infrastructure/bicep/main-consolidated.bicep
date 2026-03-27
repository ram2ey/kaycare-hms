// ============================================================
//  KayCare HMS — Azure Infrastructure (single-file, Portal-deployable)
//  Region: South Africa North
//  Target: B1 App Service + Basic SQL + Free Static Web App
// ============================================================

targetScope = 'resourceGroup'

@description('Short environment tag: dev | staging | prod')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'prod'

@description('Base name used for all resources (lowercase, no spaces)')
param appName string = 'kaycare'

@description('Azure region for all resources')
param location string = 'southafricanorth'

@description('SQL Server administrator login')
param sqlAdminLogin string = 'kaycare_admin'

@description('SQL Server administrator password — supplied at deploy time')
@secure()
param sqlAdminPassword string

@description('JWT signing key — supplied at deploy time')
@secure()
param jwtKey string

// ── Derived names ─────────────────────────────────────────────
var prefix        = '${appName}-${environment}'
var kvName        = '${prefix}-kv'
var sqlServerName = '${prefix}-sql'
var sqlDbName     = 'KayCareDb'
var storageName   = replace('${appName}${environment}stor', '-', '')
var appPlanName   = '${prefix}-plan'
var apiAppName    = '${prefix}-api'
var staticWebName = '${prefix}-web'

// ── Key Vault ─────────────────────────────────────────────────
resource kv 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name:     kvName
  location: location
  properties: {
    sku: {
      family: 'A'
      name:   'standard'
    }
    tenantId:                   tenant().tenantId
    enableRbacAuthorization:    true
    enableSoftDelete:           true
    softDeleteRetentionInDays:  7
  }
}

resource secretSqlPassword 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: kv
  name:   'SqlAdminPassword'
  properties: { value: sqlAdminPassword }
}

resource secretJwtKey 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: kv
  name:   'JwtKey'
  properties: { value: jwtKey }
}

var connString = 'Server=tcp:${sqlServerName}.database.windows.net,1433;Initial Catalog=${sqlDbName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource secretConnString 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: kv
  name:   'DefaultConnection'
  properties: { value: connString }
}

// BlobStorageConnection written directly from storage account key (no placeholder needed)
resource secretBlobConn 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: kv
  name:   'BlobStorageConnection'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${az.environment().suffixes.storage}'
  }
}

// ── SQL Server + Database (Basic, ~$5/month) ──────────────────
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name:     sqlServerName
  location: location
  properties: {
    administratorLogin:         sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion:          '1.2'
    publicNetworkAccess:        'Enabled'
  }

  resource azureFirewallRule 'firewallRules' = {
    name: 'AllowAzureServices'
    properties: {
      startIpAddress: '0.0.0.0'
      endIpAddress:   '0.0.0.0'
    }
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent:   sqlServer
  name:     sqlDbName
  location: location
  sku: {
    name:     'Basic'
    tier:     'Basic'
    capacity: 5
  }
  properties: {
    collation:    'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
  }
}

// ── Blob Storage (LRS, ~$1/month) ─────────────────────────────
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name:     storageName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier:               'Hot'
    allowBlobPublicAccess:    false
    minimumTlsVersion:        'TLS1_2'
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        blob: { enabled: true }
        file: { enabled: true }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name:   'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days:    7
    }
  }
}

// ── App Service Plan (B1, ~$13/month) + API Web App ──────────
var kvBaseUri           = 'https://${kvName}${az.environment().suffixes.keyvaultDns}/secrets'
var kvSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

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
    reserved: true
  }
}

resource apiApp 'Microsoft.Web/sites@2023-01-01' = {
  name:     apiAppName
  location: location
  kind:     'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appPlan.id
    httpsOnly:    true
    siteConfig: {
      linuxFxVersion:  'DOTNETCORE|8.0'
      alwaysOn:        true
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
  dependsOn: [kv, sqlServer, storageAccount]
}

// Grant App Service managed identity the "Key Vault Secrets User" role
resource kvRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name:  guid(kv.id, apiApp.id, kvSecretsUserRoleId)
  scope: kv
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId:      apiApp.identity.principalId
    principalType:    'ServicePrincipal'
  }
}

// ── Static Web App (Free tier) ────────────────────────────────
resource staticWeb 'Microsoft.Web/staticSites@2023-01-01' = {
  name:     staticWebName
  location: 'eastus2'   // Static Web Apps have limited region support
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Disabled'
    allowConfigFileUpdates:   true
    buildProperties: {
      appLocation:     'frontend'
      outputLocation:  'dist'
      appBuildCommand: 'npm run build'
    }
  }
}

// ── Outputs ───────────────────────────────────────────────────
output apiUrl             string = 'https://${apiApp.properties.defaultHostName}'
output staticWebUrl       string = 'https://${staticWeb.properties.defaultHostname}'
output keyVaultName       string = kvName
output sqlServerFqdn      string = sqlServer.properties.fullyQualifiedDomainName
output staticWebDeployToken string = staticWeb.listSecrets().properties.apiKey
