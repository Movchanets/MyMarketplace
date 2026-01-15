import { useTranslation } from 'react-i18next'
import { useSearchStore } from '../../store/searchStore'
import type { PopularQueryDto } from '../../api/searchApi'

interface SearchDropdownProps {
  onSelect: (query: string) => void
  onClose: () => void
}

export function SearchDropdown({ onSelect, onClose }: SearchDropdownProps) {
  const { t } = useTranslation()
  const { history, popularQueries, removeFromHistory, clearHistory } = useSearchStore()

  const handleSelect = (query: string) => {
    onSelect(query)
    onClose()
  }

  const handleRemoveHistory = (e: React.MouseEvent, query: string) => {
    e.stopPropagation()
    removeFromHistory(query)
  }

  const hasContent = history.length > 0 || popularQueries.length > 0

  if (!hasContent) {
    return (
      <div className="absolute left-0 right-0 top-full z-50 mt-1 rounded-lg border border-text/10 bg-surface-card p-4 shadow-lg">
        <p className="text-center text-sm text-text-muted">{t('search.no_suggestions')}</p>
      </div>
    )
  }

  return (
    <div className="absolute left-0 right-0 top-full z-50 mt-1 rounded-lg border border-text/10 bg-surface-card shadow-lg">
      {/* Popular queries */}
      {popularQueries.length > 0 && (
        <div className="border-b border-text/10 p-3">
          <p className="mb-2 text-xs font-medium uppercase text-text-muted">
            {t('search.popular')}
          </p>
          <div className="flex flex-wrap gap-2">
            {popularQueries.map((item: PopularQueryDto) => (
              <button
                key={item.query}
                type="button"
                onClick={() => handleSelect(item.query)}
                className="rounded-full bg-surface px-3 py-1 text-sm text-text transition-colors hover:bg-brand hover:text-white"
              >
                {item.query}
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Search history */}
      {history.length > 0 && (
        <div className="p-3">
          <div className="mb-2 flex items-center justify-between">
            <p className="text-xs font-medium uppercase text-text-muted">
              {t('search.history')}
            </p>
            <button
              type="button"
              onClick={clearHistory}
              className="text-xs text-text-muted hover:text-text"
            >
              {t('search.clear_history')}
            </button>
          </div>
          <ul className="space-y-1">
            {history.map((query) => (
              <li key={query}>
                <button
                  type="button"
                  onClick={() => handleSelect(query)}
                  className="group flex w-full items-center justify-between rounded-md px-2 py-1.5 text-left text-sm text-text hover:bg-surface"
                >
                  <span className="flex items-center gap-2">
                    <span className="text-text-muted">&#128337;</span>
                    {query}
                  </span>
                  <span
                    role="button"
                    tabIndex={0}
                    onClick={(e) => handleRemoveHistory(e, query)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' || e.key === ' ') {
                        handleRemoveHistory(e as unknown as React.MouseEvent, query)
                      }
                    }}
                    className="opacity-0 transition-opacity group-hover:opacity-100 hover:text-red-500"
                    aria-label={t('search.remove_from_history')}
                  >
                    &times;
                  </span>
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
