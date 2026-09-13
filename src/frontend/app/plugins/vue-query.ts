import { QueryClient, VueQueryPlugin, type VueQueryPluginOptions } from "@tanstack/vue-query"

export default defineNuxtPlugin((nuxtApp) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 1000 * 60 * 5,
        gcTime: 1000 * 60 * 30,
        retry: 1,
        refetchOnWindowFocus: false
      }
    }
  })

  const options: VueQueryPluginOptions = { queryClient, enableDevtoolsV6Plugin: true }
  nuxtApp.vueApp.use(VueQueryPlugin, options)
})