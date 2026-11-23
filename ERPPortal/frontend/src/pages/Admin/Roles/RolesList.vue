<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { apiClient } from '@/services/api/apiClient'

interface Role {
  id: string
  name: string
  description: string | null
  permissions: string[]
}

const roles = ref<Role[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const newRoleName = ref('')
const newRoleDescription = ref('')

async function loadRoles() {
  loading.value = true
  error.value = null
  try {
    roles.value = await apiClient.get<Role[]>('/api/v1/admin/roles')
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load roles.'
  } finally {
    loading.value = false
  }
}

async function createRole() {
  if (!newRoleName.value.trim()) return
  try {
    await apiClient.post('/api/v1/admin/roles', {
      name: newRoleName.value,
      description: newRoleDescription.value || null,
    })
    newRoleName.value = ''
    newRoleDescription.value = ''
    await loadRoles()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to create role.'
  }
}

onMounted(loadRoles)
</script>

<template>
  <div>
    <h1>Roles & Permissions</h1>

    <p v-if="error" style="color: red">{{ error }}</p>
    <p v-if="loading">Loading...</p>

    <table v-else border="1" cellpadding="6">
      <thead>
        <tr>
          <th>Name</th>
          <th>Description</th>
          <th>Permissions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="role in roles" :key="role.id">
          <td>{{ role.name }}</td>
          <td>{{ role.description }}</td>
          <td>{{ role.permissions.join(', ') || '(none)' }}</td>
        </tr>
      </tbody>
    </table>

    <h2>Create a role</h2>
    <input v-model="newRoleName" placeholder="Role name" />
    <input v-model="newRoleDescription" placeholder="Description (optional)" />
    <button @click="createRole">Create</button>
  </div>
</template>
