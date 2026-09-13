import { type } from "@arkenv/nuxt"
import { string } from "arktype/internal/keywords/string.ts"

export const Env = type({
  // API_BASE_URL: type.string().default("http://localhost:3001"),
  API_BASE_URL: string.url.
})