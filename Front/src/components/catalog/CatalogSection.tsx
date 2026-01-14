import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { categoriesApi, type CategoryDto } from '../../api/catalogApi'

export function CatalogSection() {
  const { t } = useTranslation()
  const [categories, setCategories] = useState<CategoryDto[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const fetchCategories = async () => {
      try {
        const response = await categoriesApi.getAll(undefined, true)
        if (response.isSuccess && response.payload) {
          setCategories(response.payload)
        }
      } catch (error) {
        console.error('Failed to load categories:', error)
      } finally {
        setIsLoading(false)
      }
    }
    fetchCategories()
  }, [])

  if (isLoading) {
    return (
      <section className="py-6">
        <h2 className="mb-4 text-xl font-semibold text-text">{t('home.popular_categories')}</h2>
        <div className="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
          {[...Array(6)].map((_, i) => (
            <div key={i} className="card h-28 animate-pulse bg-surface" />
          ))}
        </div>
      </section>
    )
  }

  if (categories.length === 0) {
    return null
  }

  return (
    <section className="py-6">
      <h2 className="mb-4 text-xl font-semibold text-text">{t('home.popular_categories')}</h2>
      <div className="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
        {categories.map((category) => (
          <Link
            key={category.id}
            to={`/category/${category.slug}`}
            className="group card flex flex-col items-center justify-center gap-2 p-4 text-center transition-all hover:border-brand hover:shadow-lg"
          >
            {category.emoji ? (
              <span className="text-4xl transition-transform group-hover:scale-110">
                {category.emoji}
              </span>
            ) : (
              <span className="flex h-12 w-12 items-center justify-center rounded-full bg-brand/10 text-brand">
                <svg
                  className="h-6 w-6"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  strokeWidth={2}
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"
                  />
                </svg>
              </span>
            )}
            <span className="text-sm font-medium text-text group-hover:text-brand">
              {category.name}
            </span>
          </Link>
        ))}
      </div>
    </section>
  )
}
