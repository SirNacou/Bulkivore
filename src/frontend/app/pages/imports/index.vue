<script lang="ts" setup>
import ImportTable from '~/components/imports/ImportTable.vue'
</script>

<template>
  <div class="flex flex-col gap-6">
    <UPageHeader title="Imports" description="Track bulk import sessions."
      :links="[{ label: 'New import', icon: 'i-lucide-plus', disabled: true }]" />

    <UCard>
      <ImportTable />
    </UCard>
  </div>
</template>