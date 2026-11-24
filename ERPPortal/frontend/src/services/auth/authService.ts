import { UserManager, WebStorageStateStore, type User } from 'oidc-client-ts'

const userManager = new UserManager({
  authority: 'http://localhost:8080/realms/erpportal',
  client_id: 'erpportal-api',
  redirect_uri: 'http://localhost:5173/callback',
  post_logout_redirect_uri: 'http://localhost:5173/',
  response_type: 'code',
  scope: 'openid profile email',
  userStore: new WebStorageStateStore({ store: window.localStorage }),
  // Renew the access token shortly before it expires (uses the refresh token).
  automaticSilentRenew: true,
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

  /** Try to get a fresh access token. Returns false if the session can't be renewed. */
  async refresh(): Promise<boolean> {
    try {
      const user = await userManager.signinSilent()
      return user !== null
    } catch {
      return false
    }
  }

  /** Forget the local session without a round trip to Keycloak. */
  async clearSession(): Promise<void> {
    await userManager.removeUser()
  }

  async getAccessToken(): Promise<string | null> {
    let user = await userManager.getUser()
    if (user?.expired) {
      await this.refresh()
      user = await userManager.getUser()
    }
    return user && !user.expired ? user.access_token : null
  }
}

export const authService = new AuthService()
