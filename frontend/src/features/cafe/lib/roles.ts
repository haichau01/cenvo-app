import type { UserIdentity } from '@/lib/api/types'

/** Mirrors the backend `Cashiers` policy — UI only, the backend still enforces it. */
const CHECKOUT_ROLES = ['Admin', 'BranchManager', 'Cashier']

export function canCheckout(user: UserIdentity): boolean {
  return user.roles.some((role) => CHECKOUT_ROLES.includes(role))
}
