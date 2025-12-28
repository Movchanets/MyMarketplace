import { useState, useEffect, useCallback } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  categoriesApi,
  tagsApi,
  productsApi,
  type CategoryDto,
  type TagDto,
  type UpdateProductRequest,
  type ProductDetailsDto
} from '../../api/catalogApi'

interface FormErrors {
  name?: string
}

export default function ProductEdit() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { productId } = useParams<{ productId: string }>()

  // Data loading state
  const [categories, setCategories] = useState<CategoryDto[]>([])
  const [tags, setTags] = useState<TagDto[]>([])
  const [product, setProduct] = useState<ProductDetailsDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [formErrors, setFormErrors] = useState<FormErrors>({})

  // Form state
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [selectedCategories, setSelectedCategories] = useState<string[]>([])
  const [selectedTags, setSelectedTags] = useState<string[]>([])

  // Dropdowns state
  const [showCategoryDropdown, setShowCategoryDropdown] = useState(false)
  const [showTagDropdown, setShowTagDropdown] = useState(false)
  const [categorySearch, setCategorySearch] = useState('')
  const [tagSearch, setTagSearch] = useState('')

  const fetchData = useCallback(async () => {
    if (!productId) return
    
    setLoading(true)
    setError(null)
    try {
      const [categoriesRes, tagsRes, productRes] = await Promise.all([
        categoriesApi.getAll(),
        tagsApi.getAll(),
        productsApi.getById(productId)
      ])

      if (categoriesRes.isSuccess) {
        setCategories(categoriesRes.payload || [])
      }
      if (tagsRes.isSuccess) {
        setTags(tagsRes.payload || [])
      }
      if (productRes.isSuccess && productRes.payload) {
        const p = productRes.payload
        setProduct(p)
        setName(p.name)
        setDescription(p.description || '')
        setSelectedCategories(p.categories.map(c => c.id))
        setSelectedTags(p.tags.map(t => t.id))
      } else {
        setError(t('product.notFound'))
      }
    } catch (err) {
      setError(t('common.error'))
    } finally {
      setLoading(false)
    }
  }, [productId, t])

  useEffect(() => {
    fetchData()
  }, [fetchData])

  const validateForm = (): boolean => {
    const errors: FormErrors = {}
    
    if (!name.trim()) {
      errors.name = t('product.name_required')
    }
    
    setFormErrors(errors)
    return Object.keys(errors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!validateForm() || !productId) return
    
    setSubmitting(true)
    setError(null)

    try {
      const request: UpdateProductRequest = {
        name: name.trim(),
        description: description.trim() || null,
        categoryIds: selectedCategories,
        tagIds: selectedTags.length > 0 ? selectedTags : undefined
      }

      const result = await productsApi.update(productId, request)
      
      if (result.isSuccess) {
        navigate('/cabinet/products')
      } else {
        setError(result.message || t('common.error'))
      }
    } catch (err) {
      setError(t('common.error'))
    } finally {
      setSubmitting(false)
    }
  }

  const toggleCategory = (id: string) => {
    setSelectedCategories(prev =>
      prev.includes(id) ? prev.filter(c => c !== id) : [...prev, id]
    )
  }

  const toggleTag = (id: string) => {
    setSelectedTags(prev =>
      prev.includes(id) ? prev.filter(t => t !== id) : [...prev, id]
    )
  }

  const filteredCategories = categories.filter(c =>
    c.name.toLowerCase().includes(categorySearch.toLowerCase())
  )

  const filteredTags = tags.filter(t =>
    t.name.toLowerCase().includes(tagSearch.toLowerCase())
  )

  if (loading) {
    return (
      <div className="max-w-2xl mx-auto py-8 px-4">
        <div className="animate-pulse space-y-4">
          <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded w-1/3"></div>
          <div className="h-12 bg-gray-200 dark:bg-gray-700 rounded"></div>
          <div className="h-32 bg-gray-200 dark:bg-gray-700 rounded"></div>
        </div>
      </div>
    )
  }

  if (error && !product) {
    return (
      <div className="max-w-2xl mx-auto py-8 px-4">
        <div className="text-center">
          <p className="text-red-500 mb-4">{error}</p>
          <button
            onClick={() => navigate('/cabinet/products')}
            className="btn btn-secondary"
          >
            {t('common.back')}
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="max-w-2xl mx-auto py-8 px-4">
      <h1 className="text-2xl font-bold text-text mb-6">{t('product.edit_title')}</h1>

      {error && (
        <div className="mb-4 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-600 dark:text-red-400">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Name */}
        <div>
          <label className="block text-sm font-medium text-text mb-2">
            {t('product.name')} *
          </label>
          <input
            type="text"
            value={name}
            onChange={e => setName(e.target.value)}
            className={`w-full px-4 py-2 border rounded-lg bg-background text-text focus:outline-none focus:ring-2 focus:ring-brand ${
              formErrors.name ? 'border-red-500' : 'border-border'
            }`}
            placeholder={t('product.name_placeholder')}
          />
          {formErrors.name && (
            <p className="mt-1 text-sm text-red-500">{formErrors.name}</p>
          )}
        </div>

        {/* Description */}
        <div>
          <label className="block text-sm font-medium text-text mb-2">
            {t('product.description')}
          </label>
          <textarea
            value={description}
            onChange={e => setDescription(e.target.value)}
            rows={4}
            className="w-full px-4 py-2 border border-border rounded-lg bg-background text-text focus:outline-none focus:ring-2 focus:ring-brand resize-none"
            placeholder={t('product.description_placeholder')}
          />
        </div>

        {/* Categories */}
        <div className="relative">
          <label className="block text-sm font-medium text-text mb-2">
            {t('product.categories')}
          </label>
          
          {/* Selected categories chips */}
          <div className="flex flex-wrap gap-2 mb-2">
            {selectedCategories.map(id => {
              const cat = categories.find(c => c.id === id)
              return cat ? (
                <span
                  key={id}
                  className="inline-flex items-center gap-1 px-3 py-1 rounded-full bg-brand/10 text-brand text-sm"
                >
                  {cat.name}
                  <button
                    type="button"
                    onClick={() => toggleCategory(id)}
                    className="hover:text-brand-hover"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </span>
              ) : null
            })}
          </div>

          {/* Dropdown trigger */}
          <button
            type="button"
            onClick={() => setShowCategoryDropdown(!showCategoryDropdown)}
            className="w-full px-4 py-2 border border-border rounded-lg bg-background text-left text-text-muted hover:border-brand transition-colors"
          >
            {t('product.select_categories')}
          </button>

          {/* Dropdown */}
          {showCategoryDropdown && (
            <div className="absolute z-50 w-full mt-1 bg-background border border-border rounded-lg shadow-lg max-h-60 overflow-auto">
              <div className="p-2 border-b border-border">
                <input
                  type="text"
                  value={categorySearch}
                  onChange={e => setCategorySearch(e.target.value)}
                  placeholder={t('common.search')}
                  className="w-full px-3 py-2 border border-border rounded bg-background text-text text-sm"
                />
              </div>
              <div className="p-2">
                {filteredCategories.length === 0 ? (
                  <p className="text-text-muted text-sm py-2 px-3">{t('common.noResults')}</p>
                ) : (
                  filteredCategories.map(cat => (
                    <label
                      key={cat.id}
                      className="flex items-center gap-2 px-3 py-2 hover:bg-background-secondary rounded cursor-pointer"
                    >
                      <input
                        type="checkbox"
                        checked={selectedCategories.includes(cat.id)}
                        onChange={() => toggleCategory(cat.id)}
                        className="rounded border-border text-brand focus:ring-brand"
                      />
                      <span className="text-text">{cat.name}</span>
                    </label>
                  ))
                )}
              </div>
            </div>
          )}
        </div>

        {/* Tags */}
        <div className="relative">
          <label className="block text-sm font-medium text-text mb-2">
            {t('product.tags')}
          </label>
          
          {/* Selected tags chips */}
          <div className="flex flex-wrap gap-2 mb-2">
            {selectedTags.map(id => {
              const tag = tags.find(t => t.id === id)
              return tag ? (
                <span
                  key={id}
                  className="inline-flex items-center gap-1 px-3 py-1 rounded-full bg-gray-100 dark:bg-gray-800 text-text text-sm"
                >
                  {tag.name}
                  <button
                    type="button"
                    onClick={() => toggleTag(id)}
                    className="hover:text-red-500"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </span>
              ) : null
            })}
          </div>

          {/* Dropdown trigger */}
          <button
            type="button"
            onClick={() => setShowTagDropdown(!showTagDropdown)}
            className="w-full px-4 py-2 border border-border rounded-lg bg-background text-left text-text-muted hover:border-brand transition-colors"
          >
            {t('product.select_tags')}
          </button>

          {/* Dropdown */}
          {showTagDropdown && (
            <div className="absolute z-50 w-full mt-1 bg-background border border-border rounded-lg shadow-lg max-h-60 overflow-auto">
              <div className="p-2 border-b border-border">
                <input
                  type="text"
                  value={tagSearch}
                  onChange={e => setTagSearch(e.target.value)}
                  placeholder={t('common.search')}
                  className="w-full px-3 py-2 border border-border rounded bg-background text-text text-sm"
                />
              </div>
              <div className="p-2">
                {filteredTags.length === 0 ? (
                  <p className="text-text-muted text-sm py-2 px-3">{t('common.noResults')}</p>
                ) : (
                  filteredTags.map(tag => (
                    <label
                      key={tag.id}
                      className="flex items-center gap-2 px-3 py-2 hover:bg-background-secondary rounded cursor-pointer"
                    >
                      <input
                        type="checkbox"
                        checked={selectedTags.includes(tag.id)}
                        onChange={() => toggleTag(tag.id)}
                        className="rounded border-border text-brand focus:ring-brand"
                      />
                      <span className="text-text">{tag.name}</span>
                    </label>
                  ))
                )}
              </div>
            </div>
          )}
        </div>

        {/* Info about SKUs */}
        <div className="p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
          <p className="text-sm text-blue-700 dark:text-blue-300">
            {t('product.edit_sku_info')}
          </p>
        </div>

        {/* Submit */}
        <div className="flex gap-4">
          <button
            type="submit"
            disabled={submitting}
            className="btn btn-brand"
          >
            {submitting ? t('common.saving') : t('common.save')}
          </button>
          <button
            type="button"
            onClick={() => navigate('/cabinet/products')}
            className="btn btn-secondary"
          >
            {t('common.cancel')}
          </button>
        </div>
      </form>
    </div>
  )
}
