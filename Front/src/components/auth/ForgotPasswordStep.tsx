import { useForm } from 'react-hook-form'
import { yupResolver } from '@hookform/resolvers/yup'
import { forgotPasswordFormSchema, type ForgotPasswordFormValues } from '../../validation/authSchemas'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

interface ForgotPasswordStepProps {
  onBack: () => void
  onSubmit: (email: string) => void
}
type FormData = ForgotPasswordFormValues

export function ForgotPasswordStep({ onBack, onSubmit }: ForgotPasswordStepProps) {
  const [isLoading, setIsLoading] = useState(false)
  const [success, setSuccess] = useState(false)
  const { t } = useTranslation()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({
    resolver: yupResolver(forgotPasswordFormSchema),
  })

  const onFormSubmit = async (data: FormData) => {
    setIsLoading(true)
    try {
      await onSubmit(data.email)
      setSuccess(true)
    } catch (error) {
      console.error(error)
    } finally {
      setIsLoading(false)
    }
  }

  if (success) {
    return (
      <div className="space-y-6 text-center">
        <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-green-500/20">
          <svg className="h-8 w-8 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
          </svg>
        </div>
        <div>
          <h2 className="text-2xl font-bold text-foreground">{t('forgot.success_title')}</h2>
          <p className="mt-2 text-sm text-foreground-muted">{t('forgot.success_text')}</p>
        </div>
        <button onClick={onBack} className="btn-primary w-full">{t('forgot.back_to_login')}</button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <button onClick={onBack} className="mb-4 flex items-center gap-1 text-sm text-foreground-muted hover:text-foreground">
          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
          {t('auth.back')}
        </button>
        <h2 className="text-2xl font-bold text-foreground">{t('forgot.title')}</h2>
        <p className="mt-2 text-sm text-foreground-muted">{t('forgot.instructions')}</p>
      </div>

  <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-4">
        <div>
          <label htmlFor="email" className="mb-1 block text-sm text-foreground-muted">{t('common.email')}</label>
          <input
            {...register('email')}
            id="email"
            type="email"
            placeholder="your@email.com"
            autoComplete="email"
            className="w-full rounded-lg border border-foreground/20 bg-transparent px-4 py-3 text-foreground outline-none transition-colors focus:border-brand"
          />
          {errors.email && <p className="mt-1 text-sm text-red-500">{errors.email.message}</p>}
        </div>

        <button type="submit" disabled={isLoading} className="btn-primary w-full disabled:opacity-50">
          {isLoading ? t('forgot.sending') : t('forgot.send')}
        </button>
      </form>
    </div>
  )
}
