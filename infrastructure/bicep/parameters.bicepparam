// ── KayCare HMS Deployment Parameters ─────────────────────────
// Usage:
//   az deployment group create \
//     --resource-group kaycare-rg \
//     --template-file infrastructure/bicep/main.bicep \
//     --parameters infrastructure/bicep/parameters.bicepparam \
//                  sqlAdminPassword="<YourStrongPassword>" \
//                  jwtKey="<YourJwtSigningKey>"
//
// NEVER commit real passwords here. Pass them at deploy time via CLI.

using './main.bicep'

param environment      = 'prod'
param appName          = 'kaycare'
param location         = 'southafricanorth'
param sqlAdminLogin    = 'kaycare_admin'

// Secrets — passed via CLI flags, NOT stored in this file
param sqlAdminPassword = ''   // override: --parameters sqlAdminPassword="..."
param jwtKey           = ''   // override: --parameters jwtKey="..."
