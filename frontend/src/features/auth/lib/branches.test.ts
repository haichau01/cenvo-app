import { describe, expect, it } from 'vitest'
import type { UserIdentity } from '@/lib/api/types'
import { currentBranchName, hasNoBranch, needsBranchChoice } from './branches'

const q1 = { id: 'b1', code: 'CN01', name: 'Cenvo Quận 1' }
const td = { id: 'b2', code: 'CN02', name: 'Cenvo Thủ Đức' }

function user(overrides: Partial<UserIdentity>): UserIdentity {
  return { userId: 'u', userName: 'u', fullName: 'U', roles: ['Cashier'], branchId: null, branches: [], ...overrides }
}

describe('branches', () => {
  it('asks to choose when several branches and none selected', () => {
    expect(needsBranchChoice(user({ branches: [q1, td] }))).toBe(true)
    expect(needsBranchChoice(user({ branches: [q1, td], branchId: 'b2' }))).toBe(false)
    expect(needsBranchChoice(user({ branches: [q1], branchId: 'b1' }))).toBe(false)
    expect(needsBranchChoice(user({ roles: ['Admin'], branches: [q1, td] }))).toBe(false)
  })

  it('flags staff without any branch, but not admin', () => {
    expect(hasNoBranch(user({}))).toBe(true)
    expect(hasNoBranch(user({ roles: ['Admin'] }))).toBe(false)
  })

  it('names the current branch, or whole chain when none', () => {
    expect(currentBranchName(user({ branches: [q1, td], branchId: 'b2' }))).toBe('Cenvo Thủ Đức')
    expect(currentBranchName(user({ roles: ['Admin'], branches: [q1, td] }))).toBe('Toàn chuỗi')
  })
})
