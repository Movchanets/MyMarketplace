import { useState, useRef, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useSearchStore } from '../../store/searchStore'
import { SearchDropdown } from './SearchDropdown'

export function SearchBar() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [isDropdownOpen, setIsDropdownOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  const { 
    query, 
    setQuery, 
    search, 
    fetchPopularQueries,
    isSearching 
  } = useSearchStore()

  // Fetch popular queries on mount
  useEffect(() => {
    fetchPopularQueries()
  }, [fetchPopularQueries])

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsDropdownOpen(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const handleSearch = useCallback(async () => {
    if (!query.trim()) return
    
    await search(query)
    setIsDropdownOpen(false)
    // Navigate to search results page
    navigate(`/search?q=${encodeURIComponent(query.trim())}`)
  }, [query, search, navigate])

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch()
    } else if (e.key === 'Escape') {
      setIsDropdownOpen(false)
    }
  }

  const handleFocus = () => {
    setIsDropdownOpen(true)
  }

  const handleSelectQuery = (selectedQuery: string) => {
    setQuery(selectedQuery)
    // Auto-search when selecting from dropdown
    search(selectedQuery).then(() => {
      navigate(`/search?q=${encodeURIComponent(selectedQuery)}`)
    })
  }

  return (
    <div ref={containerRef} className="relative flex-1 max-w-xl">
      <div className="flex items-stretch">
        <div className="relative flex-1">
          <input
            ref={inputRef}
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onFocus={handleFocus}
            onKeyDown={handleKeyDown}
            placeholder={t('search.placeholder')}
            className="h-10 w-full rounded-l-lg border border-r-0 border-text/20 bg-surface px-4 pr-10 text-sm text-text placeholder:text-text-muted focus:border-brand focus:outline-none"
          />
          {/* Search icon inside input */}
          <span className="absolute right-3 top-1/2 -translate-y-1/2 text-text-muted pointer-events-none">
            &#128269;
          </span>
        </div>
        <button
          type="button"
          onClick={handleSearch}
          disabled={isSearching || !query.trim()}
          className="h-10 rounded-r-lg bg-brand px-4 text-sm font-medium text-white transition-colors hover:bg-brand/90 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isSearching ? (
            <span className="inline-block h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
          ) : (
            t('search.button')
          )}
        </button>
      </div>

      {/* Dropdown */}
      {isDropdownOpen && (
        <SearchDropdown
          onSelect={handleSelectQuery}
          onClose={() => setIsDropdownOpen(false)}
        />
      )}
    </div>
  )
}
