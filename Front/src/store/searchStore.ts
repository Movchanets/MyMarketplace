import { create } from 'zustand'
import { devtools, persist } from 'zustand/middleware'
import { searchApi, type PopularQueryDto } from '../api/searchApi'
import type { ProductSummaryDto } from '../api/catalogApi'

const SEARCH_HISTORY_KEY = 'search_history'
const MAX_HISTORY_ITEMS = 10

interface SearchState {
  // Search query input
  query: string
  setQuery: (query: string) => void
  
  // Search results
  results: ProductSummaryDto[]
  isSearching: boolean
  searchError: string | null
  
  // Search history (persisted in localStorage)
  history: string[]
  addToHistory: (query: string) => void
  removeFromHistory: (query: string) => void
  clearHistory: () => void
  
  // Popular queries from API
  popularQueries: PopularQueryDto[]
  isLoadingPopular: boolean
  
  // Actions
  search: (query: string) => Promise<void>
  fetchPopularQueries: () => Promise<void>
  clearResults: () => void
}

export const useSearchStore = create<SearchState>()(
  devtools(
    persist(
      (set, get) => ({
        // State
        query: '',
        results: [],
        isSearching: false,
        searchError: null,
        history: [],
        popularQueries: [],
        isLoadingPopular: false,

        // Set query
        setQuery: (query) => set({ query }),

        // Search products
        search: async (query) => {
          const trimmedQuery = query.trim()
          if (!trimmedQuery) {
            set({ results: [], searchError: null })
            return
          }

          set({ isSearching: true, searchError: null })
          
          try {
            const response = await searchApi.search(trimmedQuery)
            if (response.isSuccess && response.payload) {
              set({ results: response.payload, isSearching: false })
              // Add to history on successful search
              get().addToHistory(trimmedQuery)
            } else {
              set({ 
                results: [], 
                isSearching: false, 
                searchError: response.message || 'Search failed' 
              })
            }
          } catch (error) {
            set({ 
              results: [], 
              isSearching: false, 
              searchError: error instanceof Error ? error.message : 'Search failed' 
            })
          }
        },

        // Fetch popular queries
        fetchPopularQueries: async () => {
          set({ isLoadingPopular: true })
          try {
            const response = await searchApi.getPopular(10)
            if (response.isSuccess && response.payload) {
              set({ popularQueries: response.payload, isLoadingPopular: false })
            } else {
              set({ isLoadingPopular: false })
            }
          } catch {
            set({ isLoadingPopular: false })
          }
        },

        // Clear results
        clearResults: () => set({ results: [], searchError: null }),

        // Add to history (unique, max 10 items, newest first)
        addToHistory: (query) => {
          const trimmedQuery = query.trim()
          if (!trimmedQuery) return
          
          set((state) => {
            // Remove if already exists (to move to top)
            const filtered = state.history.filter(
              (h) => h.toLowerCase() !== trimmedQuery.toLowerCase()
            )
            // Add to beginning, limit to max items
            const newHistory = [trimmedQuery, ...filtered].slice(0, MAX_HISTORY_ITEMS)
            return { history: newHistory }
          })
        },

        // Remove from history
        removeFromHistory: (query) => {
          set((state) => ({
            history: state.history.filter(
              (h) => h.toLowerCase() !== query.toLowerCase()
            )
          }))
        },

        // Clear all history
        clearHistory: () => set({ history: [] })
      }),
      {
        name: SEARCH_HISTORY_KEY,
        partialize: (state) => ({ history: state.history }) // Only persist history
      }
    ),
    { name: 'SearchStore' }
  )
)
