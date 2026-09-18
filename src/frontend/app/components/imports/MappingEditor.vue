<script lang="ts" setup>
import { useMutation } from '@tanstack/vue-query'
import type { ColumnMapping, ColumnMatch, ColumnMetadata, ProblemDetails } from '~/client'
import { setImportMappingsMutation } from '~/client/@tanstack/vue-query.gen'

const IGNORE = '__ignore__'

const props = defineProps<{
  sessionId: string
  headers: string[]
  targetColumns: ColumnMetadata[]
  suggestedMappings: ColumnMatch[]
}>()

const emit = defineEmits<{
  back: []
  complete: [mappings: ColumnMapping[]]
}>()

const selection = ref<Record<string, string>>({})
const errorMessage = ref<string | null>(null)

const saveMutation = useMutation(setImportMappingsMutation())
const isSaving = computed(() => saveMutation.isPending.value)

// Prefill from the backend's suggested mappings (only where the target still exists).
watchEffect(() => {
  const targets = new Set(props.targetColumns.map(c => c.name))
  const next: Record<string, string> = {}
  for (const header of props.headers) {
    const suggestion = props.suggestedMappings.find(s => s.sourceHeader === header)
    next[header] = suggestion && targets.has(suggestion.targetColumn)
      ? suggestion.targetColumn
      : IGNORE
  }
  selection.value = next
})

const targetOptions = computed(() => [
  { label: '— Do not import —', value: IGNORE },
  ...props.targetColumns.map(column => ({
    label: column.name,
    value: column.name,
    description: column.isWritable === false ? `Read-only (${column.dataType})` : column.dataType,
    disabled: column.isWritable === false
  }))
])

const mappedTargets = computed(() =>
  Object.values(selection.value).filter(target => target !== IGNORE))

const duplicateTargets = computed(() => {
  const seen = new Set<string>()
  const duplicates = new Set<string>()
  for (const target of mappedTargets.value) {
    if (seen.has(target))
      duplicates.add(target)
    seen.add(target)
  }
  return [...duplicates]
})

const missingRequired = computed(() =>
  props.targetColumns
    .filter(column => column.isRequired && !mappedTargets.value.includes(column.name))
    .map(column => column.name))

const validationError = computed<string | null>(() => {
  if (duplicateTargets.value.length > 0)
    return `Each target column can be mapped only once: ${duplicateTargets.value.join(', ')}.`
  if (missingRequired.value.length > 0)
    return `Required columns must be mapped: ${missingRequired.value.join(', ')}.`
  if (mappedTargets.value.length === 0)
    return 'Map at least one column to continue.'
  return null
})

function suggestionBadge(header: string): string | null {
  const suggestion = props.suggestedMappings.find(s => s.sourceHeader === header)
  if (!suggestion || suggestion.targetColumn !== selection.value[header])
    return null
  return suggestion.isAutoMatched
    ? `Auto-matched (${Math.round(suggestion.confidence * 100)}%)`
    : 'Suggested'
}

function toErrorMessage(error: unknown): string {
  if (typeof error === 'object' && error !== null && ('detail' in error || 'title' in error)) {
    const details = error as ProblemDetails
    return details.detail || details.title || 'Saving mappings failed.'
  }
  if (error instanceof Error && error.message)
    return error.message
  return 'Saving mappings failed. Please try again.'
}

async function handleSave(): Promise<void> {
  if (validationError.value || isSaving.value)
    return
  errorMessage.value = null

  const mappings: ColumnMapping[] = []
  for (const header of props.headers) {
    const target = selection.value[header]
    if (target === undefined || target === IGNORE)
      continue
    mappings.push({ sourceHeader: header, targetColumn: target })
  }

  try {
    await saveMutation.mutateAsync({
      path: { id: props.sessionId },
      body: { mappings }
    })
    emit('complete', mappings)
  }
  catch (error) {
    errorMessage.value = toErrorMessage(error)
  }
}
</script>

<template>
  <div class="flex flex-col gap-4">
    <div class="flex flex-col gap-3">
      <div v-for="header in headers" :key="header" class="flex items-center gap-3">
        <div class="flex-1 min-w-0">
          <div class="font-medium truncate">{{ header }}</div>
          <div v-if="suggestionBadge(header)" class="text-muted text-xs">
            {{ suggestionBadge(header) }}
          </div>
        </div>
        <UIcon name="i-lucide-arrow-right" class="size-4 text-muted shrink-0" />
        <USelectMenu v-model="selection[header]" :items="targetOptions" value-key="value" class="w-64 shrink-0"
          :disabled="isSaving" />
      </div>
    </div>

    <UAlert v-if="validationError" color="warning" variant="subtle" :description="validationError" />
    <UAlert v-if="errorMessage" color="error" variant="subtle" title="Saving mappings failed"
      :description="errorMessage" />

    <div class="flex justify-between">
      <UButton variant="ghost" icon="i-lucide-arrow-left" label="Back" :disabled="isSaving" @click="emit('back')" />
      <UButton icon="i-lucide-arrow-right" label="Save & continue" :loading="isSaving"
        :disabled="validationError !== null" @click="handleSave" />
    </div>
  </div>
</template>