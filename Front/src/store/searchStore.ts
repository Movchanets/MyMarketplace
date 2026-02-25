import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'

const SEARCH_HISTORY_KEY = 'search_history'
const MAX_HISTORY_ITEMS = 10

interface SearchState {
  query: string
  history: string[]
  setQuery: (query: string) => void
  addToHistory: (query: string) => void
  removeFromHistory: (query: string) => void
  clearHistory: () => void
}

export const useSearchStore = create<SearchState>()(
  persist(
    (set) => ({
      query: '',
      history: [],

      setQuery: (query) => set({ query }),

      addToHistory: (query) => {
        const trimmedQuery = query.trim()
        if (!trimmedQuery) return

        set((state) => {
          const filtered = state.history.filter(
            (item) => item.toLowerCase() !== trimmedQuery.toLowerCase(),
          )
          return {
            history: [trimmedQuery, ...filtered].slice(0, MAX_HISTORY_ITEMS),
          }
        })
      },

      removeFromHistory: (query) => {
        set((state) => ({
          history: state.history.filter((item) => item.toLowerCase() !== query.toLowerCase()),
        }))
      },

      clearHistory: () => set({ history: [] }),
    }),
    {
      name: SEARCH_HISTORY_KEY,
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({ history: state.history }),
    },
  ),
)
