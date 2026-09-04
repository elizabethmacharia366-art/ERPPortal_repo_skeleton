<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const router = useRouter()

onMounted(async () => {
  await authStore.handleLoginCallback()
  router.push('/')
})
</script>

<template>
  <p>Signing you in...</p>
</template>
