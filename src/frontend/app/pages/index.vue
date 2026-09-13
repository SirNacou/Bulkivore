<script lang="ts" setup>
import { useQuery } from '@tanstack/vue-query'
import { getTableSchemaQueryOptions } from '~/gen/hooks'

const count = ref(0)
const { data, isLoading, error } = useQuery(getTableSchemaQueryOptions({ path: { tableName: "products" } }))
</script>

<template>
  <p>Hello</p>
  <UButton @click="count++">{{ count }}</UButton>
  <div v-for="value in data">
    {{ value.name }} - {{ value.dataType }}
  </div>
</template>