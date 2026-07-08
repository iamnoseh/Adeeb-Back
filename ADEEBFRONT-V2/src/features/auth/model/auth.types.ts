export type DeviceRequest = {
  deviceId: string
  deviceName: string
  platform: string
  appVersion?: string | null
}

export type LoginRequest = {
  identifier?: string | null
  email?: string | null
  password: string
  device?: DeviceRequest | null
}

export type RefreshTokenRequest = {
  refreshToken: string
}

export type UserResponse = {
  id: string
  email: string
  phoneNumber?: string | null
  firstName: string
  lastName: string
  preferredLanguage: string
  role: 'User' | 'SuperAdmin' | 'Admin' | string
}

export type TokenResponse = {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAtUtc: string
  accessTokenExpiresAtDushanbe: string
}

export type SessionResponse = {
  id: string
  deviceName: string
}

export type AuthResponse = {
  user: UserResponse
  tokens: TokenResponse
  session: SessionResponse
}
