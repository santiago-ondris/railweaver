import { useEffect, useRef, useState } from 'react'
import type { AlignmentSection } from '../planning/api'
import {
  calculateCorridor,
  calculateRoute,
  fetchPresets,
  type RunningTime,
  type Train,
  type TrainPreset,
} from './api'

type Target =
  | { kind: 'corridor'; key: string; sections: AlignmentSection[] }
  | {
      kind: 'route'
      key: string
      originId: string
      destinationId: string
      includeDisused: boolean
      reversalCount: number
    }

export function useRunningTime(regionId: string) {
  const [presets, setPresets] = useState<TrainPreset[]>([])
  const [target, setTarget] = useState<Target | null>(null)
  const [result, setResult] = useState<{ key: string; value: RunningTime } | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const controllerRef = useRef<AbortController | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    void fetchPresets(controller.signal)
      .then(setPresets)
      .catch((reason: unknown) => {
        if (!controller.signal.aborted)
          setError(reason instanceof Error ? reason.message : 'No se pudieron cargar los trenes.')
      })
    return () => controller.abort()
  }, [])

  const cancel = () => {
    controllerRef.current?.abort()
    controllerRef.current = null
    setBusy(false)
    setTarget(null)
    setError(null)
  }
  const open = (next: Target) => {
    cancel()
    setTarget(next)
  }
  const clear = () => {
    cancel()
    setResult(null)
  }
  const calculate = async (train: Train, lineSpeed: number, dwellMinutes: number) => {
    if (!target) return
    const controller = new AbortController()
    controllerRef.current = controller
    setBusy(true)
    setError(null)
    try {
      const value =
        target.kind === 'corridor'
          ? await calculateCorridor(regionId, target.sections, train, controller.signal)
          : await calculateRoute(
              regionId,
              target.originId,
              target.destinationId,
              target.includeDisused,
              lineSpeed,
              dwellMinutes,
              train,
              controller.signal,
            )
      if (controller.signal.aborted) return
      setResult({ key: target.key, value })
      setTarget(null)
    } catch (reason) {
      if (!controller.signal.aborted)
        setError(reason instanceof Error ? reason.message : 'No se pudo calcular el tiempo.')
    } finally {
      if (controllerRef.current === controller) {
        controllerRef.current = null
        setBusy(false)
      }
    }
  }
  return {
    presets,
    target,
    result,
    busy,
    error,
    open,
    cancel,
    clear,
    calculate,
    remove: () => setResult(null),
  }
}
