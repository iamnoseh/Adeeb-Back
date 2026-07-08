import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { appConfig } from '@/shared/config/env'
import { refreshOnce } from '@/shared/auth/refresh-manager'
import { tokenStore } from '@/shared/auth/token-store'
import { ApiError, isProblemDetails } from '@/shared/api/problem-details'
import { getStoredUiLanguage } from '@/shared/i18n/language'

type RetryableRequestConfig = InternalAxiosRequestConfig & {
  _retry?: boolean
}

export const httpClient = axios.create({
  baseURL: appConfig.apiBaseUrl,
  headers: {
    Accept: 'application/json',
    'X-Adeeb-Language': 'tg-TJ',
  },
})

httpClient.interceptors.request.use((config) => {
  const token = tokenStore.getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  config.headers['X-Adeeb-Language'] = getStoredUiLanguage()

  return config
})

httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetryableRequestConfig | undefined
    const isRefreshCall = originalRequest?.url?.includes('/api/v2/auth/refresh') ?? false

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry && !isRefreshCall) {
      originalRequest._retry = true
      await refreshOnce()
      return httpClient(originalRequest)
    }

    const payload = error.response?.data
    if (isProblemDetails(payload)) {
      throw new ApiError(payload.title, payload, payload.status)
    }

    throw new ApiError(error.message, undefined, error.response?.status)
  },
)
