import { UserManager, WebStorageStateStore, type User } from 'oidc-client-ts'

// Points at our real Keycloak realm and the erpportal-api client we created.
// In a real multi-environment setup, these values would come from environment
// variables (VITE_KEYCLOAK_AUTHORITY, etc.) rather than being hardcoded here.
const userManager = new UserManager({
  authority: 'http://localhost:8080/realms/erpportal',
  client_id: 'erpportal-api',
  redirect_uri: 'http://localhost:5173/callback',
  post_logout_redirect_uri: 'http://localhost:5173/',
  response_type: 'code',
  scope: 'openid profile email',
  userStore: new WebStorageStateStore({ store: window.localStorage }),
})

class AuthService {
  async signIn(): Promise<void> {
    await userManager.signinRedirect()
  }

  async handleSignInCallback(): Promise<User | null> {
    return userManager.signinRedirectCallback()
  }

  async trySilentSignIn(): Promise<User | null> {
    return userManager.getUser()
  }

  async signOut(): Promise<void> {
    await userManager.signoutRedirect()
  }

  async getAccessToken(): Promise<string | null> {
    const user = await userManager.getUser()
    return user?.access_token ?? null
  }
}

export const authService = new AuthService()
