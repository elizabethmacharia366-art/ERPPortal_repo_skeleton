<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

function handleLogin() {
  authStore.login()
}
</script>

<template>
  <div>
    <h1>ERPPortal</h1>
    <button @click="handleLogin">Log in with Keycloak</button>
  </div>
</template>
