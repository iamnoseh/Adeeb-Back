import { appConfig } from '@/shared/config/env'

export function toAssetUrl(url?: string | null) {
  if (!url) return null
  if (/^https?:\/\//i.test(url) || url.startsWith('blob:') || url.startsWith('data:')) return url
  return `${appConfig.apiBaseUrl}${url.startsWith('/') ? url : `/${url}`}`
}
