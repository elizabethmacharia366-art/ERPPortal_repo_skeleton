<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-2xl font-semibold">Dashboard</h1>
      <p v-if="authStore.currentUser" class="mt-1 text-slate-600">
        Signed in as <strong>{{ authStore.currentUser.username }}</strong>
        ({{ authStore.currentUser.email }})
      </p>
    </div>

    <section class="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <h2 class="mb-3 text-sm font-medium uppercase tracking-wide text-slate-500">Your permissions</h2>
      <div class="flex flex-wrap gap-2">
        <span
          v-for="permission in authStore.currentUser?.permissions"
          :key="permission"
          class="rounded-full bg-emerald-100 px-3 py-1 text-sm text-emerald-800"
        >
          {{ permission }}
        </span>
        <span v-if="!authStore.currentUser?.permissions.length" class="text-sm text-slate-500">
          No permissions assigned yet.
        </span>
      </div>
    </section>

    <section v-if="authStore.can('Roles.Manage')" class="grid gap-4 sm:grid-cols-2">
      <router-link
        to="/admin/roles"
        class="rounded-lg border border-slate-200 bg-white p-5 shadow-sm hover:border-slate-400"
      >
        <h3 class="font-medium">Roles &amp; Permissions</h3>
        <p class="mt-1 text-sm text-slate-500">Create roles and see what each one can do.</p>
      </router-link>
      <router-link
        to="/admin/users"
        class="rounded-lg border border-slate-200 bg-white p-5 shadow-sm hover:border-slate-400"
      >
        <h3 class="font-medium">Users</h3>
        <p class="mt-1 text-sm text-slate-500">Assign or remove roles for each user.</p>
      </router-link>
    </section>
  </div>
</template>
