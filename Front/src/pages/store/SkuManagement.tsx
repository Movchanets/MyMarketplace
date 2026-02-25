import { useState, useEffect, useCallback } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  type ProductDetailsDto,
  type SkuDto,
  type AddSkuRequest,
  type UpdateSkuRequest,
  type MediaImageDto
} from '../../api/catalogApi'
import { type AttributeDefinitionDto } from '../../api/attributeDefinitionsApi'
import { useAdminAttributeDefinitions } from '../../hooks/queries/useAdminCatalog'
import {
  useAddSku,
  useDeleteSku,
  useDeleteSkuGalleryImage,
  useProductById,
  useUpdateSku,
  useUploadSkuGalleryImage,
} from '../../hooks/queries/useProducts'
import AttributeSelector from '../../components/catalog/AttributeSelector'
import { ErrorAlert } from '../../components/ui/ErrorAlert'
import { SuccessAlert } from '../../components/ui/SuccessAlert'
import { WarningAlert } from '../../components/ui/WarningAlert'

interface AttributeField {
  key: string
  value: string
}

interface SkuFormData {
  id: string
  skuCode: string
  price: string
  stockQuantity: string
  attributes: AttributeField[]
  gallery: MediaImageDto[]
  isNew: boolean
  isModified: boolean
}

interface FormErrors {
  skus?: string
}

const generateTempId = () => `temp_${Math.random().toString(36).substring(2, 9)}`

const parseAttributes = (attrs: Record<string, unknown> | null): AttributeField[] => {
  if (!attrs) return []
  return Object.entries(attrs).map(([key, value]) => ({
    key,
    value: String(value ?? '')
  }))
}

const buildAttributesObject = (attrs: AttributeField[]): Record<string, unknown> | undefined => {
  const result: Record<string, unknown> = {}
  for (const attr of attrs) {
    const trimmedKey = attr.key.trim()
    if (!trimmedKey) continue
    
    const val = attr.value.trim()
    if (val === 'true') {
      result[trimmedKey] = true
    } else if (val === 'false') {
      result[trimmedKey] = false
    } else if (!isNaN(Number(val)) && val !== '') {
      result[trimmedKey] = Number(val)
    } else {
      result[trimmedKey] = val
    }
  }
  return Object.keys(result).length > 0 ? result : undefined
}

