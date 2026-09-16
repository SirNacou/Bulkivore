<script lang="ts" setup>
import type { NavigationMenuItem } from '@nuxt/ui'

const mainItems: NavigationMenuItem[] = [
  {
    label: 'Imports',
    icon: 'i-lucide-upload',
    to: '/imports'
  },
  {
    label: 'Schema',
    icon: 'i-lucide-table',
    to: '/schema'
  },
  {
    label: 'Settings',
    icon: 'i-lucide-settings',
    to: '/settings'
  }
]

const secondaryItems: NavigationMenuItem[] = [
  {
    label: 'Documentation',
    icon: 'i-lucide-book-open',
    to: 'https://ui.nuxt.com',
    target: '_blank'
  }
]
</script>

<template>
  <UDashboardGroup>
    <UDashboardSidebar collapsible :ui="{ footer: 'border-t border-default' }">
      <template #header="{ collapsed }">
        <div class="flex items-center gap-2">
          <UIcon name="i-lucide-package" class="size-5 text-primary shrink-0" />
          <span v-if="!collapsed" class="font-semibold text-highlighted text-sm truncate">
            Bulkivore
          </span>
        </div>
      </template>

      <template #default="{ collapsed }">
        <UNavigationMenu :collapsed="collapsed" :items="mainItems" orientation="vertical" />

        <UNavigationMenu :collapsed="collapsed" :items="secondaryItems" orientation="vertical" class="mt-auto" />
      </template>

      <template #footer="{ collapsed }">
        <UButton color="neutral" variant="ghost" class="w-full" :block="collapsed" :square="collapsed"
          icon="i-lucide-circle-user-round" :label="collapsed ? undefined : 'Account'" />
      </template>
    </UDashboardSidebar>

    <UDashboardPanel>
      <template #header>
        <UDashboardNavbar title="Bulkivore">
          <template #leading>
            <UDashboardSidebarCollapse />
          </template>

          <template #right>
            <UColorModeButton />
          </template>
        </UDashboardNavbar>
      </template>

      <template #body>
        <div class="flex flex-col gap-3">
          <slot />
        </div>
      </template>
    </UDashboardPanel>
  </UDashboardGroup>
</template>