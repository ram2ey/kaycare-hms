// ── Azure Static Web Apps (Free tier) ─────────────────────────
// Hosts the React + TypeScript frontend
// Free tier: 100GB bandwidth/month, custom domains, SSL — $0/month

param staticWebName string
param location      string

resource staticWeb 'Microsoft.Web/staticSites@2023-01-01' = {
  name:     staticWebName
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Disabled'
    allowConfigFileUpdates:   true
    buildProperties: {
      appLocation:        'frontend'      // React app root
      outputLocation:     'dist'          // Vite build output
      appBuildCommand:    'npm run build'
    }
  }
}

output staticWebUrl         string = 'https://${staticWeb.properties.defaultHostname}'
output staticWebDeployToken string = staticWeb.listSecrets().properties.apiKey
