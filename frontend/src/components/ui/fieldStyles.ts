/** Shared look of native inputs/selects (h-12 = 48px touch target, Cenvo focus ring). */
export const controlClass = `h-12 rounded-(--radius-control) border border-line bg-surface px-3 text-base text-ink outline-none
  focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-foam disabled:bg-concrete disabled:text-ink-muted`

/** Denser variant for table cells on desktop screens. */
export const cellControlClass = `h-10 w-full rounded-(--radius-control) border border-line bg-surface px-2 text-table text-ink outline-none
  focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-foam`
