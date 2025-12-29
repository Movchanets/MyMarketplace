import { useState, useEffect, useCallback } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  productsApi,
  type ProductDetailsDto,
  type SkuDto,
  type AddSkuRequest,
  type UpdateSkuRequest
} from '../../api/catalogApi'
import {
  attributeDefinitionsApi,
  type AttributeDefinitionDto,
} from '../../api/attributeDefinitionsApi'
import AttributeSelector from '../../components/catalog/AttributeSelector'

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

  const [product, setProduct] = useState<ProductDetailsDto | null>(null)
  const [skus, setSkus] = useState<SkuFormData[]>([])
  const [attributeDefinitions, setAttributeDefinitions] = useState<AttributeDefinitionDto[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [formErrors, setFormErrors] = useState<FormErrors>({})
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null)

  const mapSkuToForm = (sku: SkuDto): SkuFormData => ({
    id: sku.id,
    skuCode: sku.skuCode,
    price: sku.price.toString(),
    stockQuantity: sku.stockQuantity.toString(),
    attributes: parseAttributes(sku.attributes),
    isNew: false,
    isModified: false
  })

  const fetchData = useCallback(async () => {
    if (!productId) return
    
    setLoading(true)
    setError(null)
    try {
      const [productResult, attributesResult] = await Promise.all([
        productsApi.getById(productId),
        attributeDefinitionsApi.getAll()
      ])
      
      if (productResult.isSuccess && productResult.payload) {
        setProduct(productResult.payload)
        setSkus(productResult.payload.skus.map(mapSkuToForm))
      } else {
        setError(t('product.notFound'))
      }
      
      if (attributesResult.isSuccess && attributesResult.payload) {
        setAttributeDefinitions(attributesResult.payload)
      }
    } catch {
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
      const result = await productsApi.deleteSku(productId, skuId)
      if (result.isSuccess) {
        setSkus(skus.filter(s => s.id !== skuId))
        setSuccessMessage(t('sku.deleted_success'))
        setTimeout(() => setSuccessMessage(null), 3000)
      } else {
        setError(result.message || t('errors.delete_failed'))
      }
    } catch {
      setError(t('errors.delete_failed'))
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
        const result = await productsApi.addSku(productId, request)
        
        if (result.isSuccess && result.payload) {
          // Update the SKU with the new ID from the server
          setSkus(skus.map(s => s.id === skuId
            ? { ...s, id: result.payload!, isNew: false, isModified: false }
            : s
          ))
          setSuccessMessage(t('sku.created_success'))
          // Refresh to get the new SKU code
          await fetchData()
        } else {
          setError(result.message || t('errors.save_failed'))
        }
      } else {
        // Update existing SKU
        const request: UpdateSkuRequest = {
          price,
          stockQuantity,
          attributes
        }
        const result = await productsApi.updateSku(productId, skuId, request)
        
        if (result.isSuccess) {
          setSkus(skus.map(s => s.id === skuId
            ? { ...s, isModified: false }
            : s
          ))
          setSuccessMessage(t('sku.updated_success'))
          // Refresh to get updated data
          await fetchData()
        } else {
          setError(result.message || t('errors.update_failed'))
        }
      }
      
      setTimeout(() => setSuccessMessage(null), 3000)
    } catch {
      setError(t('errors.save_failed'))
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
          await productsApi.addSku(productId, request)
        } else {
          const request: UpdateSkuRequest = { price, stockQuantity, attributes }
          await productsApi.updateSku(productId, sku.id, request)
        }
      }

      setSuccessMessage(t('sku.all_saved_success'))
      await fetchData()
      setTimeout(() => setSuccessMessage(null), 3000)
    } catch {
      setError(t('errors.save_failed'))
    } finally {
      setSaving(false)
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
            <h1 className="text-2xl font-bold text-text">{t('sku.management_title')}</h1>
            {product && (
              <p className="text-text-muted mt-1">{product.name}</p>
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
        <div className="mb-6 bg-red-100 dark:bg-red-900/30 border border-red-400 dark:border-red-700 text-red-700 dark:text-red-300 px-4 py-3 rounded">
          {error}
        </div>
      )}

      {successMessage && (
        <div className="mb-6 bg-green-100 dark:bg-green-900/30 border border-green-400 dark:border-green-700 text-green-700 dark:text-green-300 px-4 py-3 rounded">
          {successMessage}
        </div>
      )}

      {formErrors.skus && (
        <div className="mb-6 bg-yellow-100 dark:bg-yellow-900/30 border border-yellow-400 dark:border-yellow-700 text-yellow-700 dark:text-yellow-300 px-4 py-3 rounded">
          {formErrors.skus}
        </div>
      )}

      {/* SKU List */}
      {skus.length === 0 ? (
        <div className="card p-8 text-center">
          <p className="text-text-muted mb-4">{t('sku.no_variants')}</p>
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
                  <h3 className="font-semibold text-text">
                    {t('sku.variant')} #{index + 1}
                  </h3>
                  {sku.skuCode && (
                    <span className="px-2 py-1 bg-gray-100 dark:bg-gray-800 text-text-muted text-xs rounded font-mono">
                      {sku.skuCode}
                    </span>
                  )}
                  {sku.isNew && (
                    <span className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 text-xs rounded">
                      {t('sku.new')}
                    </span>
                  )}
                  {sku.isModified && !sku.isNew && (
                    <span className="px-2 py-1 bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-300 text-xs rounded">
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
                      <span className="text-sm text-red-500">{t('sku.confirm_delete')}</span>
                      <button
                        onClick={() => handleRemoveSku(sku.id)}
                        disabled={saving}
                        className="btn btn-danger text-sm"
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
                      className="text-red-500 hover:text-red-700 disabled:opacity-50 disabled:cursor-not-allowed"
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
                  <label className="block text-sm font-medium text-text mb-1">
                    {t('sku.price')} *
                  </label>
                  <div className="relative">
                    <span className="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted">₴</span>
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      value={sku.price}
                      onChange={e => handleSkuChange(sku.id, 'price', e.target.value)}
                      className="w-full pl-8 pr-4 py-2 border border-border rounded-lg bg-background text-text focus:outline-none focus:ring-2 focus:ring-brand"
                      placeholder="0.00"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-text mb-1">
                    {t('sku.stock_quantity')} *
                  </label>
                  <input
                    type="number"
                    min="0"
                    value={sku.stockQuantity}
                    onChange={e => handleSkuChange(sku.id, 'stockQuantity', e.target.value)}
                    className="w-full px-4 py-2 border border-border rounded-lg bg-background text-text focus:outline-none focus:ring-2 focus:ring-brand"
                    placeholder="0"
                  />
                </div>
              </div>

              {/* Attributes */}
              <div className="space-y-3">
                <label className="text-sm font-medium text-text">{t('sku.attributes')}</label>
                <AttributeSelector
                  attributes={sku.attributes}
                  availableAttributes={attributeDefinitions}
                  onChange={(attrs) => handleSkuAttributesChange(sku.id, attrs)}
                />
              </div>
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
