<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

function handleLogout() {
  authStore.logout()
}
</script>

<template>
  <div>
    <h1>ERPPortal Dashboard</h1>

    <p v-if="authStore.currentUser">
      Logged in as: <strong>{{ authStore.currentUser.username }}</strong>
      ({{ authStore.currentUser.email }})
    </p>

    <p>Your permissions:</p>
    <ul>
      <li v-for="permission in authStore.currentUser?.permissions" :key="permission">
        {{ permission }}
      </li>
    </ul>

    <p v-if="authStore.can('Roles.Manage')">
      ✅ You can manage roles and permissions.
    </p>
    <p v-else>
      🚫 You do not have permission to manage roles.
    </p>

    <button @click="handleLogout">Log out</button>
  </div>
</template>
