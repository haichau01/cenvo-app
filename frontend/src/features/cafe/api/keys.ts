/** Query keys of the cafe module: [module, resource, params]. */
export const cafeKeys = {
  menu: ['cafe', 'menu'] as const,
  tables: ['cafe', 'tables'] as const,
  orders: ['cafe', 'orders'] as const,
  openOrders: ['cafe', 'orders', 'open'] as const,
  order: (id: string) => ['cafe', 'orders', id] as const,
  currentShift: ['cafe', 'shifts', 'current'] as const,
  kitchen: ['cafe', 'kitchen'] as const,
}
