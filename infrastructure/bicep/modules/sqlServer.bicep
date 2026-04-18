// ── Azure SQL Server + Database (Basic tier ~$5/month) ────────

param sqlServerName string
param sqlDbName     string
param location      string
param adminLogin    string
param apiAppName    string

@secure()
param adminPassword string

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name:     sqlServerName
  location: location
  properties: {
    administratorLogin:         adminLogin
    administratorLoginPassword: adminPassword
    minimalTlsVersion:          '1.2'
    publicNetworkAccess:        'Enabled'   // needed until private endpoints are set up
  }

  // Allow Azure services (App Service) to connect
  resource azureFirewallRule 'firewallRules' = {
    name: 'AllowAzureServices'
    properties: {
      startIpAddress: '0.0.0.0'
      endIpAddress:   '0.0.0.0'
    }
  }
}

// Database — Basic tier (5 DTUs, 2GB max, ~$5/month)
resource sqlDb 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name:   sqlDbName
  location: location
  sku: {
    name:     'Basic'
    tier:     'Basic'
    capacity: 5
  }
  properties: {
    collation:   'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648   // 2 GB
  }
}

output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
