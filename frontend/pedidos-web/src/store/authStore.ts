import { create } from 'zustand'
import { authApi, TOKEN_KEY } from '../api/client'
import type { User } from '../types'

interface AuthState {
  token: string | null
  user: User | null
  loading: boolean
  error: string | null

  login: (email: string, password: string) => Promise<void>
  logout: () => void
  restore: () => Promise<void>
}

export const useAuthStore = create<AuthState>((set) => ({
  token: localStorage.getItem(TOKEN_KEY),
  user: null,
  loading: false,
  error: null,

  login: async (email, password) => {
    set({ loading: true, error: null })
    try {
      const result = await authApi.login(email, password)
      localStorage.setItem(TOKEN_KEY, result.token)
      set({ token: result.token, user: result.user, loading: false })
    } catch (e) {
      set({ loading: false, error: extractError(e) })
      throw e
    }
  },

  logout: () => {
    localStorage.removeItem(TOKEN_KEY)
    set({ token: null, user: null })
  },

  restore: async () => {
    const token = localStorage.getItem(TOKEN_KEY)
    if (!token) return
    try {
      const user = await authApi.me()
      set({ token, user })
    } catch {
      localStorage.removeItem(TOKEN_KEY)
      set({ token: null, user: null })
    }
  },
}))

window.addEventListener('auth:logout', () => useAuthStore.getState().logout())

function extractError(e: unknown): string {
  if (typeof e === 'object' && e !== null && 'response' in e) {
    const resp = (e as { response?: { status?: number; data?: { detail?: string } } }).response
    if (resp?.status === 401) return 'E-mail ou senha inválidos.'
    if (resp?.data?.detail) return resp.data.detail
  }
  return 'Falha ao autenticar.'
}
