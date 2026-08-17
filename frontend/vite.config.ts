import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// Keep in sync with the `headers:` block for the kaycare-hms-frontend service in render.yaml —
// that's what actually applies once deployed (Render's static-site headers, not anything Vite
// serves). This copy exists so `npm run preview` (serves the real production build, unlike `npm
// run dev`'s HMR-instrumented one) can be used to verify the policy locally against a browser
// before every real deploy. style-src needs 'unsafe-inline' for the small number of dynamic
// style={{}} JSX props in this app (charts/dashboards); everything else is same-origin only
// except img-src/connect-src, which allow any HTTPS origin since document/logo/PACS images and
// the API call target are signed URLs / a configurable origin not known at build time.
const CONTENT_SECURITY_POLICY = [
  "default-src 'self'",
  "script-src 'self'",
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data: https:",
  "font-src 'self' data:",
  "connect-src 'self' https:",
  "object-src 'none'",
  "base-uri 'self'",
  "form-action 'self'",
  "frame-ancestors 'none'",
].join('; ')

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api': 'http://localhost:5012',
    },
  },
  preview: {
    headers: {
      'Content-Security-Policy': CONTENT_SECURITY_POLICY,
    },
  },
})

