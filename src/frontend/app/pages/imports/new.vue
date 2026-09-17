<script lang="ts" setup>
import ImportWizard from '~/components/imports/ImportWizard.vue'
</script>

<template>
  <div class="flex flex-col gap-6 max-w-3xl w-full mx-auto">
    <UPageHeader title="New import" description="Upload a file, map its columns, and commit the import."
      :links="[{ label: 'Back to imports', icon: 'i-lucide-arrow-left', to: '/imports', variant: 'ghost' }]" />

    <UCard>
      <ImportWizard />
    </UCard>
  </div>
</template>
