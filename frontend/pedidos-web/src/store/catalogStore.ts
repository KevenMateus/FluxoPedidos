import { create } from 'zustand'
import { catalogApi } from '../api/client'
import type { CustomerLookup, ProductLookup } from '../types'

interface CatalogState {
  customers: CustomerLookup[]
  products: ProductLookup[]
  loaded: boolean
  load: () => Promise<void>
}

export const useCatalogStore = create<CatalogState>((set, get) => ({
  customers: [],
  products: [],
  loaded: false,

  load: async () => {
    if (get().loaded) return
    const [customers, products] = await Promise.all([catalogApi.customers(), catalogApi.products()])
    set({ customers, products, loaded: true })
  },
}))
