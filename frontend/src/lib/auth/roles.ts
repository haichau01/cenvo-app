import type { UserIdentity } from '@/lib/api/types'

/** Mirrors backend `Roles` + `Policies`. UI only (hide what would be refused) — the backend enforces every rule. */
export const Role = {
  Admin: 'Admin',
  BranchManager: 'BranchManager',
  Cashier: 'Cashier',
  Barista: 'Barista',
  Storekeeper: 'Storekeeper',
  Washer: 'Washer',
} as const

function hasAnyRole(user: UserIdentity, roles: string[]): boolean {
  return user.roles.some((role) => roles.includes(role))
}

/** Policies.Managers — cancel posted documents, change settings. */
export const isManager = (user: UserIdentity) => hasAnyRole(user, [Role.Admin, Role.BranchManager])

/** Policies.Cashiers — take payments, open/close shifts. */
export const canCheckout = (user: UserIdentity) => hasAnyRole(user, [Role.Admin, Role.BranchManager, Role.Cashier])

/** Policies.Storekeepers — inventory screens. */
export const canManageStock = (user: UserIdentity) => hasAnyRole(user, [Role.Admin, Role.BranchManager, Role.Storekeeper])

/** Policies.WashStaff — car wash intake, queue, photos. */
export const canWash = (user: UserIdentity) => hasAnyRole(user, [Role.Admin, Role.BranchManager, Role.Cashier, Role.Washer])
