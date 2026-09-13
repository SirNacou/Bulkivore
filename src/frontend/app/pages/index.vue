<script lang="ts" setup>
const count = ref(0)
</script>

<template>
  <p>Hello</p>
  <UButton @click="count++">{{ count }}</UButton>
</template>