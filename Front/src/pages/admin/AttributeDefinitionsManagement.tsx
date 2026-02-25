import { useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import {
  type AttributeDefinitionDto,
  type CreateAttributeDefinitionRequest,
  type UpdateAttributeDefinitionRequest,
  type AttributeDataType,
} from '../../api/attributeDefinitionsApi'
import {
  useAdminAttributeDefinitions,
  useCreateAttributeDefinition,
  useDeleteAttributeDefinition,
  useUpdateAttributeDefinition,
} from '../../hooks/queries/useAdminCatalog'

const ITEMS_PER_PAGE = 10

interface FormErrors {
  code?: string
  name?: string
  dataType?: string
  allowedValues?: string
}

const DATA_TYPES: AttributeDataType[] = ['string', 'number', 'boolean', 'array']

export default function AttributeDefinitionsManagement() {
  const { t } = useTranslation()
  const [error, setError] = useState<string | null>(null)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [showCreateForm, setShowCreateForm] = useState(false)
  const [currentPage, setCurrentPage] = useState(1)
  const [formErrors, setFormErrors] = useState<FormErrors>({})
  const attributesQuery = useAdminAttributeDefinitions()
  const createAttributeMutation = useCreateAttributeDefinition()
  const updateAttributeMutation = useUpdateAttributeDefinition()
  const deleteAttributeMutation = useDeleteAttributeDefinition()
  const attributes = attributesQuery.data ?? []
  const loading = attributesQuery.isLoading
  const queryError = attributesQuery.error instanceof Error ? attributesQuery.error.message : null

  // Form state
  const [formData, setFormData] = useState<CreateAttributeDefinitionRequest>({
    code: '',
    name: '',
    dataType: 'string',
    isRequired: false,
    isVariant: false,
    allowedValues: [],
    displayOrder: 0,
    unit: '',
    description: '',
  })

  // Allowed values as text for editing
  const [allowedValuesText, setAllowedValuesText] = useState('')

  // Pagination
  const totalPages = Math.ceil(attributes.length / ITEMS_PER_PAGE)
  const paginatedAttributes = useMemo(() => {
    const start = (currentPage - 1) * ITEMS_PER_PAGE
    return attributes.slice(start, start + ITEMS_PER_PAGE)
  }, [attributes, currentPage])

  const resetForm = () => {
    setFormData({
      code: '',
      name: '',
      dataType: 'string',
      isRequired: false,
      isVariant: false,
      allowedValues: [],
      displayOrder: 0,
      unit: '',
      description: '',
    })
    setAllowedValuesText('')
    setEditingId(null)
    setShowCreateForm(false)
    setFormErrors({})
  }

  const validateForm = (): boolean => {
    const errors: FormErrors = {}

    if (!editingId) {
      // Code validation only for create
      if (!formData.code.trim()) {
        errors.code = t('validation.required')
      } else if (!/^[a-z][a-z0-9_]*$/.test(formData.code.trim())) {
        errors.code = t('admin.attributes.code_format_error')
      } else if (formData.code.length > 50) {
        errors.code = t('validation.max_50')
      }
    }

    if (!formData.name.trim()) {
      errors.name = t('validation.required')
    } else if (formData.name.length > 200) {
      errors.name = t('validation.max_200')
    }

    if (!formData.dataType) {
      errors.dataType = t('validation.required')
    }

    setFormErrors(errors)
    return Object.keys(errors).length === 0
  }

  const parseAllowedValues = (): string[] => {
    if (!allowedValuesText.trim()) return []
    return allowedValuesText
      .split('\n')
      .map((v) => v.trim())
      .filter((v) => v.length > 0)
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!validateForm()) return
    try {
      setError(null)
      const request: CreateAttributeDefinitionRequest = {
        ...formData,
        allowedValues: parseAllowedValues(),
        unit: formData.unit || undefined,
        description: formData.description || undefined,
      }
      await createAttributeMutation.mutateAsync(request)
      setCurrentPage(1)
      resetForm()
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errors.save_failed'))
    }
  }

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingId) return
    if (!validateForm()) return
    try {
      setError(null)
      const request: UpdateAttributeDefinitionRequest = {
        name: formData.name,
        dataType: formData.dataType,
        isRequired: formData.isRequired,
        isVariant: formData.isVariant,
        allowedValues: parseAllowedValues(),
        displayOrder: formData.displayOrder,
        unit: formData.unit || undefined,
        description: formData.description || undefined,
      }
      await updateAttributeMutation.mutateAsync({ id: editingId, data: request })
      resetForm()
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errors.save_failed'))
    }
  }

  const handleDelete = async (id: string) => {
    if (!window.confirm(t('admin.catalog.confirm_delete'))) return
    try {
      setError(null)
      await deleteAttributeMutation.mutateAsync(id)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errors.save_failed'))
    }
  }

  const startEdit = (attr: AttributeDefinitionDto) => {
    setFormData({
      code: attr.code,
      name: attr.name,
      dataType: attr.dataType,
      isRequired: attr.isRequired,
      isVariant: attr.isVariant,
      displayOrder: attr.displayOrder,
      unit: attr.unit || '',
      description: attr.description || '',
    })
    setAllowedValuesText(attr.allowedValues?.join('\n') || '')
    setEditingId(attr.id)
    setShowCreateForm(true)
  }

  const getDataTypeBadgeClass = (dataType: AttributeDataType): string => {
    switch (dataType) {
      case 'string':
        return 'bg-blue-500/20 text-blue-400'
      case 'number':
        return 'bg-green-500/20 text-green-400'
      case 'boolean':
        return 'bg-purple-500/20 text-purple-400'
      case 'array':
        return 'bg-orange-500/20 text-orange-400'
      default:
        return 'bg-gray-500/20 text-gray-400'
    }
  }

  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-[200px]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-brand"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-foreground">{t('admin.attributes.title')}</h2>
        <button
          onClick={() => {
            resetForm()
            setShowCreateForm(true)
          }}
          className="btn btn-brand"
        >
          {t('admin.attributes.add')}
        </button>
      </div>

      {(error || queryError) && (
        <div className="bg-red-100 dark:bg-red-900/30 border border-red-400 dark:border-red-700 text-red-700 dark:text-red-300 px-4 py-3 rounded">
          {error || queryError}
        </div>
      )}

      {/* Create/Edit Form */}
      {showCreateForm && (
        <div className="card p-6">
          <h3 className="text-lg font-semibold text-foreground mb-4">
            {editingId ? t('admin.attributes.edit') : t('admin.attributes.add')}
          </h3>
          <form onSubmit={editingId ? handleUpdate : handleCreate} className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* Code - only for create */}
              {!editingId && (
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    {t('admin.attributes.code')} *
                  </label>
                  <input
                    type="text"
                    value={formData.code}
                    onChange={(e) => {
                      const value = e.target.value.toLowerCase().replace(/[^a-z0-9_]/g, '_')
                      setFormData({ ...formData, code: value })
                      if (formErrors.code) setFormErrors({ ...formErrors, code: undefined })
                    }}
                    placeholder="e.g. color, size, weight"
                    className={`w-full px-3 py-2 rounded-lg border bg-surface text-foreground 
                      focus:outline-none focus:ring-2 focus:ring-brand focus:border-transparent
                      ${formErrors.code ? 'border-red-500' : 'border-gray-600'}`}
                    maxLength={50}
                  />
                  {formErrors.code && <p className="mt-1 text-sm text-red-500">{formErrors.code}</p>}
                  <p className="mt-1 text-xs text-foreground-muted">{t('admin.attributes.code_hint')}</p>
                </div>
              )}

              {/* Name */}
              <div>
                <label className="block text-sm font-medium text-foreground mb-1">
                  {t('admin.attributes.name')} *
                </label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => {
                    setFormData({ ...formData, name: e.target.value })
                    if (formErrors.name) setFormErrors({ ...formErrors, name: undefined })
                  }}
                  placeholder="e.g. Color, Size, Weight"
                  className={`w-full px-3 py-2 rounded-lg border bg-surface text-foreground 
                    focus:outline-none focus:ring-2 focus:ring-brand focus:border-transparent
                    ${formErrors.name ? 'border-red-500' : 'border-gray-600'}`}
                  maxLength={200}
                />
                {formErrors.name && <p className="mt-1 text-sm text-red-500">{formErrors.name}</p>}
              </div>

              {/* Data Type */}
              <div>
                <label className="block text-sm font-medium text-foreground mb-1">
                  {t('admin.attributes.data_type')} *
                </label>
                <select
                  value={formData.dataType}
                  onChange={(e) =>
                    setFormData({ ...formData, dataType: e.target.value as AttributeDataType })
                  }
                  className="w-full px-3 py-2 rounded-lg border border-gray-600 bg-surface text-foreground 
                    focus:outline-none focus:ring-2 focus:ring-brand focus:border-transparent
                    [&>option]:bg-surface [&>option]:text-foreground"
                >
                  {DATA_TYPES.map((type) => (
                    <option key={type} value={type}>
                      {t(`admin.attributes.type_${type}`)}
                    </option>
                  ))}
                </select>
              </div>

              {/* Unit */}
              <div>
                <label className="block text-sm font-medium text-foreground mb-1">
                  {t('admin.attributes.unit')}
                </label>
                <input
                  type="text"
                  value={formData.unit || ''}
                  onChange={(e) => setFormData({ ...formData, unit: e.target.value })}
                  placeholder="e.g. kg, cm, GB"
                  className="w-full px-3 py-2 rounded-lg border border-gray-600 bg-surface text-foreground 
                    focus:outline-none focus:ring-2 focus:ring-brand focus:border-transparent"
                  maxLength={20}
                />
              </div>

              {/* Display Order */}
              <div>
                <label className="block text-sm font-medium text-foreground mb-1">
                  {t('admin.attributes.display_order')}
                </label>
                <input
                  type="number"
                  value={formData.displayOrder}
                  onChange={(e) =>
                    setFormData({ ...formData, displayOrder: parseInt(e.target.value) || 0 })
                  }
                  className="w-full px-3 py-2 rounded-lg border border-gray-600 bg-surface text-foreground 
                    focus:outline-none focus:ring-2 focus:ring-brand focus:border-transparent"
                  min={0}
                />
              </div>

              {/* Checkboxes */}
              <div className="flex items-center gap-6">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={formData.isRequired}
                    onChange={(e) => setFormData({ ...formData, isRequired: e.target.checked })}
                    className="w-4 h-4 rounded border-gray-600 text-brand focus:ring-brand"
                  />
                  <span className="text-sm text-foreground">{t('admin.attributes.is_required')}</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={formData.isVariant}
                    onChange={(e) => setFormData({ ...formData, isVariant: e.target.checked })}
                    className="w-4 h-4 rounded border-gray-600 text-brand focus:ring-brand"
                  />
                  <span className="text-sm text-foreground">{t('admin.attributes.is_variant')}</span>
                </label>
              </div>
            </div>

            {/* Description */}
            <div>
              <label className="block text-sm font-medium text-foreground mb-1">
                {t('admin.attributes.description')}
              </label>
              <textarea
                value={formData.description || ''}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="w-full px-3 py-2 rounded-lg border border-gray-600 bg-surface text-foreground 
                  focus:outline-none focus:ring-2 focus:ring-brand focus:border-transparent resize-y"
                rows={2}
                maxLength={500}
              />
            </div>

            {/* Allowed Values */}
            <div>
              <label className="block text-sm font-medium text-foreground mb-1">
                {t('admin.attributes.allowed_values')}
              </label>
              <textarea
                value={allowedValuesText}
                onChange={(e) => setAllowedValuesText(e.target.value)}
                placeholder={t('admin.attributes.allowed_values_hint')}
                className="w-full px-3 py-2 rounded-lg border border-gray-600 bg-surface text-foreground 
                  focus:outline-none focus:ring-2 focus:ring-brand focus:border-transparent resize-y font-mono text-sm"
                rows={4}
              />
              <p className="mt-1 text-xs text-foreground-muted">
                {t('admin.attributes.allowed_values_description')}
              </p>
            </div>

            <div className="flex gap-2">
              <button type="submit" className="btn btn-brand">
                {editingId ? t('admin.catalog.save') : t('admin.catalog.create')}
              </button>
              <button type="button" onClick={resetForm} className="btn btn-secondary">
                {t('cancel')}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Attributes Table */}
      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-border">
            <thead className="bg-surface-secondary">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-foreground-muted uppercase tracking-wider">
                  {t('admin.attributes.code')}
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-foreground-muted uppercase tracking-wider">
                  {t('admin.attributes.name')}
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-foreground-muted uppercase tracking-wider">
                  {t('admin.attributes.data_type')}
                </th>
                <th className="px-4 py-3 text-center text-xs font-medium text-foreground-muted uppercase tracking-wider">
                  {t('admin.attributes.flags')}
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-foreground-muted uppercase tracking-wider">
                  {t('admin.attributes.allowed_values')}
                </th>
                <th className="px-4 py-3 text-right text-xs font-medium text-foreground-muted uppercase tracking-wider">
                  {t('admin.catalog.actions')}
                </th>
              </tr>
            </thead>
            <tbody className="bg-surface divide-y divide-border">
              {paginatedAttributes.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-4 text-center text-foreground-muted">
                    {t('admin.attributes.no_attributes')}
                  </td>
                </tr>
              ) : (
                paginatedAttributes.map((attr) => (
                  <tr key={attr.id} className="hover:bg-surface-secondary/50 transition-colors">
                    <td className="px-4 py-4 whitespace-nowrap">
                      <code className="px-2 py-1 rounded bg-surface-secondary text-foreground font-mono text-sm">
                        {attr.code}
                      </code>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-foreground font-medium">
                      {attr.name}
                      {attr.unit && (
                        <span className="ml-1 text-foreground-muted text-sm">({attr.unit})</span>
                      )}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap">
                      <span
                        className={`px-2 py-1 rounded-full text-xs font-medium ${getDataTypeBadgeClass(attr.dataType)}`}
                      >
                        {t(`admin.attributes.type_${attr.dataType}`)}
                      </span>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-center">
                      <div className="flex justify-center gap-2">
                        {attr.isRequired && (
                          <span
                            className="px-2 py-0.5 rounded-full bg-red-500/20 text-red-400 text-xs"
                            title={t('admin.attributes.is_required')}
                          >
                            R
                          </span>
                        )}
                        {attr.isVariant && (
                          <span
                            className="px-2 py-0.5 rounded-full bg-brand/20 text-brand text-xs"
                            title={t('admin.attributes.is_variant')}
                          >
                            V
                          </span>
                        )}
                        {!attr.isActive && (
                          <span
                            className="px-2 py-0.5 rounded-full bg-gray-500/20 text-gray-400 text-xs"
                            title={t('admin.attributes.inactive')}
                          >
                            ✗
                          </span>
                        )}
                      </div>
                    </td>
                    <td className="px-4 py-4 text-foreground-muted text-sm max-w-[200px]">
                      {attr.allowedValues && attr.allowedValues.length > 0 ? (
                        <span className="truncate block" title={attr.allowedValues.join(', ')}>
                          {attr.allowedValues.slice(0, 3).join(', ')}
                          {attr.allowedValues.length > 3 && ` +${attr.allowedValues.length - 3}`}
                        </span>
                      ) : (
                        <span className="text-foreground-muted italic">{t('admin.attributes.any')}</span>
                      )}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-right text-sm">
                      <button
                        onClick={() => startEdit(attr)}
                        className="text-brand hover:text-brand-light mr-3 transition-colors"
                      >
                        {t('admin.catalog.edit')}
                      </button>
                      <button
                        onClick={() => handleDelete(attr.id)}
                        className="text-red-500 hover:text-red-400 transition-colors"
                      >
                        {t('admin.catalog.delete')}
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between px-4 py-4 border-t border-border bg-surface-secondary/30">
            <div className="text-sm text-foreground-muted">
              {t('admin.catalog.showing')} {(currentPage - 1) * ITEMS_PER_PAGE + 1}-
              {Math.min(currentPage * ITEMS_PER_PAGE, attributes.length)} {t('admin.catalog.of')}{' '}
              {attributes.length}
            </div>
            <div className="flex gap-2">
              <button
                onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                disabled={currentPage === 1}
                className="px-3 py-1 rounded border border-border text-foreground-muted hover:bg-surface-secondary 
                  disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {t('admin.catalog.prev')}
              </button>
              {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => (
                <button
                  key={page}
                  onClick={() => setCurrentPage(page)}
                  className={`px-3 py-1 rounded border transition-colors ${
                    currentPage === page
                      ? 'bg-brand text-white border-brand'
                      : 'border-border text-foreground-muted hover:bg-surface-secondary'
                  }`}
                >
                  {page}
                </button>
              ))}
              <button
                onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                disabled={currentPage === totalPages}
                className="px-3 py-1 rounded border border-border text-foreground-muted hover:bg-surface-secondary 
                  disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {t('admin.catalog.next')}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