export default function SkuManagement() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { productId } = useParams<{ productId: string }>()
  const productQuery = useProductById(productId)
  const attributesQuery = useAdminAttributeDefinitions()
  const addSkuMutation = useAddSku()
  const updateSkuMutation = useUpdateSku()
  const deleteSkuMutation = useDeleteSku()
  const uploadSkuGalleryImageMutation = useUploadSkuGalleryImage()
  const deleteSkuGalleryImageMutation = useDeleteSkuGalleryImage()

  const [product, setProduct] = useState<ProductDetailsDto | null>(null)
  const [skus, setSkus] = useState<SkuFormData[]>([])
  const [attributeDefinitions, setAttributeDefinitions] = useState<AttributeDefinitionDto[]>([])
  const loading = productQuery.isLoading || attributesQuery.isLoading
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [formErrors, setFormErrors] = useState<FormErrors>({})
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null)
  const [uploadingGallery, setUploadingGallery] = useState<Set<string>>(new Set())
  const [deletingGalleryImages, setDeletingGalleryImages] = useState<Set<string>>(new Set())

  const mapSkuToForm = (sku: SkuDto): SkuFormData => ({
    id: sku.id,
    skuCode: sku.skuCode,
    price: sku.price.toString(),
    stockQuantity: sku.stockQuantity.toString(),
    attributes: parseAttributes(sku.attributes),
    gallery: sku.gallery || [],
    isNew: false,
    isModified: false
  })

  const fetchData = useCallback(async () => {
    if (!productId) return
    await Promise.all([productQuery.refetch(), attributesQuery.refetch()])
  }, [attributesQuery, productId, productQuery])

  useEffect(() => {
    if (productQuery.data) {
      setProduct(productQuery.data)
      setSkus(productQuery.data.skus.map(mapSkuToForm))
    }
  }, [productQuery.data])

  useEffect(() => {
    if (attributesQuery.data) {
      setAttributeDefinitions(attributesQuery.data)
    }
  }, [attributesQuery.data])

  useEffect(() => {
    const queryError =
      (productQuery.error instanceof Error ? productQuery.error.message : null) ||
      (attributesQuery.error instanceof Error ? attributesQuery.error.message : null)

    if (queryError) {
      setError(queryError)
    }
  }, [attributesQuery.error, productQuery.error])

  const validateForm = (): boolean => {
    const errors: FormErrors = {}

    for (const sku of skus) {
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

      // Check for duplicate attribute keys within a SKU
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

  const handleAddSku = () => {
    setSkus([
      ...skus,
      {
        id: generateTempId(),
        skuCode: '',
        price: '',
        stockQuantity: '0',
        attributes: [],
        gallery: [],
        isNew: true,
        isModified: true
      }
    ])
  }

  const handleRemoveSku = async (skuId: string) => {
    if (!productId) return
    
    const sku = skus.find(s => s.id === skuId)
    if (!sku) return

    // If it's a new SKU (not saved yet), just remove it from the list
    if (sku.isNew) {
      setSkus(skus.filter(s => s.id !== skuId))
      setDeleteConfirm(null)
      return
    }

    // If we're not in confirm mode, show confirmation
    if (deleteConfirm !== skuId) {
      setDeleteConfirm(skuId)
      return
    }

    // Delete from API
    setSaving(true)
    try {
      await deleteSkuMutation.mutateAsync({ productId, skuId })
      setSkus(skus.filter(s => s.id !== skuId))
      setSuccessMessage(t('sku.deleted_success'))
      setTimeout(() => setSuccessMessage(null), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errors.delete_failed'))
    } finally {
      setSaving(false)
      setDeleteConfirm(null)
    }
  }

  const handleSkuChange = (skuId: string, field: 'price' | 'stockQuantity', value: string) => {
    setSkus(skus.map(s => s.id === skuId 
      ? { ...s, [field]: value, isModified: true } 
      : s
    ))
  }

  const handleSkuAttributesChange = (skuId: string, attributes: { key: string; value: string }[]) => {
    setSkus(skus.map(s => s.id === skuId
      ? { ...s, attributes, isModified: true }
      : s
    ))
  }

  const handleSaveSku = async (skuId: string) => {
    if (!productId || !validateForm()) return

    const sku = skus.find(s => s.id === skuId)
    if (!sku) return

    setSaving(true)
    setError(null)

    try {
      const price = parseFloat(sku.price) || 0
      const stockQuantity = parseInt(sku.stockQuantity, 10) || 0
      const attributes = buildAttributesObject(sku.attributes)

      if (sku.isNew) {
        // Create new SKU
        const request: AddSkuRequest = {
          price,
          stockQuantity,
          attributes
        }
        const createdSkuId = await addSkuMutation.mutateAsync({ productId, data: request })
        setSkus(skus.map(s => s.id === skuId
          ? { ...s, id: createdSkuId, isNew: false, isModified: false }
          : s
        ))
        setSuccessMessage(t('sku.created_success'))
        await fetchData()
      } else {
        // Update existing SKU
        const request: UpdateSkuRequest = {
          price,
          stockQuantity,
          attributes
        }
        await updateSkuMutation.mutateAsync({ productId, skuId, data: request })
        setSkus(skus.map(s => s.id === skuId
          ? { ...s, isModified: false }
          : s
        ))
        setSuccessMessage(t('sku.updated_success'))
        await fetchData()
      }
      
      setTimeout(() => setSuccessMessage(null), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errors.save_failed'))
    } finally {
      setSaving(false)
    }
  }

  const handleSaveAll = async () => {
    if (!productId || !validateForm()) return

    const modifiedSkus = skus.filter(s => s.isModified)
    if (modifiedSkus.length === 0) {
      setSuccessMessage(t('sku.no_changes'))
      setTimeout(() => setSuccessMessage(null), 3000)
      return
    }

    setSaving(true)
    setError(null)

    try {
      for (const sku of modifiedSkus) {
        const price = parseFloat(sku.price) || 0
        const stockQuantity = parseInt(sku.stockQuantity, 10) || 0
        const attributes = buildAttributesObject(sku.attributes)

        if (sku.isNew) {
          const request: AddSkuRequest = { price, stockQuantity, attributes }
          await addSkuMutation.mutateAsync({ productId, data: request })
        } else {
          const request: UpdateSkuRequest = { price, stockQuantity, attributes }
          await updateSkuMutation.mutateAsync({ productId, skuId: sku.id, data: request })
        }
      }

      setSuccessMessage(t('sku.all_saved_success'))
      await fetchData()
      setTimeout(() => setSuccessMessage(null), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errors.save_failed'))
    } finally {
      setSaving(false)
    }
  }

  // Gallery handlers
  const handleGalleryImageUpload = async (skuId: string, e: React.ChangeEvent<HTMLInputElement>) => {
    if (!productId || !e.target.files?.length) return
    
    const sku = skus.find(s => s.id === skuId)
    if (!sku || sku.isNew) return // Can't upload images to unsaved SKUs

    setUploadingGallery(prev => new Set(prev).add(skuId))
    setError(null)

    try {
      for (const file of Array.from(e.target.files)) {
        const displayOrder = sku.gallery.length
        await uploadSkuGalleryImageMutation.mutateAsync({ productId, skuId, file, displayOrder })
      }
      
      setSuccessMessage(t('sku.image_uploaded'))
      await fetchData()
      setTimeout(() => setSuccessMessage(null), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errors.upload_failed'))
    } finally {
      setUploadingGallery(prev => {
        const next = new Set(prev)
        next.delete(skuId)
        return next
      })
      e.target.value = ''
    }
  }

  const handleGalleryImageDelete = async (skuId: string, galleryId: string) => {
    if (!productId || !confirm(t('sku.confirm_delete_image'))) return

    setDeletingGalleryImages(prev => new Set(prev).add(galleryId))
    setError(null)

    try {
      await deleteSkuGalleryImageMutation.mutateAsync({ productId, skuId, galleryId })
      setSkus(skus.map(s => s.id === skuId
        ? { ...s, gallery: s.gallery.filter(img => img.id !== galleryId) }
        : s
      ))
      setSuccessMessage(t('sku.image_deleted'))
      setTimeout(() => setSuccessMessage(null), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errors.delete_failed'))
    } finally {
      setDeletingGalleryImages(prev => {
        const next = new Set(prev)
        next.delete(galleryId)
        return next
      })
    }
  }

  const hasUnsavedChanges = skus.some(s => s.isModified)

  if (loading) {
    return (
      <div className="p-6 max-w-4xl mx-auto">
        <div className="animate-pulse space-y-4">
          <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded w-1/3"></div>
          <div className="h-64 bg-gray-200 dark:bg-gray-700 rounded"></div>
        </div>
      </div>
    )
  }

  if (error && !product) {
    return (
     <div className="p-6 max-w-4xl mx-auto">
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
    <div className="p-6 max-w-4xl mx-auto">
      {/* Header */}
      <div className="mb-6">
        <button
          onClick={() => navigate('/cabinet/products')}
          className="text-brand hover:text-brand-hover flex items-center gap-2 mb-4"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
          {t('sku.back_to_products')}
        </button>

        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-foreground">{t('sku.management_title')}</h1>
            {product && (
              <p className="text-foreground-muted mt-1">{product.name}</p>
            )}
          </div>
          
          <div className="flex gap-2">
            <button
              onClick={handleAddSku}
              className="btn btn-secondary"
              disabled={saving}
            >
              {t('sku.add_variant')}
            </button>
            {hasUnsavedChanges && (
              <button
                onClick={handleSaveAll}
                className="btn btn-brand"
                disabled={saving}
              >
                {saving ? t('common.saving') : t('sku.save_all')}
              </button>
            )}
          </div>
        </div>
      </div>

       {/* Messages */}
       {error && (
         <ErrorAlert className="mb-6">{error}</ErrorAlert>
       )}

       {successMessage && (
         <SuccessAlert className="mb-6">{successMessage}</SuccessAlert>
       )}

       {formErrors.skus && (
         <WarningAlert className="mb-6">{formErrors.skus}</WarningAlert>
       )}

      {/* SKU List */}
      {skus.length === 0 ? (
        <div className="card p-8 text-center">
          <p className="text-foreground-muted mb-4">{t('sku.no_variants')}</p>
          <button onClick={handleAddSku} className="btn btn-brand">
            {t('sku.add_first_variant')}
          </button>
        </div>
      ) : (
        <div className="space-y-6">
          {skus.map((sku, index) => (
            <div
              key={sku.id}
              className={`card p-6 space-y-4 ${sku.isModified ? 'ring-2 ring-brand/50' : ''}`}
            >
              {/* SKU Header */}
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <h3 className="font-semibold text-foreground">
                    {t('sku.variant')} #{index + 1}
                  </h3>
                  {sku.skuCode && (
                    <span className="px-2 py-1 bg-gray-100 dark:bg-gray-800 text-foreground-muted text-xs rounded font-mono">
                      {sku.skuCode}
                    </span>
                  )}
                   {sku.isNew && (
                     <span className="px-2 py-1 bg-info-light dark:bg-info-dark/20 text-info dark:text-info text-xs rounded">
                       {t('sku.new')}
                     </span>
                   )}
                   {sku.isModified && !sku.isNew && (
                     <span className="px-2 py-1 bg-warning-light dark:bg-warning-dark/20 text-warning dark:text-warning text-xs rounded">
                       {t('sku.modified')}
                     </span>
                   )}
                </div>
                
                <div className="flex items-center gap-2">
                  {sku.isModified && (
                    <button
                      onClick={() => handleSaveSku(sku.id)}
                      disabled={saving}
                      className="btn btn-brand text-sm"
                    >
                      {saving ? t('common.saving') : t('common.save')}
                    </button>
                  )}
                  
                   {deleteConfirm === sku.id ? (
                     <div className="flex items-center gap-2">
                       <span className="text-sm text-error">{t('sku.confirm_delete')}</span>
                       <button
                         onClick={() => handleRemoveSku(sku.id)}
                         disabled={saving}
                         className="btn btn-error text-sm"
                       >
                        {t('common.delete')}
                      </button>
                      <button
                        onClick={() => setDeleteConfirm(null)}
                        className="btn btn-secondary text-sm"
                      >
                        {t('common.cancel')}
                      </button>
                    </div>
                   ) : (
                     <button
                       onClick={() => handleRemoveSku(sku.id)}
                       disabled={saving || (skus.length === 1 && !sku.isNew)}
                       className="text-error hover:text-error-dark disabled:opacity-50 disabled:cursor-not-allowed"
                       title={skus.length === 1 && !sku.isNew ? t('sku.cannot_delete_last') : t('common.delete')}
                    >
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    </button>
                  )}
                </div>
              </div>

              {/* Price & Stock */}
              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    {t('sku.price')} *
                  </label>
                  <div className="relative">
                    <span className="absolute left-3 top-1/2 -translate-y-1/2 text-foreground-muted">₴</span>
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      value={sku.price}
                      onChange={e => handleSkuChange(sku.id, 'price', e.target.value)}
                      className="w-full pl-8 pr-4 py-2 border border-border rounded-lg bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-brand"
                      placeholder="0.00"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    {t('sku.stock_quantity')} *
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

              {/* Attributes */}
              <div className="space-y-3">
                <label className="text-sm font-medium text-foreground">{t('sku.attributes')}</label>
                <AttributeSelector
                  attributes={sku.attributes}
                  availableAttributes={attributeDefinitions}
                  onChange={(attrs) => handleSkuAttributesChange(sku.id, attrs)}
                />
              </div>

              {/* Gallery - only for saved SKUs */}
              {!sku.isNew && (
                <div className="space-y-3 pt-4 border-t border-border">
                  <div>
                    <label className="text-sm font-medium text-foreground">{t('sku.gallery')}</label>
                    <p className="text-xs text-foreground-muted mt-0.5">{t('sku.gallery_hint')}</p>
                  </div>

                  <div className="flex flex-wrap gap-3 items-start">
                    {/* Existing images */}
                    {sku.gallery.map((img) => (
                      <div key={img.id} className="relative group">
                        <img
                          src={img.url}
                          alt={img.altText || ''}
                          className="w-20 h-20 object-cover rounded-lg border border-border"
                        />
                         <button
                           type="button"
                           onClick={() => handleGalleryImageDelete(sku.id, img.id)}
                           disabled={deletingGalleryImages.has(img.id)}
                           className="absolute -top-2 -right-2 p-1 bg-error text-white rounded-full 
                           opacity-0 group-hover:opacity-100 transition-opacity disabled:opacity-50"
                         >
                          {deletingGalleryImages.has(img.id) ? (
                            <svg className="w-3 h-3 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                            </svg>
                          ) : (
                            <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                            </svg>
                          )}
                        </button>
                      </div>
                    ))}

                    {/* Add image button */}
                    <label className="w-20 h-20 flex flex-col items-center justify-center border-2 border-dashed 
                      border-border rounded-lg cursor-pointer hover:border-brand transition-colors">
                      {uploadingGallery.has(sku.id) ? (
                        <svg className="w-6 h-6 text-foreground-muted animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                        </svg>
                      ) : (
                        <>
                          <svg className="w-6 h-6 text-foreground-muted" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                          </svg>
                          <span className="text-xs text-foreground-muted mt-1">{t('sku.add_image')}</span>
                        </>
                      )}
                      <input
                        type="file"
                        accept="image/*"
                        multiple
                        onChange={(e) => handleGalleryImageUpload(sku.id, e)}
                        disabled={uploadingGallery.has(sku.id)}
                        className="hidden"
                      />
                    </label>
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Bottom Actions */}
      {skus.length > 0 && (
        <div className="mt-6 flex justify-between items-center">
          <button
            onClick={() => navigate('/cabinet/products')}
            className="btn btn-secondary"
          >
            {t('common.back')}
          </button>
          
          {hasUnsavedChanges && (
            <button
              onClick={handleSaveAll}
              className="btn btn-brand"
              disabled={saving}
            >
              {saving ? t('common.saving') : t('sku.save_all')}
            </button>
          )}
        </div>
      )}
    </div>
  )
}
