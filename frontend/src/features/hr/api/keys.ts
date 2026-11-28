/** Query keys of the HR module: [module, resource, params]. */
export const hrKeys = {
  all: ['hr'] as const,
  employees: ['hr', 'employees'] as const,
  employee: (id: string) => ['hr', 'employees', id] as const,
  adjustments: ['hr', 'adjustments'] as const,
  adjustmentList: (from: string, to: string) => ['hr', 'adjustments', from, to] as const,
  staff: ['hr', 'adjustments', 'staff'] as const,
  branches: ['branches'] as const,
}
