<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <div class="mx-auto mt-24 max-w-sm rounded-lg border border-slate-200 bg-white p-8 text-center shadow-sm">
    <h1 class="text-2xl font-semibold">ERPPortal</h1>
    <p class="mt-2 text-sm text-slate-500">Sign in to continue</p>
    <button
      class="mt-6 w-full rounded bg-slate-900 px-4 py-2 text-white hover:bg-slate-700"
      @click="authStore.login()"
    >
      Log in with Keycloak
    </button>
  </div>
</template>
