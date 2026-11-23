<script setup lang="ts">
import Navbar from '@/components/Navbar/Navbar.vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <div class="min-h-screen bg-slate-50 text-slate-800">
    <Navbar v-if="authStore.isAuthenticated" />
    <main class="mx-auto max-w-5xl px-6 py-8">
      <router-view />
    </main>
  </div>
</template>
