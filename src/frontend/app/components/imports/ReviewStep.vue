<script lang="ts" setup>
import { useMutation } from '@tanstack/vue-query'
import type { ColumnMapping } from '~/client'
import { commitImportMutation } from '~/client/@tanstack/vue-query.gen'

const props = defineProps<{
  sessionId: string
  targetTable: string
  fileName: string
  mappings: ColumnMapping[]
}>()

const emit = defineEmits<{
  back: []
}>()

// Bulk imports can take far longer than the client's default 10s timeout,
// so disable the timeout for this request only.
const commitMutation = useMutation(commitImportMutation({ timeout: false }))
const errorMessage = ref<string | null>(null)

const isCommitting = computed(() => commitMutation.isPending.value)

async function handleCommit(): Promise<void> {
  if (isCommitting.value)
    return
  errorMessage.value = null

  try {
    const result = await commitMutation.mutateAsync({ path: { id: props.sessionId } })
    await navigateTo(`/imports/${result.id}`)
  }
  catch (error) {
    if (error instanceof Error && error.message)
      errorMessage.value = error.message
    else
      errorMessage.value = 'Commit failed. You can go back and adjust the mappings, then try again.'
  }
}
</script>

<template>
  <div class="flex flex-col gap-4">
    <dl class="grid grid-cols-2 gap-3 text-sm">
      <dt class="text-muted">File</dt>
      <dd class="font-medium truncate">{{ fileName }}</dd>
      <dt class="text-muted">Target table</dt>
      <dd class="font-medium truncate">{{ targetTable }}</dd>
      <dt class="text-muted">Mapped columns</dt>
      <dd class="font-medium">{{ mappings.length }}</dd>
    </dl>

    <UTable :data="mappings" :columns="[
      { accessorKey: 'sourceHeader', header: 'Source header' },
      { accessorKey: 'targetColumn', header: 'Target column' }
    ]" />

    <UAlert v-if="errorMessage" color="error" variant="subtle" title="Commit failed"
      :description="errorMessage" />

    <div class="flex justify-between">
      <UButton variant="ghost" icon="i-lucide-arrow-left" label="Back to mapping"
        :disabled="isCommitting" @click="emit('back')" />
      <UButton icon="i-lucide-check" label="Commit import" color="primary"
        :loading="isCommitting" :disabled="isCommitting" @click="handleCommit" />
    </div>
  </div>
</template>
