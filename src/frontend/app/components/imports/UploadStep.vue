<script lang="ts" setup>
import { useMutation } from '@tanstack/vue-query'
import ky from 'ky'
import type { ProblemDetails } from '~/client'
import { initializeSessionMutation } from '~/client/@tanstack/vue-query.gen'

const emit = defineEmits<{
  complete: [payload: { sessionId: string, targetTable: string, fileName: string }]
}>()

const MAX_FILE_SIZE = 100 * 1024 * 1024
const ALLOWED_EXTENSIONS = ['.csv', '.xlsx']

const targetTable = ref('')
const file = ref<File | null>(null)
const busyPhase = ref<'creating' | 'uploading' | null>(null)
const errorMessage = ref<string | null>(null)

const initializeMutation = useMutation(initializeSessionMutation())

const isBusy = computed(() => busyPhase.value !== null)
const canSubmit = computed(
  () => targetTable.value.trim() !== '' && file.value !== null && !isBusy.value
)

const busyLabel: Record<NonNullable<typeof busyPhase.value>, string> = {
  creating: 'Creating session…',
  uploading: 'Uploading file…'
}

function isProblemDetails(error: unknown): error is ProblemDetails {
  return typeof error === 'object' && error !== null && ('detail' in error || 'title' in error)
}

function toErrorMessage(error: unknown, fallback: string): string {
  if (isProblemDetails(error))
    return error.detail || error.title || fallback
  if (error instanceof Error && error.message)
    return error.message
  return fallback
}

function validateFile(selected: File): string | null {
  const extension = `.${selected.name.split('.').pop()}`.toLowerCase()
  if (selected.size === 0)
    return 'File is empty. Choose a file with content.'
  if (!ALLOWED_EXTENSIONS.includes(extension))
    return `File extension must be one of: ${ALLOWED_EXTENSIONS.join(', ')}.`
  if (selected.size > MAX_FILE_SIZE)
    return `File size cannot exceed ${MAX_FILE_SIZE / (1024 * 1024)} MB.`
  return null
}

async function handleSubmit(): Promise<void> {
  if (!canSubmit.value || !file.value)
    return

  const selectedFile = file.value
  const table = targetTable.value.trim()
  errorMessage.value = null

  const localError = validateFile(selectedFile)
  if (localError) {
    errorMessage.value = localError
    return
  }

  try {
    // 1. Create the session to get the presigned S3 upload URL.
    busyPhase.value = 'creating'
    const session = await initializeMutation.mutateAsync({
      body: { targetTable: table, fileName: selectedFile.name }
    })

    // 2. PUT raw bytes straight to storage (different host — bypass the API client).
    // File content itself is validated server-side by the inspect step.
    busyPhase.value = 'uploading'
    await ky.put(session.uploadUrl, { body: selectedFile })

    emit('complete', { sessionId: session.sessionId, targetTable: table, fileName: selectedFile.name })
  }
  catch (error) {
    errorMessage.value = toErrorMessage(error, 'Upload failed. Please try again.')
  }
  finally {
    busyPhase.value = null
  }
}
</script>

<template>
  <form class="flex flex-col gap-4" @submit.prevent="handleSubmit">
    <UFormField label="Target table" required
      hint="Schema-qualified table name, e.g. public.products">
      <UInput v-model="targetTable" placeholder="public.products" class="w-full" :disabled="isBusy" />
    </UFormField>

    <UFormField label="Data file" required hint="CSV or XLSX, up to 100 MB">
      <UFileUpload v-model="file" accept=".csv,.xlsx" label="Drop your file here or click to browse"
        description="CSV or XLSX" :disabled="isBusy" />
    </UFormField>

    <UAlert v-if="errorMessage" color="error" variant="subtle" title="Upload failed"
      :description="errorMessage" />

    <div class="flex justify-end">
      <UButton type="submit" icon="i-lucide-arrow-right" :loading="isBusy"
        :disabled="!canSubmit" :label="busyPhase ? busyLabel[busyPhase] : 'Continue'" />
    </div>
  </form>
</template>
