import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { myStoreApi, type CreateStoreRequest } from '../../api/storeApi'
import { ErrorAlert } from '../../components/ui/ErrorAlert'

interface FormErrors {
  name?: string
  description?: string
}

export default function CreateStore() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [formErrors, setFormErrors] = useState<FormErrors>({})

  const [formData, setFormData] = useState<CreateStoreRequest>({
    name: '',
    description: ''
  })

  const validateForm = (): boolean => {
    const errors: FormErrors = {}

    // Name validation
    if (!formData.name.trim()) {
      errors.name = t('validation.required')
    } else if (formData.name.trim().length < 2) {
      errors.name = t('validation.min_2')
    } else if (formData.name.length > 200) {
      errors.name = t('validation.max_200')
    }

    // Description validation
    if (formData.description && formData.description.length > 2000) {
      errors.description = t('validation.max_2000')
    }

    setFormErrors(errors)
    return Object.keys(errors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!validateForm()) {
      return
    }

    setLoading(true)
    setError(null)

    try {
      const response = await myStoreApi.create(formData)
      if (response.isSuccess) {
        navigate('/cabinet/my-store')
      } else {
        setError(response.message || t('errors.save_failed'))
      }
    } catch {
      setError(t('errors.save_failed'))
    } finally {
      setLoading(false)
    }
  }

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value
    setFormData({ ...formData, name: value })
    // Clear error when user starts typing
    if (formErrors.name) {
      setFormErrors({ ...formErrors, name: undefined })
    }
  }

  const handleDescriptionChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const value = e.target.value || null
    setFormData({ ...formData, description: value })
    // Clear error when user starts typing
    if (formErrors.description) {
      setFormErrors({ ...formErrors, description: undefined })
    }
  }

  return (
    <div className="max-w-2xl mx-auto p-6">
      <h1 className="text-2xl font-bold text-foreground mb-6">{t('store.create.title')}</h1>

      {error && (
        <ErrorAlert className="mb-6">{error}</ErrorAlert>
      )}

      <div className="card p-6">
        <p className="text-foreground-muted mb-6">{t('store.create.description')}</p>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-foreground mb-1">
              {t('store.create.name')} *
            </label>
            <input
              type="text"
              value={formData.name}
              onChange={handleNameChange}
              className={`w-full px-3 py-2 rounded-lg border bg-surface text-foreground 
                focus:outline-none focus:ring-2 focus:ring-brand focus:border-transparent
                ${formErrors.name ? 'border-error' : 'border-border'}`}
              placeholder={t('store.create.name_placeholder')}
              maxLength={200}
            />
            {formErrors.name && (
              <p className="mt-1 text-sm text-error">{formErrors.name}</p>
            )}
            <p className="mt-1 text-xs text-foreground-muted">{formData.name.length}/200</p>
          </div>

          <div>
            <label className="block text-sm font-medium text-foreground mb-1">
              {t('store.create.store_description')}
            </label>
            <textarea
              value={formData.description || ''}
              onChange={handleDescriptionChange}
              className={`w-full px-3 py-2 rounded-lg border bg-surface text-foreground 
                focus:outline-none focus:ring-2 focus:ring-brand focus:border-transparent resize-y
                ${formErrors.description ? 'border-error' : 'border-border'}`}
              rows={4}
              placeholder={t('store.create.description_placeholder')}
              maxLength={2000}
            />
            {formErrors.description && (
              <p className="mt-1 text-sm text-error">{formErrors.description}</p>
            )}
            <p className="mt-1 text-xs text-foreground-muted">{(formData.description || '').length}/2000</p>
          </div>

          <div className="flex gap-3 pt-4">
            <button
              type="submit"
              disabled={loading}
              className="btn btn-brand disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? t('store.create.creating') : t('store.create.button')}
            </button>
            <button
              type="button"
              onClick={() => navigate('/cabinet')}
              className="btn btn-secondary"
            >
              {t('cancel')}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
