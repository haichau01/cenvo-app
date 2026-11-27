import { Navigate, Outlet, useLocation } from 'react-router'
import { needsBranchChoice } from '@/features/auth/lib/branches'
import { useSession } from '@/lib/auth/session'

const BRANCH_PICKER = '/chon-chi-nhanh'

/** Guards routes until the user is signed in and has a branch. Navigation only — real authorization is enforced by the backend. */
export function RequireAuth() {
  const session = useSession()
  const location = useLocation()
  const from = location.pathname + location.search

  if (!session) return <Navigate to="/dang-nhap" replace state={{ from }} />
  if (needsBranchChoice(session.user)) return <Navigate to={BRANCH_PICKER} replace state={{ from }} />
  return <Outlet />
}
