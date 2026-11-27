/** Query keys of the inventory module: [module, resource, params]. */
export const inventoryKeys = {
  all: ['inventory'] as const,
  warehouses: (allBranches: boolean) => ['inventory', 'warehouses', { allBranches }] as const,
  suppliers: ['inventory', 'suppliers'] as const,
  products: ['inventory', 'products'] as const,
  balances: (filter: object) => ['inventory', 'balances', filter] as const,
  stockCard: (params: object) => ['inventory', 'stock-card', params] as const,
  documents: ['inventory', 'documents'] as const,
  documentList: (filter: object) => ['inventory', 'documents', 'list', filter] as const,
  incoming: ['inventory', 'documents', 'incoming'] as const,
  document: (id: string) => ['inventory', 'documents', id] as const,
}
