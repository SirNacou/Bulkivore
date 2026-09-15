import { defineConfig } from '@hey-api/openapi-ts'

export default defineConfig({
  input: {
    path: 'http://localhost:3001/openapi/v1.yaml',
    watch: true
  },
  output: {
    path: "app/client",
    postProcess: ["prettier"],
  },
  logs: {
    path: "./logs"
  },
  plugins: [
    { name: '@hey-api/typescript', enums: 'typescript' },
    { name: '@hey-api/sdk', validator: true, transformer: true },
    { name: '@hey-api/client-ky', runtimeConfigPath: './app/hey-api.ts' },
    { name: 'zod', dates: { offset: true }, types: { input: true, output: true } },
    { name: '@tanstack/vue-query' }
  ]
})