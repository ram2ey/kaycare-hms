// ── Key Vault ─────────────────────────────────────────────────
// Stores all production secrets. App Service reads them via
// Key Vault references in app settings (no secrets in config files).

param kvName             string
param location           string
param sqlAdminLogin      string
param sqlAdminPassword   string
param jwtKey             string
param sqlServerName      string
param sqlDbName          string
param storageAccountName string
param apiAppName         string

// Key Vault
resource kv 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name:     kvName
  location: location
  properties: {
    sku: {
      family: 'A'
      name:   'standard'
    }
    tenantId:                    tenant().tenantId
    enableRbacAuthorization:     true      // use RBAC, not access policies
    enableSoftDelete:            true
    softDeleteRetentionInDays:   7
    enablePurgeProtection:       false     // allow purge in dev; set true in prod for compliance
  }
}

// Store secrets
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

// Build and store the full SQL connection string as a secret
var connString = 'Server=tcp:${sqlServerName}.database.windows.net,1433;Initial Catalog=${sqlDbName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource secretConnString 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: kv
  name:   'DefaultConnection'
  properties: { value: connString }
}

// Storage connection string placeholder — updated after storage account is created
// (storage account key is retrieved via listKeys and stored here)
resource secretBlobConn 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: kv
  name:   'BlobStorageConnection'
  properties: {
    // Will be set by the deploy script after storage account is provisioned
    value: 'PLACEHOLDER-run-deploy-script-to-set'
  }
}

// Grant the App Service managed identity access to read secrets
// (role assignment done in appService.bicep after identity is known)

output kvUri string = kv.properties.vaultUri
output kvId  string = kv.id
