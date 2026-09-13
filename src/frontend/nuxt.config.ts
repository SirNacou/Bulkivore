// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  ssr: false,
  spaLoadingTemplate: true,
  devtools: { enabled: true },
  css: ['~/assets/css/main.css'],
  typescript: {
    typeCheck: true
  },

  vite: {
    server: {
      watch: {
        // Enforces polling to catch file updates on Windows/WSL/network mounts
        usePolling: true,
        interval: 100,
      },
      hmr: {
        protocol: 'ws',
        // If Aspire assigns a dynamic port via the PORT env var, force Vite's HMR client to match
        clientPort: process.env.PORT ? Number(process.env.PORT) : undefined,
      }
    }
  },
  icon: {
    collections: ["lucide"]
  },
  modules: [
    '@nuxt/eslint',
    '@arkenv/nuxt',
    '@nuxt/ui',
    '@arkenv/nuxt/module'
  ],
})
