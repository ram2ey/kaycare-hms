// ── Azure Blob Storage ─────────────────────────────────────────
// LRS (locally redundant) — cheapest, ~$0.02/GB/month
// Used for lab results, documents, prescriptions per-tenant containers

param storageAccountName string
param location           string

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name:     storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier:             'Hot'
    allowBlobPublicAccess:  false          // no public access — SAS tokens only
    minimumTlsVersion:      'TLS1_2'
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

// Blob service settings
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name:   'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days:    7              // soft-delete blobs for 7 days (accidental deletion protection)
    }
  }
}

output storageAccountName string = storageAccount.name
output storageAccountId   string = storageAccount.id

// Output the connection string for Key Vault
output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${az.environment().suffixes.storage}'
