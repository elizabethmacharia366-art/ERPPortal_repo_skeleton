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
  <div class="space-y-6">
    <h1 class="text-2xl font-semibold">Roles &amp; Permissions</h1>

    <p v-if="error" class="rounded border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">
      {{ error }}
    </p>
    <p v-if="loading" class="text-slate-500">Loading...</p>

    <div v-else class="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm">
      <table class="w-full text-left text-sm">
        <thead class="bg-slate-100 text-xs uppercase tracking-wide text-slate-500">
          <tr>
            <th class="px-4 py-3">Role</th>
            <th class="px-4 py-3">Permissions</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100">
          <tr v-for="role in roles" :key="role.id">
            <td class="px-4 py-3">
              <div class="font-medium">{{ role.name }}</div>
              <div class="text-slate-500">{{ role.description }}</div>
            </td>
            <td class="px-4 py-3">
              <div class="flex flex-wrap gap-2">
                <span
                  v-for="permission in role.permissions"
                  :key="permission"
                  class="rounded-full bg-emerald-100 px-3 py-1 text-emerald-800"
                >
                  {{ permission }}
                </span>
                <span v-if="!role.permissions.length" class="text-slate-400">No permissions</span>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <section class="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <h2 class="mb-3 text-sm font-medium uppercase tracking-wide text-slate-500">Create a role</h2>
      <div class="flex flex-wrap gap-3">
        <input
          v-model="newRoleName"
          placeholder="Role name"
          class="rounded border border-slate-300 px-3 py-2 text-sm"
        />
        <input
          v-model="newRoleDescription"
          placeholder="Description (optional)"
          class="min-w-64 flex-1 rounded border border-slate-300 px-3 py-2 text-sm"
        />
        <button
          class="rounded bg-slate-900 px-4 py-2 text-sm text-white hover:bg-slate-700"
          @click="createRole"
        >
          Create
        </button>
      </div>
    </section>
  </div>
</template>
