import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes = [
  {
    path: '/',
    name: 'dashboard',
    component: () => import('@/pages/Dashboard/Dashboard.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/login',
    name: 'login',
    component: () => import('@/pages/Login/Login.vue'),
    meta: { requiresAuth: false },
  },
  {
    path: '/callback',
    name: 'callback',
    component: () => import('@/pages/Callback/Callback.vue'),
    meta: { requiresAuth: false },
  },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach(async (to) => {
  const authStore = useAuthStore()

  if (authStore.isInitializing) {
    await authStore.initialize()
  }

  const requiresAuth = to.meta.requiresAuth !== false
  if (requiresAuth && !authStore.isAuthenticated) {
    return { name: 'login' }
  }

  return true
})

export default router
