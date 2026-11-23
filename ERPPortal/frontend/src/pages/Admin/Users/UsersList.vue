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
  <div class="space-y-4">
    <h1 class="text-2xl font-semibold">Users</h1>

    <p v-if="error" class="rounded border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">
      {{ error }}
    </p>
    <p v-if="loading" class="text-slate-500">Loading...</p>

    <div v-else class="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm">
      <table class="w-full text-left text-sm">
        <thead class="bg-slate-100 text-xs uppercase tracking-wide text-slate-500">
          <tr>
            <th class="px-4 py-3">User</th>
            <th class="px-4 py-3">Roles</th>
            <th class="px-4 py-3">Assign a role</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100">
          <tr v-for="user in users" :key="user.id">
            <td class="px-4 py-3">
              <div class="font-medium">{{ user.displayName }}</div>
              <div class="text-slate-500">{{ user.email }}</div>
            </td>
            <td class="px-4 py-3">
              <div class="flex flex-wrap gap-2">
                <span
                  v-for="role in user.roles"
                  :key="role"
                  class="inline-flex items-center gap-1 rounded-full bg-sky-100 px-3 py-1 text-sky-800"
                >
                  {{ role }}
                  <button
                    class="text-sky-600 hover:text-red-600"
                    title="Remove role"
                    @click="removeRole(user.id, role)"
                  >
                    &times;
                  </button>
                </span>
                <span v-if="!user.roles.length" class="text-slate-400">No roles</span>
              </div>
            </td>
            <td class="px-4 py-3">
              <select
                class="rounded border border-slate-300 bg-white px-2 py-1"
                @change="(e) => assignRole(user.id, (e.target as HTMLSelectElement).value)"
              >
                <option value="">Pick a role</option>
                <option v-for="role in roles" :key="role.id" :value="role.name">
                  {{ role.name }}
                </option>
              </select>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
