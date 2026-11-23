<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <header class="bg-slate-900 text-white">
    <div class="mx-auto flex max-w-5xl items-center justify-between px-6 py-3">
      <div class="flex items-center gap-6">
        <router-link to="/" class="text-lg font-semibold">ERPPortal</router-link>
        <nav v-if="authStore.can('Roles.Manage')" class="flex gap-4 text-sm text-slate-300">
          <router-link to="/admin/roles" class="hover:text-white" active-class="font-medium text-white">Roles</router-link>
          <router-link to="/admin/users" class="hover:text-white" active-class="font-medium text-white">Users</router-link>
        </nav>
      </div>
      <div class="flex items-center gap-4 text-sm">
        <span class="text-slate-300">{{ authStore.currentUser?.username }}</span>
        <button class="rounded bg-slate-700 px-3 py-1 hover:bg-slate-600" @click="authStore.logout()">
          Log out
        </button>
      </div>
    </div>
  </header>
</template>
