<script lang="ts" setup>
import { useQuery } from '@tanstack/vue-query'
import type { TableColumn } from '@nuxt/ui'
import type { ImportSessionStatus, ImportSummaryDto } from '~/client'
import { listImportsOptions } from '~/client/@tanstack/vue-query.gen'

const page = ref(1)
const pageSize = ref(20)

const pageSizeOptions = [10, 20, 50, 100]

watch(pageSize, () => {
  page.value = 1
})

const { data, isPending } = useQuery(computed(() => listImportsOptions({
  query: { page: page.value, pageSize: pageSize.value }
})))

const rows = computed(() => data.value?.items ?? [])
const total = computed(() => data.value?.totalCount ?? 0)

const statusColor: Record<ImportSessionStatus, 'success' | 'error' | 'info' | 'warning' | 'neutral'> = {
  Completed: 'success',
  Failed: 'error',
  Ingesting: 'info',
  Mapped: 'warning',
  Initialized: 'neutral'
}

const dateFormatter = new Intl.DateTimeFormat('en-US', {
  dateStyle: 'medium',
  timeStyle: 'short'
})
function formatDate(value: string | null): string {
  if (!value)
    return '—'
  return dateFormatter.format(new Date(value))
}

const numberFormatter = new Intl.NumberFormat('en-US');
function formatNumber(value: number): string {
  return numberFormatter.format(value)
}

const columns: TableColumn<ImportSummaryDto>[] = [
  {
    accessorKey: 'fileName',
    header: 'File'
  },
  {
    accessorKey: 'targetTable',
    header: 'Target table'
  },
  {
    accessorKey: 'status',
    header: 'Status'
  },
  {
    accessorKey: 'processedRows',
    header: 'Processed'
  },
  {
    accessorKey: 'successRowCount',
    header: 'Succeeded'
  },
  {
    accessorKey: 'failedRowCount',
    header: 'Failed'
  },
  {
    accessorKey: 'createdAt',
    header: 'Created'
  },
  {
    accessorKey: 'completedAt',
    header: 'Completed'
  }
]
</script>

<template>
  <div class="flex flex-col gap-4">
    <UTable ref="table" class="min-h-48 max-h-[calc(100dvh-25rem)]" sticky :data="rows" :columns="columns"
      :loading="isPending">
      <template #status-cell="{ row }">
        <UBadge :color="statusColor[row.original.status]" variant="subtle">
          {{ row.original.status }}
        </UBadge>
      </template>

      <template #processedRows-cell="{ row }">
        {{ formatNumber(row.original.processedRows) }}
      </template>

      <template #successRowCount-cell="{ row }">
        {{ formatNumber(row.original.successRowCount) }}
      </template>

      <template #failedRowCount-cell="{ row }">
        {{ formatNumber(row.original.failedRowCount) }}
      </template>

      <template #createdAt-cell="{ row }">
        <span class="whitespace-nowrap">{{ formatDate(row.original.createdAt) }}</span>
      </template>

      <template #completedAt-cell="{ row }">
        <span class="whitespace-nowrap">{{ formatDate(row.original.completedAt) }}</span>
      </template>

      <template #empty>
        No imports yet.
      </template>
    </UTable>

    <div class="flex justify-between items-center gap-4">
      <div class="flex items-center gap-2">
        <span class="text-muted text-sm">Rows per page</span>
        <USelectMenu v-model="pageSize" :items="pageSizeOptions" class="w-24" />
      </div>
      <UPagination v-if="total > pageSize" v-model:page="page" :items-per-page="pageSize" :total="total" />
    </div>
  </div>
</template>