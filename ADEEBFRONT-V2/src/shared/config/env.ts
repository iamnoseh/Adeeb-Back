import { z } from 'zod'

const envSchema = z.object({
  VITE_API_BASE_URL: z.string().optional(),
})

const env = envSchema.parse(import.meta.env)

export const appConfig = {
  apiBaseUrl: env.VITE_API_BASE_URL?.replace(/\/$/, '') ?? '',
}
