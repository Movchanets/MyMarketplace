import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useCategories } from '../../hooks/queries/useCategories'

interface CatalogMenuProps {
  onClose: () => void
}

export function CatalogMenu({ onClose }: CatalogMenuProps) {
  const { t } = useTranslation()
  const [hoveredCategoryId, setHoveredCategoryId] = useState<string | null>(null)
  const { data: topCategories = [], isPending: isLoading } = useCategories({ topLevel: true })
  const { data: subCategories = [], isPending: isLoadingSub } = useCategories({
    parentId: hoveredCategoryId ?? undefined,
  })

  useEffect(() => {
    if (!hoveredCategoryId && topCategories.length > 0) {
      setHoveredCategoryId(topCategories[0].id)
    }
  }, [hoveredCategoryId, topCategories])

  const hoveredCategory = topCategories.find((c) => c.id === hoveredCategoryId)

  if (isLoading) {
    return (
      <div className="absolute left-0 top-full z-50 mt-2 w-[700px] rounded-lg border border-foreground/10 bg-surface p-6 shadow-xl">
        <div className="flex items-center justify-center">
          <span className="h-6 w-6 animate-spin rounded-full border-2 border-brand border-t-transparent" />
        </div>
      </div>
    )
  }

  if (topCategories.length === 0) {
    return (
      <div className="absolute left-0 top-full z-50 mt-2 rounded-lg border border-foreground/10 bg-surface p-6 shadow-xl">
        <p className="text-sm text-foreground-muted">{t('catalog.no_categories')}</p>
      </div>
    )
  }

  return (
    <div className="absolute left-0 top-full z-50 mt-2 flex w-[700px] rounded-lg border border-foreground/10 bg-surface shadow-xl">
      {/* Left column: Top-level categories */}
      <div className="w-64 border-r border-foreground/10 py-2">
        <ul>
          {topCategories.map((category) => (
            <li key={category.id}>
              <Link
                to={`/category/${category.slug}`}
                onClick={onClose}
                onMouseEnter={() => setHoveredCategoryId(category.id)}
                className={`flex items-center gap-3 px-4 py-2.5 text-sm transition-colors ${
                  hoveredCategoryId === category.id
                    ? 'bg-brand/10 text-brand'
                    : 'text-foreground hover:bg-surface'
                }`}
              >
                {category.emoji && (
                  <span className="text-lg">{category.emoji}</span>
                )}
                <span className="flex-1">{category.name}</span>
                {/* Chevron right */}
                <svg
                  className={`h-4 w-4 transition-opacity ${
                    hoveredCategoryId === category.id ? 'opacity-100' : 'opacity-0'
                  }`}
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  strokeWidth={2}
                >
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
                </svg>
              </Link>
            </li>
          ))}
        </ul>
      </div>

      {/* Right column: Subcategories */}
      <div className="flex-1 p-4">
        {hoveredCategory && (
          <>
            {/* Category header */}
            <div className="mb-4 border-b border-foreground/10 pb-3">
              <Link
                to={`/category/${hoveredCategory.slug}`}
                onClick={onClose}
                className="flex items-center gap-2 text-lg font-semibold text-foreground hover:text-brand"
              >
                {hoveredCategory.emoji && (
                  <span>{hoveredCategory.emoji}</span>
                )}
                {hoveredCategory.name}
              </Link>
              {hoveredCategory.description && (
                <p className="mt-1 text-xs text-foreground-muted">{hoveredCategory.description}</p>
              )}
            </div>

            {/* Subcategories grid */}
            {isLoadingSub ? (
              <div className="flex items-center justify-center py-8">
                <span className="h-5 w-5 animate-spin rounded-full border-2 border-brand border-t-transparent" />
              </div>
            ) : subCategories.length > 0 ? (
              <div className="grid grid-cols-2 gap-x-4 gap-y-1">
                {subCategories.map((sub) => (
                  <Link
                    key={sub.id}
                    to={`/category/${sub.slug}`}
                    onClick={onClose}
                    className="rounded px-2 py-1.5 text-sm text-foreground-muted transition-colors hover:bg-surface hover:text-foreground"
                  >
                    {sub.emoji && <span className="mr-2">{sub.emoji}</span>}
                    {sub.name}
                  </Link>
                ))}
              </div>
            ) : (
              <p className="text-sm text-foreground-muted">{t('catalog.no_subcategories')}</p>
            )}

            {/* View all link */}
            <div className="mt-4 border-t border-foreground/10 pt-3">
              <Link
                to={`/category/${hoveredCategory.slug}`}
                onClick={onClose}
                className="text-sm font-medium text-brand hover:underline"
              >
                {t('catalog.view_all')} &rarr;
              </Link>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
