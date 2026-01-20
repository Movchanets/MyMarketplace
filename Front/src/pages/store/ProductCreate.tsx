import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  categoriesApi,
  tagsApi,
  productsApi,
  type CategoryDto,
  type TagDto,
  type CreateProductRequest,
  type SkuRequest
} from '../../api/catalogApi'
import {
  attributeDefinitionsApi,
  type AttributeDefinitionDto,
} from '../../api/attributeDefinitionsApi'
import AttributeSelector from '../../components/catalog/AttributeSelector'
import { ErrorAlert } from '../../components/ui/ErrorAlert'

interface AttributeField {
  key: string
  value: string
}

interface SkuFormData {
  id: string
  price: string
  stockQuantity: string
  attributes: AttributeField[]
}

interface GalleryImage {
  id: string
  file: File
  preview: string
}

interface FormErrors {
  name?: string
  description?: string
  skus?: string
}

const generateId = () => Math.random().toString(36).substring(2, 9)

export default function ProductCreate() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  // Data loading state
  const [categories, setCategories] = useState<CategoryDto[]>([])
  const [tags, setTags] = useState<TagDto[]>([])
  const [attributeDefinitions, setAttributeDefinitions] = useState<AttributeDefinitionDto[]>([])
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [formErrors, setFormErrors] = useState<FormErrors>({})

  // Form state
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [selectedCategories, setSelectedCategories] = useState<string[]>([])
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [skus, setSkus] = useState<SkuFormData[]>([
    { id: generateId(), price: '', stockQuantity: '', attributes: [] }
  ])
  const [galleryImages, setGalleryImages] = useState<GalleryImage[]>([])

  // Dropdowns state
  const [showCategoryDropdown, setShowCategoryDropdown] = useState(false)
  const [showTagDropdown, setShowTagDropdown] = useState(false)
  const [categorySearch, setCategorySearch] = useState('')
  const [tagSearch, setTagSearch] = useState('')

  const fetchData = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [categoriesRes, tagsRes, attributesRes] = await Promise.all([
        categoriesApi.getAll(),
        tagsApi.getAll(),
        attributeDefinitionsApi.getAll()
      ])

      if (categoriesRes.isSuccess) {
        setCategories(categoriesRes.payload || [])
      }
      if (tagsRes.isSuccess) {
        setTags(tagsRes.payload || [])
      }
      if (attributesRes.isSuccess) {
        setAttributeDefinitions(attributesRes.payload || [])
      }
    } catch {
      setError(t('errors.fetch_failed'))
    } finally {
      setLoading(false)
    }
  }, [t])

  useEffect(() => {
    fetchData()
  }, [fetchData])

  // Close dropdowns when clicking outside
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      const target = e.target as HTMLElement
      if (!target.closest('.category-dropdown')) {
        setShowCategoryDropdown(false)
      }
      if (!target.closest('.tag-dropdown')) {
        setShowTagDropdown(false)
      }
    }
    document.addEventListener('click', handleClickOutside)
    return () => document.removeEventListener('click', handleClickOutside)
  }, [])

  // Cleanup image previews
  useEffect(() => {
    return () => {
      galleryImages.forEach(img => URL.revokeObjectURL(img.preview))
    }
  }, [galleryImages])

  const validateForm = (): boolean => {
    const errors: FormErrors = {}

    if (!name.trim()) {
      errors.name = t('validation.required')
    } else if (name.trim().length < 2) {
      errors.name = t('validation.min_2')
    } else if (name.length > 200) {
      errors.name = t('validation.max_200')
    }

    if (description && description.length > 2000) {
      errors.description = t('validation.max_2000')
    }

    // Validate SKUs
    for (const sku of skus) {
      if (sku.price || sku.stockQuantity) {
        const priceNum = parseFloat(sku.price)
        const stockNum = parseInt(sku.stockQuantity, 10)
        if (sku.price && (isNaN(priceNum) || priceNum < 0)) {
          errors.skus = t('product.validation.price_positive')
          break
        }
        if (sku.stockQuantity && (isNaN(stockNum) || stockNum < 0)) {
          errors.skus = t('product.validation.stock_positive')
          break
        }
      }

      // Check duplicate attribute keys within a SKU
      const keys = sku.attributes.map(a => a.key.trim()).filter(k => k)
      const uniqueKeys = new Set(keys)
      if (keys.length !== uniqueKeys.size) {
        errors.skus = t('product.validation.duplicate_attributes')
        break
      }
    }

    setFormErrors(errors)
    return Object.keys(errors).length === 0
  }

  // SKU management
  const handleAddSku = () => {
    setSkus([...skus, { id: generateId(), price: '', stockQuantity: '', attributes: [] }])
  }

  const handleRemoveSku = (skuId: string) => {
    if (skus.length <= 1) return
    setSkus(skus.filter(s => s.id !== skuId))
  }

  const handleSkuChange = (skuId: string, field: 'price' | 'stockQuantity', value: string) => {
    setSkus(skus.map(s => s.id === skuId ? { ...s, [field]: value } : s))
  }

  // SKU Attributes - using AttributeSelector
  const handleSkuAttributesChange = (skuId: string, attributes: AttributeField[]) => {
    setSkus(skus.map(s => s.id === skuId ? { ...s, attributes } : s))
  }

  // Category/Tag selection
  const toggleCategory = (categoryId: string) => {
    setSelectedCategories(prev =>
      prev.includes(categoryId)
        ? prev.filter(id => id !== categoryId)
        : [...prev, categoryId]
    )
  }

  const toggleTag = (tagId: string) => {
    setSelectedTags(prev =>
      prev.includes(tagId)
        ? prev.filter(id => id !== tagId)
        : [...prev, tagId]
    )
  }

  // Gallery management
  const handleImageSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files
    if (!files) return

    const newImages: GalleryImage[] = Array.from(files).map(file => ({
      id: generateId(),
      file,
      preview: URL.createObjectURL(file)
    }))

    setGalleryImages([...galleryImages, ...newImages])
    e.target.value = '' // Reset input
  }

  const handleRemoveImage = (imageId: string) => {
    const img = galleryImages.find(i => i.id === imageId)
    if (img) {
      URL.revokeObjectURL(img.preview)
    }
    setGalleryImages(galleryImages.filter(i => i.id !== imageId))
  }

  const buildSkuRequest = (sku: SkuFormData): SkuRequest | null => {
    const price = parseFloat(sku.price)
    const stockQuantity = parseInt(sku.stockQuantity, 10)

    // Skip empty SKUs
    if (!sku.price && !sku.stockQuantity && sku.attributes.length === 0) {
      return null
    }

    // Build attributes object
    const attributesObj: Record<string, unknown> = {}
    for (const attr of sku.attributes) {
      if (attr.key.trim()) {
        const val = attr.value.trim()
        if (val === 'true') {
          attributesObj[attr.key.trim()] = true
        } else if (val === 'false') {
          attributesObj[attr.key.trim()] = false
        } else if (!isNaN(Number(val)) && val !== '') {
          attributesObj[attr.key.trim()] = Number(val)
        } else {
          attributesObj[attr.key.trim()] = val
        }
      }
    }

    return {
      price: isNaN(price) ? 0 : price,
      stockQuantity: isNaN(stockQuantity) ? 0 : stockQuantity,
      attributes: Object.keys(attributesObj).length > 0 ? attributesObj : undefined
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!validateForm()) return

    setSubmitting(true)
    setError(null)

    try {
      // Build SKUs array
      const skuRequests = skus
        .map(buildSkuRequest)
        .filter((s): s is SkuRequest => s !== null)

      const request: CreateProductRequest = {
        name: name.trim(),
        description: description.trim() || null,
        categoryIds: selectedCategories.length > 0 ? selectedCategories : undefined,
        tagIds: selectedTags.length > 0 ? selectedTags : undefined,
        skus: skuRequests.length > 0 ? skuRequests : undefined
      }

      const response = await productsApi.create(request)

      if (!response.isSuccess || !response.payload) {
        setError(response.message || t('errors.save_failed'))
        setSubmitting(false)
        return
      }

      const productId = response.payload

      // Upload gallery images
      if (galleryImages.length > 0) {
        for (let i = 0; i < galleryImages.length; i++) {
          try {
            await productsApi.uploadGalleryImage(productId, galleryImages[i].file, i)
          } catch {
            console.error('Failed to upload image:', galleryImages[i].file.name)
          }
        }
      }

      navigate('/cabinet/my-store')
    } catch {
      setError(t('errors.save_failed'))
    } finally {
      setSubmitting(false)
    }
  }

  const filteredCategories = categories.filter(cat =>
    cat.name.toLowerCase().includes(categorySearch.toLowerCase())
  )

  const filteredTags = tags.filter(tag =>
    tag.name.toLowerCase().includes(tagSearch.toLowerCase())
  )

  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-[300px]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-brand"></div>
      </div>
    )
  }

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <div className="mb-6">
        <button
          onClick={() => navigate('/cabinet/my-store')}
          className="text-brand hover:text-brand-hover flex items-center gap-2"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
          {t('product.back_to_store')}
        </button>
      </div>

      <h1 className="text-2xl font-bold text-foreground mb-6">{t('product.create_title')}</h1>

      {error && (
        <ErrorAlert className="mb-6">{error}</ErrorAlert>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Basic Info */}
        <div className="card p-6 space-y-4">
          <h2 className="text-lg font-semibold text-foreground">{t('product.basic_info')}</h2>

          <div>
            <label className="block text-sm font-medium text-foreground mb-1">
              {t('product.name')} <span className="text-error">*</span>
            </label>
            <input
              type="text"
              value={name}
              onChange={e => setName(e.target.value)}
              className={`w-full px-4 py-2 border rounded-lg bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-brand ${
                formErrors.name ? 'border-error' : 'border-border'
              }`}
              placeholder={t('product.name_placeholder')}
            />
            {formErrors.name && (
              <p className="mt-1 text-sm text-error">{formErrors.name}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-foreground mb-1">
              {t('product.description')}
            </label>
            <textarea
              value={description}
              onChange={e => setDescription(e.target.value)}
              rows={4}
              className={`w-full px-4 py-2 border rounded-lg bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-brand ${
                formErrors.description ? 'border-error' : 'border-border'
              }`}
              placeholder={t('product.description_placeholder')}
            />
            {formErrors.description && (
              <p className="mt-1 text-sm text-error">{formErrors.description}</p>
            )}
          </div>
        </div>

        {/* Gallery */}
        <div className="card p-6 space-y-4">
          <h2 className="text-lg font-semibold text-foreground">{t('product.gallery')}</h2>
          <p className="text-sm text-foreground-muted">{t('product.gallery_hint')}</p>

          <div className="flex flex-wrap gap-4">
            {galleryImages.map((img) => (
              <div key={img.id} className="relative group">
                <img
                  src={img.preview}
                  alt=""
                  className="w-24 h-24 object-cover rounded-lg border border-border"
                />
                <button
                  type="button"
                  onClick={() => handleRemoveImage(img.id)}
                  className="absolute -top-2 -right-2 p-1 bg-error text-white rounded-full opacity-0 group-hover:opacity-100 transition-opacity"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            ))}

            <label className="w-24 h-24 flex flex-col items-center justify-center border-2 border-dashed border-border rounded-lg cursor-pointer hover:border-brand transition-colors">
              <svg className="w-8 h-8 text-foreground-muted" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              <span className="text-xs text-foreground-muted mt-1">{t('product.add_image')}</span>
              <input
                type="file"
                accept="image/*"
                multiple
                onChange={handleImageSelect}
                className="hidden"
              />
            </label>
          </div>
        </div>

        {/* Categories */}
        <div className="card p-6 space-y-4">
          <h2 className="text-lg font-semibold text-foreground">{t('product.categories')}</h2>

          <div className="category-dropdown relative z-20">
            <div
              onClick={(e) => {
                e.stopPropagation()
                setShowTagDropdown(false)
                setShowCategoryDropdown(!showCategoryDropdown)
              }}
              className="w-full px-4 py-2 border border-border rounded-lg bg-background text-foreground cursor-pointer flex items-center justify-between"
            >
              <span className={selectedCategories.length === 0 ? 'text-foreground-muted' : ''}>
                {selectedCategories.length === 0
                  ? t('product.select_categories')
                  : t('product.categories_selected', { count: selectedCategories.length })}
              </span>
              <svg className="w-5 h-5 text-foreground-muted" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </div>

            {showCategoryDropdown && (
              <div className="absolute z-50 w-full mt-1 bg-surface border border-border rounded-lg shadow-lg max-h-60 overflow-auto">
                <div className="p-2 sticky top-0 bg-surface border-b border-border">
                  <input
                    type="text"
                    value={categorySearch}
                    onChange={e => setCategorySearch(e.target.value)}
                    placeholder={t('common.search')}
                    className="w-full px-3 py-2 border border-border rounded-lg bg-background text-foreground text-sm"
                    onClick={e => e.stopPropagation()}
                  />
                </div>
                {filteredCategories.length === 0 ? (
                  <div className="p-3 text-foreground-muted text-sm">{t('common.no_results')}</div>
                ) : (
                  filteredCategories.map(cat => (
                    <div
                      key={cat.id}
                      onClick={(e) => {
                        e.stopPropagation()
                        toggleCategory(cat.id)
                      }}
                      className="px-4 py-2 hover:bg-background-secondary cursor-pointer flex items-center gap-2"
                    >
                      <input
                        type="checkbox"
                        checked={selectedCategories.includes(cat.id)}
                        onChange={() => {}}
                        className="w-4 h-4"
                      />
                      <span className="text-foreground">{cat.name}</span>
                    </div>
                  ))
                )}
              </div>
            )}
          </div>

          {/* Selected categories chips */}
          {selectedCategories.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {selectedCategories.map(catId => {
                const cat = categories.find(c => c.id === catId)
                return cat ? (
                  <span
                    key={catId}
                    className="inline-flex items-center gap-1 px-3 py-1 bg-brand/10 text-brand rounded-full text-sm"
                  >
                    {cat.name}
                    <button
                      type="button"
                      onClick={() => toggleCategory(catId)}
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
          )}
        </div>

        {/* Tags */}
        <div className="card p-6 space-y-4">
          <h2 className="text-lg font-semibold text-foreground">{t('product.tags')}</h2>

          <div className="tag-dropdown relative z-10">
            <div
              onClick={(e) => {
                e.stopPropagation()
                setShowCategoryDropdown(false)
                setShowTagDropdown(!showTagDropdown)
              }}
              className="w-full px-4 py-2 border border-border rounded-lg bg-background text-foreground cursor-pointer flex items-center justify-between"
            >
              <span className={selectedTags.length === 0 ? 'text-foreground-muted' : ''}>
                {selectedTags.length === 0
                  ? t('product.select_tags')
                  : t('product.tags_selected', { count: selectedTags.length })}
              </span>
              <svg className="w-5 h-5 text-foreground-muted" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </div>

            {showTagDropdown && (
              <div className="absolute z-50 w-full mt-1 bg-surface border border-border rounded-lg shadow-lg max-h-60 overflow-auto">
                <div className="p-2 sticky top-0 bg-surface border-b border-border">
                  <input
                    type="text"
                    value={tagSearch}
                    onChange={e => setTagSearch(e.target.value)}
                    placeholder={t('common.search')}
                    className="w-full px-3 py-2 border border-border rounded-lg bg-background text-foreground text-sm"
                    onClick={e => e.stopPropagation()}
                  />
                </div>
                {filteredTags.length === 0 ? (
                  <div className="p-3 text-foreground-muted text-sm">{t('common.no_results')}</div>
                ) : (
                  filteredTags.map(tag => (
                    <div
                      key={tag.id}
                      onClick={(e) => {
                        e.stopPropagation()
                        toggleTag(tag.id)
                      }}
                      className="px-4 py-2 hover:bg-background-secondary cursor-pointer flex items-center gap-2"
                    >
                      <input
                        type="checkbox"
                        checked={selectedTags.includes(tag.id)}
                        onChange={() => {}}
                        className="w-4 h-4"
                      />
                      <span className="text-foreground">{tag.name}</span>
                    </div>
                  ))
                )}
              </div>
            )}
          </div>

          {/* Selected tags chips */}
          {selectedTags.length > 0 && (
            <div className="flex flex-wrap gap-2">
           {selectedTags.map(tagId => {
                 const tag = tags.find(t => t.id === tagId)
                 return tag ? (
                   <span
                     key={tagId}
                     className="inline-flex items-center gap-1 px-3 py-1 bg-success-light dark:bg-success-dark/20 text-success dark:text-success rounded-full text-sm"
                   >
                    {tag.name}
                    <button
                      type="button"
                       onClick={() => toggleTag(tagId)}
                       className="hover:text-success-dark dark:hover:text-success"
                     >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </span>
                ) : null
              })}
            </div>
          )}
        </div>

        {/* SKUs */}
        <div className="card p-6 space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-lg font-semibold text-foreground">{t('product.sku_variants')}</h2>
              <p className="text-sm text-foreground-muted">{t('product.sku_variants_hint')}</p>
            </div>
            <button
              type="button"
              onClick={handleAddSku}
              className="btn btn-secondary text-sm"
            >
              {t('product.add_sku')}
            </button>
          </div>

           {formErrors.skus && (
             <p className="text-sm text-error">{formErrors.skus}</p>
           )}

          <div className="space-y-6">
            {skus.map((sku, skuIndex) => (
              <div key={sku.id} className="border border-border rounded-lg p-4 space-y-4">
                <div className="flex items-center justify-between">
                  <h3 className="font-medium text-foreground">
                    {t('product.sku_variant')} #{skuIndex + 1}
                  </h3>
                   {skus.length > 1 && (
                     <button
                       type="button"
                       onClick={() => handleRemoveSku(sku.id)}
                       className="text-error hover:text-error-dark text-sm"
                     >
                      {t('common.delete')}
                    </button>
                  )}
                </div>

                <div className="grid md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-foreground mb-1">
                      {t('product.price')}
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      value={sku.price}
                      onChange={e => handleSkuChange(sku.id, 'price', e.target.value)}
                      className="w-full px-4 py-2 border border-border rounded-lg bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-brand"
                      placeholder="0.00"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-foreground mb-1">
                      {t('product.stock_quantity')}
                    </label>
                    <input
                      type="number"
                      min="0"
                      value={sku.stockQuantity}
                      onChange={e => handleSkuChange(sku.id, 'stockQuantity', e.target.value)}
                      className="w-full px-4 py-2 border border-border rounded-lg bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-brand"
                      placeholder="0"
                    />
                  </div>
                </div>

                {/* Attributes for this SKU */}
                <AttributeSelector
                  attributes={sku.attributes}
                  availableAttributes={attributeDefinitions}
                  onChange={(attrs) => handleSkuAttributesChange(sku.id, attrs)}
                />
              </div>
            ))}
          </div>
        </div>

        {/* Submit */}
        <div className="flex gap-4">
          <button
            type="submit"
            disabled={submitting}
            className="btn btn-brand"
          >
            {submitting ? t('common.saving') : t('product.create_button')}
          </button>
          <button
            type="button"
            onClick={() => navigate('/cabinet/my-store')}
            className="btn btn-secondary"
          >
            {t('common.cancel')}
          </button>
        </div>
      </form>
    </div>
  )
}
