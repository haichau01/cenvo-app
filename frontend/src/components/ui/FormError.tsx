/** Form-level error (from the server). Icon + text — never color alone. */
export function FormError({ message }: { message?: string | null }) {
  if (!message) return null
  return (
    <div role="alert" className="flex gap-2.5 rounded-(--radius-control) border border-stop/30 bg-stop/8 px-3 py-2.5 text-sm text-ink">
      <svg aria-hidden viewBox="0 0 20 20" className="mt-0.5 size-4 shrink-0 fill-stop">
        <path d="M10 1.5a8.5 8.5 0 1 0 0 17 8.5 8.5 0 0 0 0-17Zm-.9 4.5h1.8v5.5H9.1V6Zm0 7h1.8v1.8H9.1V13Z" />
      </svg>
      <span>{message}</span>
    </div>
  )
}
