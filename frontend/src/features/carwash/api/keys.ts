/** Query keys of the car wash module: [module, resource, params]. */
export const washKeys = {
  all: ['carwash'] as const,
  queue: ['carwash', 'tickets', 'queue'] as const,
  tickets: ['carwash', 'tickets'] as const,
  ticket: (id: string) => ['carwash', 'tickets', id] as const,
  history: (from: string, to: string, q: string) => ['carwash', 'tickets', 'history', from, to, q] as const,
  photo: (ticketId: string, photoId: string) => ['carwash', 'photos', ticketId, photoId] as const,
  offerings: (all: boolean) => ['carwash', 'services', all] as const,
  packages: (all: boolean) => ['carwash', 'packages', all] as const,
  washers: ['carwash', 'staff'] as const,
  lookup: (plate: string) => ['carwash', 'lookup', plate] as const,
  customers: ['carwash', 'customers'] as const,
  customerSearch: (q: string) => ['carwash', 'customers', 'search', q] as const,
  customer: (id: string) => ['carwash', 'customers', id] as const,
  commissions: (from: string, to: string) => ['carwash', 'commissions', from, to] as const,
}
