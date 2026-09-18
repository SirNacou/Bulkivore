<script lang="ts" setup>
import { useQuery } from '@tanstack/vue-query'
import type { StepperItem, TableColumn } from '@nuxt/ui'
import type { ColumnMapping, InspectSessionResponse } from '~/client'
import { inspectSessionOptions } from '~/client/@tanstack/vue-query.gen'
import MappingEditor from '~/components/imports/MappingEditor.vue'
import ReviewStep from '~/components/imports/ReviewStep.vue'
import UploadStep from '~/components/imports/UploadStep.vue'

type Step = 'upload' | 'inspect' | 'map' | 'review'

const step = ref<Step>('upload')
const sessionId = ref('')
const targetTable = ref('')
const fileName = ref('')
const mappings = ref<ColumnMapping[]>([])

const steps = computed<StepperItem[]>(() => [
  {
    value: 'upload',
    title: 'Upload',
    description: 'File and target table',
    icon: 'i-lucide-upload'
  },
  {
    value: 'inspect',
    title: 'Inspect',
    description: 'Headers and preview',
    icon: 'i-lucide-scan-search',
    disabled: sessionId.value === ''
  },
  {
    value: 'map',
    title: 'Map',
    description: 'Match columns',
    icon: 'i-lucide-arrow-right-left',
    disabled: !inspectData.value
  },
  {
    value: 'review',
    title: 'Review & commit',
    description: 'Confirm and import',
    icon: 'i-lucide-check',
    disabled: mappings.value.length === 0
  }
])

const inspectEnabled = computed(() => sessionId.value !== '')
const {
  data: inspectData,
  isPending: isInspectPending,
  isError: isInspectError,
  error: inspectError,
  refetch: refetchInspect
} = useQuery(computed(() => ({
  ...inspectSessionOptions({ path: { id: sessionId.value } }),
  enabled: inspectEnabled.value
})))

function inspectErrorMessage(error: unknown): string {
  if (typeof error === 'object' && error !== null && 'detail' in error)
    return String((error as { detail: unknown }).detail || 'File inspection failed.')
  if (error instanceof Error && error.message)
    return error.message
  return 'File inspection failed. The file may be unreadable — try uploading again.'
}

const suggestedColumns: TableColumn<InspectSessionResponse['suggestedMappings'][number]>[] = [
  { accessorKey: 'sourceHeader', header: 'Source header' },
  { accessorKey: 'targetColumn', header: 'Suggested target' },
  { accessorKey: 'confidence', header: 'Confidence' }
]

const previewColumns = computed<TableColumn<Record<string, string>>[]>(() =>
  (inspectData.value?.headers ?? []).map(header => ({ accessorKey: header, header })))

function stringifyCell(value: unknown): string {
  if (value === null || value === undefined)
    return '—'
  if (typeof value === 'object')
    return JSON.stringify(value)
  return String(value)
}

const previewRows = computed<Record<string, string>[]>(() => {
  const headers = inspectData.value?.headers ?? []
  return (inspectData.value?.previewRows ?? []).map(row =>
    Object.fromEntries(headers.map(header => [header, stringifyCell(row[header])])))
})

function handleUploadComplete(payload: { sessionId: string, targetTable: string, fileName: string }): void {
  sessionId.value = payload.sessionId
  targetTable.value = payload.targetTable
  fileName.value = payload.fileName
  mappings.value = []
  step.value = 'inspect'
}

function handleBackToUpload(): void {
  sessionId.value = ''
  mappings.value = []
  step.value = 'upload'
}

function handleMappingsComplete(saved: ColumnMapping[]): void {
  mappings.value = saved
  step.value = 'review'
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <UStepper v-model="step" :items="steps" linear class="w-full" />

    <UploadStep v-if="step === 'upload'" @complete="handleUploadComplete" />

    <div v-else-if="step === 'inspect'" class="flex flex-col gap-4">
      <div v-if="isInspectPending" class="flex flex-col gap-2">
        <USkeleton class="w-full h-8" />
        <USkeleton class="w-full h-32" />
        <p class="text-muted text-sm">Reading headers and preview rows…</p>
      </div>

      <template v-else-if="isInspectError">
        <UAlert color="error" variant="subtle" title="File inspection failed"
          :description="inspectErrorMessage(inspectError)" :actions="[
            { label: 'Try again', icon: 'i-lucide-refresh-cw', onClick: () => refetchInspect() }
          ]" />
        <div class="flex justify-start">
          <UButton variant="ghost" icon="i-lucide-arrow-left" label="Back to upload" @click="handleBackToUpload" />
        </div>
      </template>

      <template v-else-if="inspectData">
        <div>
          <h3 class="mb-2 font-medium">Suggested mappings</h3>
          <UTable :data="[...inspectData.suggestedMappings]" :columns="suggestedColumns">
            <template #targetColumn-cell="{ row }">
              {{ row.original.targetColumn || '—' }}
            </template>
            <template #confidence-cell="{ row }">
              {{ row.original.isAutoMatched ? `${Math.round(row.original.confidence * 100)}%` : '—' }}
            </template>
            <template #empty>No headers found.</template>
          </UTable>
        </div>

        <div>
          <h3 class="mb-2 font-medium">Preview (first {{ previewRows.length }} rows)</h3>
          <UTable :data="previewRows" :columns="previewColumns" class="max-h-[calc(100dvh-30rem)]" />
        </div>

        <div class="flex justify-between">
          <UButton variant="ghost" icon="i-lucide-arrow-left" label="Back to upload" @click="handleBackToUpload" />
          <UButton icon="i-lucide-arrow-right" label="Continue to mapping" @click="step = 'map'" />
        </div>
      </template>
    </div>

    <MappingEditor v-else-if="step === 'map' && inspectData" :session-id="sessionId" :headers="[...inspectData.headers]"
      :target-columns="[...inspectData.targetColumns]" :suggested-mappings="[...inspectData.suggestedMappings]"
      @back="step = 'inspect'" @complete="handleMappingsComplete" />

    <ReviewStep v-else-if="step === 'review'" :session-id="sessionId" :target-table="targetTable" :file-name="fileName"
      :mappings="mappings" @back="step = 'map'" />
  </div>
</template>