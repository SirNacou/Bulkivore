import arkenv from "@arkenv/nuxt"

/**
 * Environment variable schema.
 * In Nuxt, use `@arkenv/nuxt` to validate variables at build-time and runtime.
 * Enforces client/server separation and prevents secret leaks.
 */
export const env = arkenv({
	DATABASE_URL: "string = 'postgres://localhost:5432/mydb'",
	NUXT_PUBLIC_API_URL: "string.url = 'https://api.example.com'",
	NODE_ENV: "'development' | 'production' | 'test' = 'development'",
},
	{
		exposeToClient: ['NUXT_PUBLIC_API_URL', 'NODE_ENV']
	})