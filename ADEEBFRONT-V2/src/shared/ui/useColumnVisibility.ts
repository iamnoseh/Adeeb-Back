import { useEffect, useState } from 'react'

export type AdminListColumn = {
  id: string
  label: string
  locked?: boolean
  defaultVisible?: boolean
}

export function useColumnVisibility(storageKey: string, columns: AdminListColumn[]) {
  const signature = columns.map((column) => `${column.id}:${column.defaultVisible !== false}`).join('|')
  const [visibility, setVisibility] = useState<Record<string, boolean>>(() => readVisibility(storageKey, signature))

  useEffect(() => {
    setVisibility(readVisibility(storageKey, signature))
  }, [storageKey, signature])

  useEffect(() => {
    window.localStorage.setItem(storageKey, JSON.stringify(visibility))
  }, [storageKey, visibility])

  function isVisible(id: string) {
    return columns.find((column) => column.id === id)?.locked || visibility[id] !== false
  }

  function toggle(id: string) {
    const column = columns.find((item) => item.id === id)
    if (!column || column.locked) return
    setVisibility((current) => ({ ...current, [id]: current[id] === false }))
  }

  function reset() {
    setVisibility(defaultVisibility(signature))
  }

  function showAll() {
    setVisibility(Object.fromEntries(columns.map((column) => [column.id, true])))
  }

  function hideOptional() {
    setVisibility(Object.fromEntries(columns.map((column) => [column.id, Boolean(column.locked)])))
  }

  return { isVisible, toggle, reset, showAll, hideOptional }
}

function readVisibility(storageKey: string, signature: string) {
  try {
    const stored = JSON.parse(window.localStorage.getItem(storageKey) ?? '{}') as Record<string, boolean>
    return { ...defaultVisibility(signature), ...stored }
  } catch {
    return defaultVisibility(signature)
  }
}

function defaultVisibility(signature: string) {
  return Object.fromEntries(signature.split('|').filter(Boolean).map((item) => {
    const [id, visible] = item.split(':')
    return [id, visible === 'true']
  }))
}
