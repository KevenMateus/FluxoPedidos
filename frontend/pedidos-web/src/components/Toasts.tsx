import { useToastStore } from '../store/toastStore'

export function Toasts() {
  const { toasts, dismiss } = useToastStore()

  return (
    <div className="toasts">
      {toasts.map((t) => (
        <div key={t.id} className={`toast toast-${t.kind}`} onClick={() => dismiss(t.id)}>
          {t.message}
        </div>
      ))}
    </div>
  )
}
