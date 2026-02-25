import { useTranslation } from 'react-i18next'
import { useSearchStore } from '../../store/searchStore'
import { usePopularQueries } from '../../hooks/queries/useSearch'

interface SearchDropdownProps {
  onSelect: (query: string) => void
  onClose: () => void
}

export function SearchDropdown({ onSelect, onClose }: SearchDropdownProps) {
  const { t } = useTranslation()
  const { history, removeFromHistory, clearHistory } = useSearchStore()
  const { data: popularQueries = [] } = usePopularQueries()

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
      <div className="absolute left-0 right-0 top-full z-50 mt-1 rounded-lg border border-foreground/10 bg-surface p-4 shadow-lg">
        <p className="text-center text-sm text-foreground-muted">{t('search.no_suggestions')}</p>
      </div>
    )
  }

  return (
    <div className="absolute left-0 right-0 top-full z-50 mt-1 rounded-lg border border-foreground/10 bg-surface shadow-lg">
      {/* Popular queries */}
      {popularQueries.length > 0 && (
        <div className="border-b border-foreground/10 p-3">
          <p className="mb-2 text-xs font-medium uppercase text-foreground-muted">
            {t('search.popular')}
          </p>
          <div className="flex flex-wrap gap-2">
            {popularQueries.map((item) => (
              <button
                key={item.query}
                type="button"
                onClick={() => handleSelect(item.query)}
                className="rounded-full bg-surface px-3 py-1 text-sm text-foreground transition-colors hover:bg-brand hover:text-white"
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
            <p className="text-xs font-medium uppercase text-foreground-muted">
              {t('search.history')}
            </p>
            <button
              type="button"
              onClick={clearHistory}
              className="text-xs text-foreground-muted hover:text-foreground"
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
                  className="group flex w-full items-center justify-between rounded-md px-2 py-1.5 text-left text-sm text-foreground hover:bg-surface-hover"
                >
                  <span className="flex items-center gap-2">
                    <span className="text-foreground-muted">&#128337;</span>
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
                    className="opacity-0 transition-opacity group-hover:opacity-100 hover:text-error"
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
