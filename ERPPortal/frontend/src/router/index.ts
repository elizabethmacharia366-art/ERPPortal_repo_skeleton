import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  {
    path: '/',
    name: 'dashboard',
    component: () => import('@/pages/Dashboard/Dashboard.vue'),
  },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
})

export default router
