// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },
  css: ['~/assets/css/main.css'],
  modules: [
    '@peterbud/nuxt-query',
    '@nuxt/eslint',
    '@arkenv/nuxt',
    '@nuxt/ui'
  ]
})