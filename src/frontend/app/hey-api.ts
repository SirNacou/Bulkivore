import type { CreateClientConfig } from './client/client.gen'

export const createClientConfig: CreateClientConfig = (config) => ({
  ...config,
  baseURL: 'http://localhost:3001',
})