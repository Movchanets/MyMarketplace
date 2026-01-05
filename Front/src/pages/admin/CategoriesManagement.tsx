import { useState, useEffect, useMemo, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { categoriesApi, type CategoryDto, type CreateCategoryRequest, type UpdateCategoryRequest } from '../../api/catalogApi'

const ITEMS_PER_PAGE = 10

interface FormErrors {
  name?: string
  description?: string
}

export default function CategoriesManagement() {
  const { t } = useTranslation()
  const [categories, setCategories] = useState<CategoryDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [showCreateForm, setShowCreateForm] = useState(false)
  const [currentPage, setCurrentPage] = useState(1)
  const [formErrors, setFormErrors] = useState<FormErrors>({})

  // Form state
  const [formData, setFormData] = useState<CreateCategoryRequest>({
    name: '',
    description: '',
    parentCategoryId: null
  })

  // Create a map for quick parent lookup
  const categoryMap = useMemo(() => {
    const map = new Map<string, CategoryDto>()
    categories.forEach(cat => map.set(cat.id, cat))
    return map
  }, [categories])

  // Pagination
  const totalPages = Math.ceil(categories.length / ITEMS_PER_PAGE)
  const paginatedCategories = useMemo(() => {
    const start = (currentPage - 1) * ITEMS_PER_PAGE
    return categories.slice(start, start + ITEMS_PER_PAGE)
  }, [categories, currentPage])

  const fetchCategories = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const response = await categoriesApi.getAll()
      if (response.isSuccess) {
        setCategories(response.payload || [])
        setCurrentPage(1)
      } else {
        setError(response.message || t('errors.fetch_failed'))
      }
    } catch {
      setError(t('errors.fetch_failed'))
    } finally {
      setLoading(false)
    }
  }, [t])

  useEffect(() => {
    fetchCategories()
  }, [fetchCategories])

  const resetForm = () => {
    setFormData({ name: '', description: '', parentCategoryId: null })
    setEditingId(null)
    setShowCreateForm(false)
    setFormErrors({})
  }

  const validateForm = (): boolean => {
    const errors: FormErrors = {}

    if (!formData.name.trim()) {
      errors.name = t('validation.required')
    } else if (formData.name.trim().length < 2) {
      errors.name = t('validation.min_2')
    } else if (formData.name.length > 200) {
      errors.name = t('validation.max_200')
    }

    if (formData.description && formData.description.length > 2000) {
      errors.description = t('validation.max_2000')
    }

    setFormErrors(errors)
    return Object.keys(errors).length === 0
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!validateForm()) return
    try {
      const response = await categoriesApi.create(formData)
      if (response.isSuccess) {
        await fetchCategories()
        resetForm()
      } else {
        setError(response.message || t('errors.save_failed'))
      }
    } catch {
      setError(t('errors.save_failed'))
    }
  }

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingId) return
    if (!validateForm()) return
    try {
      const updateData: UpdateCategoryRequest = {
        name: formData.name,
        description: formData.description,
        parentCategoryId: formData.parentCategoryId
      }
      const response = await categoriesApi.update(editingId, updateData)
      if (response.isSuccess) {
        await fetchCategories()
        resetForm()
      } else {
        setError(response.message || t('errors.save_failed'))
      }
    } catch {
      setError(t('errors.save_failed'))
    }
  }

  const handleDelete = async (id: string) => {
    if (!window.confirm(t('admin.catalog.confirm_delete'))) return
    try {
      const response = await categoriesApi.delete(id)
      if (response.isSuccess) {
        await fetchCategories()
      } else {
        setError(response.message || t('errors.save_failed'))
      }
    } catch {
      setError(t('errors.save_failed'))
    }
  }

  const startEdit = (category: CategoryDto) => {
    setFormData({
      name: category.name,
      description: category.description || '',
      parentCategoryId: category.parentCategoryId
    })
    setEditingId(category.id)
    setShowCreateForm(true)
  }

  const getParentName = (parentId: string | null): string => {
    if (!parentId) return '-'
    const parent = categoryMap.get(parentId)
    return parent?.name || '-'
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
        <h2 className="text-2xl font-bold text-text">{t('admin.catalog.categories')}</h2>
        <button
          onClick={() => { resetForm(); setShowCreateForm(true) }}
          className="btn btn-brand"
        >
          {t('admin.catalog.add_category')}
        </button>
      </div>

      {error && (
        <div className="bg-red-100 dark:bg-red-900/30 border border-red-400 dark:border-red-700 text-red-700 dark:text-red-300 px-4 py-3 rounded">
          {error}
        </div>
      )}

      {/* Create/Edit Form */}
      {showCreateForm && (
        <div className="card p-6">
          <h3 className="text-lg font-semibold text-text mb-4">
            {editingId ? t('admin.catalog.edit_category') : t('admin.catalog.add_category')}
          </h3>
          <form onSubmit={editingId ? handleUpdate : handleCreate} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-text mb-1">
                {t('admin.catalog.name')} *
              </label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => {
                  setFormData({ ...formData, name: e.target.value })
                  if (formErrors.name) setFormErrors({ ...formErrors, name: undefined })
                }}
                className={`w-full px-3 py-2 rounded-lg border bg-surface text-text 
                  focus:outline-none focus:ring-2 focus:ring-brand focus:border-transparent
                  ${formErrors.name ? 'border-red-500' : 'border-gray-600'}`}
                maxLength={200}
              />
              {formErrors.name && (
                <p className="mt-1 text-sm text-red-500">{formErrors.name}</p>
              )}
              <p className="mt-1 text-xs text-text-muted">{formData.name.length}/200</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-text mb-1">
                {t('admin.catalog.description')}
              </label>
              <textarea
                value={formData.description || ''}
                onChange={(e) => {
                  setFormData({ ...formData, description: e.target.value || null })
                  if (formErrors.description) setFormErrors({ ...formErrors, description: undefined })
                }}
                className={`w-full px-3 py-2 rounded-lg border bg-surface text-text 
                  focus:outline-none focus:ring-2 focus:ring-brand focus:border-transparent resize-y
                  ${formErrors.description ? 'border-red-500' : 'border-gray-600'}`}
                rows={3}
                maxLength={2000}
              />
              {formErrors.description && (
                <p className="mt-1 text-sm text-red-500">{formErrors.description}</p>
              )}
              <p className="mt-1 text-xs text-text-muted">{(formData.description || '').length}/2000</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-text mb-1">
                {t('admin.catalog.parent_category')}
              </label>
              <select
                value={formData.parentCategoryId || ''}
                onChange={(e) => setFormData({ ...formData, parentCategoryId: e.target.value || null })}
                className="w-full px-3 py-2 rounded-lg border border-gray-600 bg-surface text-text 
                  focus:outline-none focus:ring-2 focus:ring-brand focus:border-transparent
                  [&>option]:bg-surface [&>option]:text-text"
              >
                <option value="">{t('admin.catalog.no_parent')}</option>
                {categories
                  .filter(c => c.id !== editingId)
                  .map(category => (
                    <option key={category.id} value={category.id}>
                      {category.name}
                    </option>
                  ))}
              </select>
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

      {/* Categories Table */}
      <div className="card overflow-hidden">
        <table className="min-w-full divide-y divide-border">
          <thead className="bg-surface-secondary">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-text-muted uppercase tracking-wider">
                {t('admin.catalog.name')}
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-text-muted uppercase tracking-wider">
                {t('admin.catalog.slug')}
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-text-muted uppercase tracking-wider">
                {t('admin.catalog.parent_category')}
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-text-muted uppercase tracking-wider">
                {t('admin.catalog.description')}
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-text-muted uppercase tracking-wider">
                {t('admin.catalog.actions')}
              </th>
            </tr>
          </thead>
          <tbody className="bg-surface divide-y divide-border">
            {paginatedCategories.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-6 py-4 text-center text-text-muted">
                  {t('admin.catalog.no_categories')}
                </td>
              </tr>
            ) : (
              paginatedCategories.map(category => (
                <tr key={category.id} className="hover:bg-surface-secondary/50 transition-colors">
                  <td className="px-6 py-4 whitespace-nowrap text-text font-medium">
                    {category.name}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-text-muted text-sm">
                    {category.slug}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-text-muted text-sm">
                    {category.parentCategoryId ? (
                      <span className="px-2 py-1 rounded-full bg-brand/10 text-brand text-xs font-medium">
                        {getParentName(category.parentCategoryId)}
                      </span>
                    ) : (
                      <span className="text-text-muted">-</span>
                    )}
                  </td>
                  <td className="px-6 py-4 text-text-muted text-sm max-w-xs truncate">
                    {category.description || '-'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm">
                    <button
                      onClick={() => startEdit(category)}
                      className="text-brand hover:text-brand-light mr-3 transition-colors"
                    >
                      {t('admin.catalog.edit')}
                    </button>
                    <button
                      onClick={() => handleDelete(category.id)}
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

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between px-6 py-4 border-t border-border bg-surface-secondary/30">
            <div className="text-sm text-text-muted">
              {t('admin.catalog.showing')} {(currentPage - 1) * ITEMS_PER_PAGE + 1}-
              {Math.min(currentPage * ITEMS_PER_PAGE, categories.length)} {t('admin.catalog.of')} {categories.length}
            </div>
            <div className="flex gap-2">
              <button
                onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                disabled={currentPage === 1}
                className="px-3 py-1 rounded border border-border text-text-muted hover:bg-surface-secondary 
                  disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {t('admin.catalog.prev')}
              </button>
              {Array.from({ length: totalPages }, (_, i) => i + 1).map(page => (
                <button
                  key={page}
                  onClick={() => setCurrentPage(page)}
                  className={`px-3 py-1 rounded border transition-colors ${
                    currentPage === page
                      ? 'bg-brand text-white border-brand'
                      : 'border-border text-text-muted hover:bg-surface-secondary'
                  }`}
                >
                  {page}
                </button>
              ))}
              <button
                onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                disabled={currentPage === totalPages}
                className="px-3 py-1 rounded border border-border text-text-muted hover:bg-surface-secondary 
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
