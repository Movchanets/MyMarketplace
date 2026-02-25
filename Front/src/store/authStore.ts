import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { parseJwt, toArray } from '../utils/jwt'
import { queryClient } from '../queryClient'

export interface User {
  id: string
  email: string
  name: string
  firstName?: string
  lastName?: string
  avatarUrl?: string
  roles?: string[]
  permissions?: string[]
}

interface AuthState {
  user: User | null
  token: string | null
  refreshToken: string | null
  isAuthenticated: boolean
  setAuth: (accessToken: string, refreshToken?: string) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>()(devtools((set) => {
  // read stored tokens once at initialization
  const storedToken = localStorage.getItem('accessToken') || localStorage.getItem('token')
  const storedRefresh = localStorage.getItem('refreshToken')

  const claims = storedToken ? parseJwt(storedToken) : null
  const c = claims as Record<string, unknown> | null

  const extractAvatarUrl = (c: Record<string, unknown> | null): string | undefined => {
    if (!c) return undefined
    const rawValue = c['avatarUrl'] || c['picture'] || c['http://schemas.microsoft.com/ws/2008/06/identity/claims/thumbnailphoto']
    if (!rawValue) return undefined
    // Handle both string and array (in case of duplicate claims)
    return Array.isArray(rawValue) ? String(rawValue[0]) : String(rawValue)
  }

  const initialUser = c
    ? {
        id: String(
          c['sub'] ||
            c['nameid'] ||
            c['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
            c['id'] ||
            ''
        ),
        email: String(c['email'] || ''),
        name: String(c['name'] || c['preferred_username'] || c['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || ''),
        firstName: c['given_name'] || c['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'] ? String(c['given_name'] || c['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname']) : undefined,
        lastName: c['family_name'] || c['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'] ? String(c['family_name'] || c['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname']) : undefined,
        avatarUrl: extractAvatarUrl(c),
        roles: toArray(c['role'] || c['roles'] || c['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']),
        permissions: toArray(c['permission'] || c['permissions'])
      }
    : null

  return {
    user: initialUser,
    token: storedToken,
    refreshToken: storedRefresh,
    isAuthenticated: !!storedToken,
  
    // Parse JWT and create a small user stub from claims
    setAuth: (accessToken, refreshToken) => {
      try {
        // store tokens
        if (accessToken) {
          localStorage.setItem('accessToken', accessToken)
        }
        if (refreshToken) {
          localStorage.setItem('refreshToken', refreshToken)
        }

        const claims = accessToken ? parseJwt(accessToken) : null
        const c2 = claims as Record<string, unknown> | null
        const user = c2
          ? {
              id: String(
                c2['sub'] ||
                  c2['nameid'] ||
                  c2['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
                  c2['id'] ||
                  ''
              ),
              email: String(c2['email'] || ''),
              name: String(c2['name'] || c2['preferred_username'] || c2['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || ''),
              firstName: c2['given_name'] || c2['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'] ? String(c2['given_name'] || c2['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname']) : undefined,
              lastName: c2['family_name'] || c2['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'] ? String(c2['family_name'] || c2['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname']) : undefined,
              avatarUrl: extractAvatarUrl(c2),
              roles: toArray(c2['role'] || c2['roles'] || c2['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']),
              permissions: toArray(c2['permission'] || c2['permissions'])
            }
          : null

        set({ token: accessToken, refreshToken: refreshToken || null, user, isAuthenticated: !!accessToken })
      } catch {
        // on parse error still set token
        localStorage.setItem('accessToken', accessToken)
        set({ token: accessToken, refreshToken: refreshToken || null, user: null, isAuthenticated: !!accessToken })
      }
    },
  
  logout: () => {
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    queryClient.clear()
    set({ token: null, refreshToken: null, user: null, isAuthenticated: false })
  }
}
}, { name: 'AuthStore' }));
