import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { categoriesApi, type CategoryDto } from '../../api/catalogApi'

interface CatalogMenuProps {
  onClose: () => void
}

export function CatalogMenu({ onClose }: CatalogMenuProps) {
  const { t } = useTranslation()
  const [topCategories, setTopCategories] = useState<CategoryDto[]>([])
  const [subCategories, setSubCategories] = useState<CategoryDto[]>([])
  const [hoveredCategoryId, setHoveredCategoryId] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingSub, setIsLoadingSub] = useState(false)

  // Fetch top-level categories
  useEffect(() => {
    const fetchCategories = async () => {
      try {
        const response = await categoriesApi.getAll(undefined, true)
        if (response.isSuccess && response.payload) {
          setTopCategories(response.payload)
          // Auto-select first category
          if (response.payload.length > 0) {
            setHoveredCategoryId(response.payload[0].id)
          }
        }
      } catch (error) {
        console.error('Failed to load categories:', error)
      } finally {
        setIsLoading(false)
      }
    }
    fetchCategories()
  }, [])

  // Fetch subcategories when hovering
  useEffect(() => {
    if (!hoveredCategoryId) {
      setSubCategories([])
      return
    }

    const fetchSubCategories = async () => {
      setIsLoadingSub(true)
      try {
        const response = await categoriesApi.getAll(hoveredCategoryId)
        if (response.isSuccess && response.payload) {
          setSubCategories(response.payload)
        }
      } catch (error) {
        console.error('Failed to load subcategories:', error)
        setSubCategories([])
      } finally {
        setIsLoadingSub(false)
      }
    }

    fetchSubCategories()
  }, [hoveredCategoryId])

  const hoveredCategory = topCategories.find((c) => c.id === hoveredCategoryId)

  if (isLoading) {
    return (
      <div className="absolute left-0 top-full z-50 mt-2 w-[700px] rounded-lg border border-text/10 bg-surface-card p-6 shadow-xl">
        <div className="flex items-center justify-center">
          <span className="h-6 w-6 animate-spin rounded-full border-2 border-brand border-t-transparent" />
        </div>
      </div>
    )
  }

  if (topCategories.length === 0) {
    return (
      <div className="absolute left-0 top-full z-50 mt-2 rounded-lg border border-text/10 bg-surface-card p-6 shadow-xl">
        <p className="text-sm text-text-muted">{t('catalog.no_categories')}</p>
      </div>
    )
  }

  return (
    <div className="absolute left-0 top-full z-50 mt-2 flex w-[700px] rounded-lg border border-text/10 bg-surface-card shadow-xl">
      {/* Left column: Top-level categories */}
      <div className="w-64 border-r border-text/10 py-2">
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
                    : 'text-text hover:bg-surface'
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
            <div className="mb-4 border-b border-text/10 pb-3">
              <Link
                to={`/category/${hoveredCategory.slug}`}
                onClick={onClose}
                className="flex items-center gap-2 text-lg font-semibold text-text hover:text-brand"
              >
                {hoveredCategory.emoji && (
                  <span>{hoveredCategory.emoji}</span>
                )}
                {hoveredCategory.name}
              </Link>
              {hoveredCategory.description && (
                <p className="mt-1 text-xs text-text-muted">{hoveredCategory.description}</p>
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
                    className="rounded px-2 py-1.5 text-sm text-text-muted transition-colors hover:bg-surface hover:text-text"
                  >
                    {sub.emoji && <span className="mr-2">{sub.emoji}</span>}
                    {sub.name}
                  </Link>
                ))}
              </div>
            ) : (
              <p className="text-sm text-text-muted">{t('catalog.no_subcategories')}</p>
            )}

            {/* View all link */}
            <div className="mt-4 border-t border-text/10 pt-3">
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
