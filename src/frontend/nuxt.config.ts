// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  ssr: false,
  spaLoadingTemplate: true,
  devtools: { enabled: true },
  devServer: {
    // Aspire injects PORT via WithHttpEndpoint(env: "PORT"). Bind to localhost
    // in that mode so the server's self-origin matches the browser origin
    // (http://localhost:<PORT>). Keep 0.0.0.0 for standalone `bun dev`
    // where LAN access may be needed.
    host: process.env.PORT ? 'localhost' : '0.0.0.0',
    port: process.env.PORT ? Number(process.env.PORT) : undefined,
  },
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
      // NOTE: do NOT pin `server.ws` host/port/clientPort here.
      // The Vite client builds its WebSocket URL as
      // `(ws.host || page.hostname):(clientPort || ws.port || page.port)`,
      // and @nuxt/devtools >= 3.3.1 rejects RPC connections whose
      // `Origin` doesn't match the WS `Host` header (CVE-2026-71319 fix).
      // Pinning the ports to the Aspire-injected internal PORT makes the
      // browser send `Origin: http://localhost:3000` while the WS upgrade
      // carries `Host: localhost:<internal-port>` -> "Ignored a cross-origin
      // RPC connection" + "Can't receive token". With the defaults, the WS
      // client connects back to the page's own origin, so the check passes
      // in every environment (direct, Aspire proxy, dynamic ports).
    }
  },
  icon: {
    collections: ["lucide"]
  },
  nuxtQuery: {
    autoImports: true,
    devtools: true,
    queryClientOptions: {
      defaultOptions: {
      }
    }
  },
  modules: ['@nuxt/eslint', '@arkenv/nuxt', '@nuxt/ui', '@arkenv/nuxt/module', '@peterbud/nuxt-query'],
})