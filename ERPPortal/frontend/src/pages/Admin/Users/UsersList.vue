<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { apiClient } from '@/services/api/apiClient'

interface User {
  id: string
  email: string
  displayName: string
  isActive: boolean
  roles: string[]
}

interface Role {
  id: string
  name: string
}

const users = ref<User[]>([])
const roles = ref<Role[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

async function loadData() {
  loading.value = true
  error.value = null
  try {
    const [usersResult, rolesResult] = await Promise.all([
      apiClient.get<User[]>('/api/v1/admin/users'),
      apiClient.get<Role[]>('/api/v1/admin/roles'),
    ])
    users.value = usersResult
    roles.value = rolesResult
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load data.'
  } finally {
    loading.value = false
  }
}

function roleIdByName(name: string): string | undefined {
  return roles.value.find((r) => r.name === name)?.id
}

async function assignRole(userId: string, roleName: string) {
  const roleId = roleIdByName(roleName)
  if (!roleId) return
  try {
    await apiClient.post(`/api/v1/admin/users/${userId}/roles/${roleId}`)
    await loadData()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to assign role.'
  }
}

async function removeRole(userId: string, roleName: string) {
  const roleId = roleIdByName(roleName)
  if (!roleId) return
  try {
    await apiClient.delete(`/api/v1/admin/users/${userId}/roles/${roleId}`)
    await loadData()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to remove role.'
  }
}

onMounted(loadData)
</script>

<template>
  <div>
    <h1>Users</h1>

    <p v-if="error" style="color: red">{{ error }}</p>
    <p v-if="loading">Loading...</p>

    <table v-else border="1" cellpadding="6">
      <thead>
        <tr>
          <th>Email</th>
          <th>Display Name</th>
          <th>Roles</th>
          <th>Assign a role</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="user in users" :key="user.id">
          <td>{{ user.email }}</td>
          <td>{{ user.displayName }}</td>
          <td>
            <span v-for="role in user.roles" :key="role">
              {{ role }}
              <button @click="removeRole(user.id, role)">✕</button>
            </span>
            <span v-if="!user.roles.length">(none)</span>
          </td>
          <td>
            <select @change="(e) => assignRole(user.id, (e.target as HTMLSelectElement).value)">
              <option value="">-- pick a role --</option>
              <option v-for="role in roles" :key="role.id" :value="role.name">
                {{ role.name }}
              </option>
            </select>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
