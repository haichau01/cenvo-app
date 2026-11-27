import type { UserIdentity } from '@/lib/api/types'

export const ADMIN_ROLE = 'Admin'

/**
 * Staff with several branches that the backend did not auto-select must pick one before working.
 * Admins are not forced: no selection means viewing the whole chain.
 */
export function needsBranchChoice(user: UserIdentity): boolean {
  return user.branchId == null && user.branches.length > 1 && !canWorkAcrossBranches(user)
}

/** Only admins may work in whole-chain mode (no branch). */
export function canWorkAcrossBranches(user: UserIdentity): boolean {
  return user.roles.includes(ADMIN_ROLE)
}

/** Staff without any assigned branch cannot work; an administrator must assign one. */
export function hasNoBranch(user: UserIdentity): boolean {
  return user.branches.length === 0 && !canWorkAcrossBranches(user)
}

export function currentBranchName(user: UserIdentity): string {
  if (user.branchId == null) return 'Toàn chuỗi'
  return user.branches.find((b) => b.id === user.branchId)?.name ?? ''
}
