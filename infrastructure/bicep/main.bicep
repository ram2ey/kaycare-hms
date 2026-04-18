// ============================================================
//  KayCare HMS — Azure Infrastructure
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

@description('SQL Server administrator password — supplied at deploy time, stored in Key Vault')
@secure()
param sqlAdminPassword string

@description('JWT signing key — supplied at deploy time, stored in Key Vault')
@secure()
param jwtKey string

// ── Derived names ────────────────────────────────────────────
var prefix         = '${appName}-${environment}'
var kvName         = '${prefix}-kv'          // max 24 chars
var sqlServerName  = '${prefix}-sql'
var sqlDbName      = 'KayCareDb'
var storageName    = replace('${appName}${environment}stor', '-', '')  // no hyphens allowed
var appPlanName    = '${prefix}-plan'
var apiAppName     = '${prefix}-api'
var staticWebName  = '${prefix}-web'

// ── Modules ──────────────────────────────────────────────────

module kv 'modules/keyVault.bicep' = {
  name: 'keyVault'
  params: {
    kvName:             kvName
    location:           location
    sqlAdminPassword:   sqlAdminPassword
    jwtKey:             jwtKey
    sqlServerName:      sqlServerName
    sqlDbName:          sqlDbName
    sqlAdminLogin:      sqlAdminLogin
    storageAccountName: storageName
    apiAppName:         apiAppName
  }
}

module sql 'modules/sqlServer.bicep' = {
  name: 'sqlServer'
  params: {
    sqlServerName:  sqlServerName
    sqlDbName:      sqlDbName
    location:       location
    adminLogin:     sqlAdminLogin
    adminPassword:  sqlAdminPassword
    apiAppName:     apiAppName
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    storageAccountName: storageName
    location:           location
  }
}

module appService 'modules/appService.bicep' = {
  name: 'appService'
  params: {
    appPlanName:        appPlanName
    apiAppName:         apiAppName
    location:           location
    kvName:             kvName
    sqlServerName:      sqlServerName
    sqlDbName:          sqlDbName
    storageAccountName: storageName
    environment:        environment
  }
  dependsOn: [kv, sql, storage]
}

module staticWeb 'modules/staticWebApp.bicep' = {
  name: 'staticWebApp'
  params: {
    staticWebName: staticWebName
    location:      'eastus2'   // Static Web Apps have limited region support; eastus2 is fine globally
  }
}

// ── Outputs (shown after deployment) ─────────────────────────
output apiUrl        string = 'https://${apiAppName}.azurewebsites.net'
output staticWebUrl  string = 'https://${staticWebName}.azurestaticapps.net'
output keyVaultName  string = kvName
output sqlServerFqdn string = sql.outputs.sqlServerFqdn
