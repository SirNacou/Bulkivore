import { defineConfig } from 'kubb/config'
import { pluginTs } from '@kubb/plugin-ts'
import { pluginFetch } from '@kubb/plugin-fetch'
import { pluginVueQuery } from '@kubb/plugin-vue-query'
import { pluginZod } from '@kubb/plugin-zod'
import { pluginRedoc } from '@kubb/plugin-redoc'

export default defineConfig({
  input: 'http://localhost:3001/openapi/v1.yaml',
  output: {
    path: './app/gen',
    clean: true,
  },
  plugins: [
    pluginTs(),
    pluginFetch(),
    pluginVueQuery(),
    pluginZod(),
    pluginRedoc(),
  ],
})
