import { defineStore } from 'pinia'
import { authService } from '@/services/auth/authService'

interface CurrentUser {
  username: string | null
  email: string | null
  permissions: string[]
}

interface AuthState {
  currentUser: CurrentUser | null
  isInitializing: boolean
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    currentUser: null,
    isInitializing: true,
  }),

  getters: {
    isAuthenticated: (state) => state.currentUser !== null,
    can: (state) => (permission: string) =>
      state.currentUser?.permissions.includes(permission) ?? false,
  },

  actions: {
    async initialize() {
      this.isInitializing = true
      try {
        const user = await authService.trySilentSignIn()
        if (user) {
          await this.loadCurrentUser()
        }
      } finally {
        this.isInitializing = false
      }
    },

    async login() {
      await authService.signIn()
    },

    async handleLoginCallback() {
      await authService.handleSignInCallback()
      await this.loadCurrentUser()
    },

    async loadCurrentUser() {
      const token = await authService.getAccessToken()
      if (!token) return

      const response = await fetch('http://localhost:5000/api/v1/me', {
        headers: { Authorization: `Bearer ${token}` },
      })

      if (!response.ok) {
        this.currentUser = null
        return
      }

      const data = await response.json()
      this.currentUser = {
        username: data.username,
        email: data.email,
        permissions: data.permissions,
      }
    },

    async logout() {
      await authService.signOut()
      this.currentUser = null
    },
  },
})

