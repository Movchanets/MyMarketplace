import axiosClient from './axiosClient'
import type { User } from '../store/authStore'

export interface CheckEmailResponse {
  exists: boolean
}

export interface LoginRequest {
  email: string
  password: string
  turnstileToken?: string
}

export interface RegisterRequest {
  email: string
  name: string
  surname: string
  password: string
  confirmPassword: string
  turnstileToken?: string
}

export interface AuthResponse {
  token: string
  user: User
}

export interface TokenResponse {
  accessToken: string
  refreshToken?: string
}

export interface RequestPasswordResetRequest {
  email: string
  turnstileToken?: string
}

export interface ResetPasswordRequest {
  email: string
  token: string
  newPassword: string
}

export interface GoogleLoginRequest {
  idToken: string
  turnstileToken?: string
}

export const authApi = {
  // Перевірка чи існує email в базі
  checkEmail: async (email: string, turnstileToken?: string): Promise<CheckEmailResponse> => {
    const qs = new URLSearchParams({ email })
    if (turnstileToken) qs.set('turnstileToken', turnstileToken)
    const response = await axiosClient.get<CheckEmailResponse>(`/auth/check-email?${qs.toString()}`)
    return response.data
  },

  // Логін
  login: async (data: LoginRequest): Promise<TokenResponse> => {
  const response = await axiosClient.post<TokenResponse>('/auth/login', data)

    // Store tokens if backend returned them (keep backward-compatible keys)
   
    console.log('Login response:', response.data); // Додано для налагодження
  
    const tokens : TokenResponse = response.data ;
 console.log('Login response tokens:', tokens); // Додано для налагодження
    const access = tokens.accessToken || ""
    const refresh = tokens.refreshToken || ""

    if (access) {
      localStorage.setItem('accessToken', access);
    }
    if (refresh) {
      localStorage.setItem('refreshToken', refresh);
    }

    return { accessToken: access, refreshToken: refresh }
  },

  // Реєстрація
  register: async (data: RegisterRequest): Promise<TokenResponse> => {
    console.log('Register data:', data); // Додано для налагодження
  const response = await axiosClient.post<TokenResponse>('/auth/register', data)
    console.log('Register response:', response.data); // Додано для налагодження
    const tokens : TokenResponse = response.data ;
       const access = tokens.accessToken || ""
    const refresh = tokens.refreshToken || ""

    if (access) {
      localStorage.setItem('accessToken', access);
    }
    if (refresh) {
      localStorage.setItem('refreshToken', refresh);
    }

    return { accessToken: access, refreshToken: refresh }
  },

  // Ініціація відновлення паролю
  requestPasswordReset: async (data: RequestPasswordResetRequest): Promise<{ message: string }> => {
    const response = await axiosClient.post('/auth/forgot-password', data)
    return response.data
  },

  // Завершення відновлення паролю
  resetPassword: async (data: ResetPasswordRequest): Promise<{ message: string }> => {
    const response = await axiosClient.post('/auth/reset-password', data)
    return response.data
  },

  // Google OAuth login
  googleLogin: async (data: GoogleLoginRequest): Promise<TokenResponse> => {
    const response = await axiosClient.post<TokenResponse>('/auth/google-login', data)
    const tokens: TokenResponse = response.data
    const access = tokens.accessToken || ''
    const refresh = tokens.refreshToken || ''

    if (access) {
      localStorage.setItem('accessToken', access)
    }
    if (refresh) {
      localStorage.setItem('refreshToken', refresh)
    }

    return { accessToken: access, refreshToken: refresh }
  },

  // Refresh access/refresh tokens using stored tokens
  refreshTokens: async (): Promise<TokenResponse> => {
    const access = localStorage.getItem('accessToken') || ''
    const refresh = localStorage.getItem('refreshToken') || ''
    const response = await axiosClient.post<TokenResponse>('/auth/refresh', { accessToken: access, refreshToken: refresh })
    const tokens: TokenResponse = response.data
    const accessNew = tokens.accessToken || ''
    const refreshNew = tokens.refreshToken || ''
    if (accessNew) localStorage.setItem('accessToken', accessNew)
    if (refreshNew) localStorage.setItem('refreshToken', refreshNew)
    return { accessToken: accessNew, refreshToken: refreshNew }
  },

  // Get all users (admin)
  // (user-management methods moved to userApi.ts)
}
