<script lang="ts" setup>
import { useQuery } from '@tanstack/vue-query'
import { getTableSchemaOptions } from '~/client/@tanstack/vue-query.gen'

const count = ref(0)
const { data } = useQuery(getTableSchemaOptions({ path: { tableName: 'products' } }))
</script>

<template>
  <p>Hello</p>
  <UButton @click="count++">{{ count }}</UButton>
  <div v-for="(item, index) in data" :key="index">
    {{ item.name }} - {{ item.dataType }}
  </div>
</template>