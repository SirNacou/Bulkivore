<script setup lang="ts">
const count = ref(0)
const increment = () => {
    count.value++
}

</script>

<template>
    <div>
        <h1>Welcome to the homepage</h1>
        <AppAlert>
            This is an auto-imported component
        </AppAlert>
        <button @click="increment">Count: {{ count }}</button>
    </div>
</template>