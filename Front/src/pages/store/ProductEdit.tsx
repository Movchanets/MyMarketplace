import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  type UpdateProductRequest,
  type ProductDetailsDto,
  type MediaImageDto
} from '../../api/catalogApi'
import { useCategories } from '../../hooks/queries/useCategories'
import { useAdminTags } from '../../hooks/queries/useAdminCatalog'
import { useProductById, useSetProductBaseImage, useUpdateProduct } from '../../hooks/queries/useProducts'

import { ErrorAlert } from '../../components/ui/ErrorAlert'
import { InfoAlert } from '../../components/ui/InfoAlert'

interface FormErrors {
  name?: string
}

interface NewGalleryImage {
  id: string
  file: File
  preview: string
}

const generateId = () => Math.random().toString(36).substring(2, 11)

export default function ProductEdit() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { productId } = useParams<{ productId: string }>()

  const categoriesQuery = useCategories()
  const tagsQuery = useAdminTags()
  const productQuery = useProductById(productId)
  const updateProductMutation = useUpdateProduct()
  const setProductBaseImageMutation = useSetProductBaseImage()
  const categories = categoriesQuery.data ?? []
  const tags = tagsQuery.data ?? []
  const [product, setProduct] = useState<ProductDetailsDto | null>(null)
  const loading = categoriesQuery.isLoading || tagsQuery.isLoading || productQuery.isLoading
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [formErrors, setFormErrors] = useState<FormErrors>({})

  // Form state
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [selectedCategories, setSelectedCategories] = useState<string[]>([])
  const [primaryCategoryId, setPrimaryCategoryId] = useState<string | null>(null)
  const [selectedTags, setSelectedTags] = useState<string[]>([])

  // Gallery state
  const [existingImages, setExistingImages] = useState<MediaImageDto[]>([])
  const [newImages, setNewImages] = useState<NewGalleryImage[]>([])
  const [deletedGalleryIds, setDeletedGalleryIds] = useState<string[]>([])

  // Dropdowns state
  const [showCategoryDropdown, setShowCategoryDropdown] = useState(false)
  const [showTagDropdown, setShowTagDropdown] = useState(false)
  const [categorySearch, setCategorySearch] = useState('')
  const [tagSearch, setTagSearch] = useState('')

  // Cleanup preview URLs on unmount
  useEffect(() => {
    return () => {
      newImages.forEach(img => URL.revokeObjectURL(img.preview))
    }
  }, [newImages])

  useEffect(() => {
    const p = productQuery.data
    if (!p) return

    setProduct(p)
    setName(p.name)
    setDescription(p.description || '')
    setSelectedCategories(p.categories.map(c => c.id))
    const primaryCat = p.primaryCategory || p.categories[0]
    setPrimaryCategoryId(primaryCat?.id || null)
    setSelectedTags(p.tags.map(tag => tag.id))
    setExistingImages(p.gallery || [])
  }, [productQuery.data])

  useEffect(() => {
    const queryError =
      (categoriesQuery.error instanceof Error ? categoriesQuery.error.message : null) ||
      (tagsQuery.error instanceof Error ? tagsQuery.error.message : null) ||
      (productQuery.error instanceof Error ? productQuery.error.message : null)

    if (queryError) {
      setError(queryError)
    }
  }, [categoriesQuery.error, productQuery.error, tagsQuery.error])

  const validateForm = (): boolean => {
    const errors: FormErrors = {}
    
    if (!name.trim()) {
      errors.name = t('product.name_required')
    }
    
    if (selectedCategories.length === 0) {
      setError(t('product.categories_required') || 'At least one category is required')
      return false
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
        tagIds: selectedTags.length > 0 ? selectedTags : undefined,
        primaryCategoryId: primaryCategoryId || undefined,
        galleryIdsToDelete: deletedGalleryIds.length > 0 ? deletedGalleryIds : undefined
      }

      // Collect new images to upload
      const filesToUpload = newImages.map(img => img.file)

      console.log('Update request:', {
        request,
        selectedCategoriesLength: selectedCategories.length,
        selectedCategoriesValues: selectedCategories,
        primaryCategoryId,
        newImagesCount: filesToUpload.length,
        deletedGalleryIds
      })
      
      await updateProductMutation.mutateAsync({
        productId,
        data: request,
        newImages: filesToUpload.length > 0 ? filesToUpload : undefined,
      })
      newImages.forEach(img => URL.revokeObjectURL(img.preview))
      navigate('/cabinet/products')
    } catch (err: any) {
      console.error('Update error:', err)
      console.error('Response data:', err.response?.data)
      console.error('Response status:', err.response?.status)
      // Try to get detailed error message from response
      const errorMessage = err.response?.data?.message || 
                          err.response?.data?.title ||
                          err.message ||
                          t('common.error')
      setError(errorMessage)
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

  // Gallery management
  const handleImageSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files
    if (!files || !productId) return

    const images: NewGalleryImage[] = Array.from(files).map(file => ({
      id: generateId(),
      file,
      preview: URL.createObjectURL(file)
    }))

    setNewImages([...newImages, ...images])
    e.target.value = '' // Reset input
  }

  const handleRemoveNewImage = (imageId: string) => {
    const img = newImages.find(i => i.id === imageId)
    if (img) {
      URL.revokeObjectURL(img.preview)
    }
    setNewImages(newImages.filter(i => i.id !== imageId))
  }

  const handleDeleteExistingImage = (galleryId: string) => {
    if (!confirm(t('product.confirm_delete_image'))) return

    // Mark for deletion (will be deleted on save)
    setDeletedGalleryIds(prev => [...prev, galleryId])
    // Remove from UI immediately
    setExistingImages(prev => prev.filter(img => (img.galleryId || img.id) !== galleryId))
  }

  const handleSetBaseImage = async (imageUrl: string) => {
    if (!productId) return

    try {
      setError(null)
      await setProductBaseImageMutation.mutateAsync({ productId, baseImageUrl: imageUrl })
      if (product) {
        setProduct({ ...product, baseImageUrl: imageUrl })
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errors.update_failed'))
    }
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
           <p className="text-error mb-4">{error}</p>
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
      <h1 className="text-2xl font-bold text-foreground mb-6">{t('product.edit_title')}</h1>

      {error && (
        <ErrorAlert className="mb-4">{error}</ErrorAlert>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Name */}
        <div>
          <label className="block text-sm font-medium text-foreground mb-2">
            {t('product.name')} *
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

        {/* Description */}
        <div>
          <label className="block text-sm font-medium text-foreground mb-2">
            {t('product.description')}
          </label>
          <textarea
            value={description}
            onChange={e => setDescription(e.target.value)}
            rows={4}
            className="w-full px-4 py-2 border border-border rounded-lg bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-brand resize-none"
            placeholder={t('product.description_placeholder')}
          />
        </div>

        {/* Categories */}
        <div className="relative">
          <label className="block text-sm font-medium text-foreground mb-2">
            {t('product.categories')}
          </label>
          
          {/* Selected categories chips with primary indicator */}
          <div className="flex flex-wrap gap-2 mb-2">
            {selectedCategories.map(id => {
              const cat = categories.find(c => c.id === id)
              const isPrimary = primaryCategoryId === id
              return cat ? (
                <div
                  key={id}
                  className="inline-flex items-center gap-1 px-3 py-1 rounded-full bg-brand/10 text-brand text-sm group relative"
                >
                  {isPrimary && (
                    <span className="inline-flex items-center justify-center w-4 h-4 bg-brand text-white rounded-full text-xs font-bold" title={t('product.primary_category')}>
                      ⭐
                    </span>
                  )}
                  <span>{cat.name}</span>
                  <button
                    type="button"
                    onClick={() => toggleCategory(id)}
                    className="hover:text-brand-hover ml-1"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                  
                  {/* Quick action: Set as primary */}
                  {!isPrimary && (
                    <button
                      type="button"
                      onClick={() => setPrimaryCategoryId(id)}
                      className="ml-2 px-2 py-0.5 rounded text-xs bg-brand/20 hover:bg-brand/30 text-brand transition-colors opacity-0 group-hover:opacity-100"
                      title={t('product.set_as_primary')}
                    >
                      {t('product.set_primary')}
                    </button>
                  )}
                </div>
              ) : null
            })}
          </div>

          {/* Dropdown trigger */}
          <button
            type="button"
            onClick={() => setShowCategoryDropdown(!showCategoryDropdown)}
            className="w-full px-4 py-2 border border-border rounded-lg bg-background text-left text-foreground-muted hover:border-brand transition-colors"
          >
            {t('product.select_categories')}
          </button>

          {/* Dropdown */}
          {showCategoryDropdown && (
            <div className="absolute z-50 w-full mt-1 bg-surface border border-border rounded-lg shadow-lg max-h-60 overflow-auto">
              <div className="p-2 border-b border-border bg-surface">
                <input
                  type="text"
                  value={categorySearch}
                  onChange={e => setCategorySearch(e.target.value)}
                  placeholder={t('common.search')}
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground text-sm"
                />
              </div>
              <div className="p-2">
                {filteredCategories.length === 0 ? (
                  <p className="text-foreground-muted text-sm py-2 px-3">{t('common.noResults')}</p>
                ) : (
                  filteredCategories.map(cat => {
                    const isPrimary = primaryCategoryId === cat.id
                    const isSelected = selectedCategories.includes(cat.id)
                    return (
                      <div
                        key={cat.id}
                        className="flex items-center justify-between px-3 py-2 hover:bg-background-secondary rounded group"
                      >
                        <label className="flex items-center gap-2 flex-1 cursor-pointer">
                          <input
                            type="checkbox"
                            checked={isSelected}
                            onChange={() => {
                              toggleCategory(cat.id)
                              // Auto-set first selected category as primary
                              if (!isSelected && !primaryCategoryId) {
                                setPrimaryCategoryId(cat.id)
                              }
                            }}
                            className="rounded border-border text-brand focus:ring-brand"
                          />
                          <span className="text-foreground">{cat.name}</span>
                          {isPrimary && (
                            <span className="text-xs bg-brand/20 text-brand px-2 py-1 rounded">
                              {t('product.primary')}
                            </span>
                          )}
                        </label>
                        {isSelected && !isPrimary && (
                          <button
                            type="button"
                            onClick={() => setPrimaryCategoryId(cat.id)}
                            className="px-2 py-1 text-xs rounded bg-brand/10 hover:bg-brand/20 text-brand transition-colors opacity-0 group-hover:opacity-100"
                            title={t('product.set_as_primary')}
                          >
                            ⭐ {t('product.set_primary')}
                          </button>
                        )}
                      </div>
                    )
                  })
                )}
              </div>
            </div>
          )}
        </div>

        {/* Gallery */}
        <div>
          <label className="block text-sm font-medium text-foreground mb-2">
            {t('product.gallery')}
          </label>
          <p className="text-xs text-foreground-muted mb-3">{t('product.gallery_hint')}</p>

          {/* Existing Images */}
          {existingImages.length > 0 && (
            <div className="mb-4">
              <p className="text-xs font-medium text-foreground mb-2">{t('product.current_images')}</p>
              <div className="flex flex-wrap gap-3">
                {existingImages.map((img) => {
                  const isBaseImage = product?.baseImageUrl === img.url
                  return (
                    <div key={img.id} className="relative group">
                      <div
                        className={`w-24 h-24 rounded-lg overflow-hidden border-2 cursor-pointer transition-all ${
                          isBaseImage ? 'border-brand ring-2 ring-brand/50' : 'border-border hover:border-brand'
                        }`}
                        onClick={() => handleSetBaseImage(img.url)}
                        title={isBaseImage ? t('product.base_image_current') : t('product.set_as_base')}
                      >
                        <img
                          src={img.url}
                          alt={img.altText || ''}
                          className="w-full h-full object-cover"
                        />
                        {isBaseImage && (
                          <div className="absolute top-1 left-1 bg-brand text-white text-xs px-1.5 py-0.5 rounded">
                            {t('product.base')}
                          </div>
                        )}
                      </div>
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation()
                          handleDeleteExistingImage(img.galleryId || img.id)
                        }}
                        className="absolute -top-2 -right-2 p-1 bg-red-500 text-white rounded-full 
                          opacity-0 group-hover:opacity-100 transition-opacity"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                      </button>
                    </div>
                  )
                })}
              </div>
            </div>
          )}

          {/* New Images to Upload (will be saved with form) */}
          {newImages.length > 0 && (
            <div className="mb-4">
              <p className="text-xs font-medium text-foreground mb-2">
                {t('product.new_images')} 
                <span className="text-foreground-muted ml-1">({t('product.will_upload_on_save')})</span>
              </p>
              <div className="flex flex-wrap gap-3">
                {newImages.map((img) => (
                  <div key={img.id} className="relative group">
                    <img
                      src={img.preview}
                      alt=""
                      className="w-24 h-24 object-cover rounded-lg border-2 border-dashed border-brand/50"
                    />
                       <button
                       type="button"
                       onClick={() => handleRemoveNewImage(img.id)}
                       className="absolute -top-2 -right-2 p-1 bg-error text-white rounded-full 
                       opacity-0 group-hover:opacity-100 transition-opacity"
                     >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Add New Images */}
          <label className="inline-flex flex-col items-center justify-center px-4 py-3 border-2 border-dashed 
            border-border rounded-lg cursor-pointer hover:border-brand transition-colors">
            <svg className="w-8 h-8 text-foreground-muted" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            <span className="text-sm text-foreground-muted mt-1">{t('product.add_images')}</span>
            <input
              type="file"
              accept="image/*"
              multiple
              onChange={handleImageSelect}
              className="hidden"
            />
          </label>
        </div>

        {/* Tags */}
        <div className="relative">
          <label className="block text-sm font-medium text-foreground mb-2">
            {t('product.tags')}
          </label>
          
          {/* Selected tags chips */}
          <div className="flex flex-wrap gap-2 mb-2">
             {selectedTags.map(id => {
               const tag = tags.find(t => t.id === id)
               return tag ? (
                 <span
                   key={id}
                   className="inline-flex items-center gap-1 px-3 py-1 rounded-full bg-foreground/10 text-foreground text-sm"
                 >
                  {tag.name}
                  <button
                    type="button"
                     onClick={() => toggleTag(id)}
                     className="hover:text-error"
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
            className="w-full px-4 py-2 border border-border rounded-lg bg-background text-left text-foreground-muted hover:border-brand transition-colors"
          >
            {t('product.select_tags')}
          </button>

          {/* Dropdown */}
          {showTagDropdown && (
            <div className="absolute z-50 w-full mt-1 bg-surface border border-border rounded-lg shadow-lg max-h-60 overflow-auto">
              <div className="p-2 border-b border-border bg-surface">
                <input
                  type="text"
                  value={tagSearch}
                  onChange={e => setTagSearch(e.target.value)}
                  placeholder={t('common.search')}
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground text-sm"
                />
              </div>
              <div className="p-2">
                {filteredTags.length === 0 ? (
                  <p className="text-foreground-muted text-sm py-2 px-3">{t('common.noResults')}</p>
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
                      <span className="text-foreground">{tag.name}</span>
                    </label>
                  ))
                )}
              </div>
            </div>
          )}
        </div>

         {/* Info about SKUs */}
         <InfoAlert>
           <p className="text-sm">
             {t('product.edit_sku_info')}
           </p>
         </InfoAlert>

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
