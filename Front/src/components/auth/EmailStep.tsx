import { useForm } from 'react-hook-form'
import { yupResolver } from '@hookform/resolvers/yup'
import { emailFormSchema, type EmailFormValues } from '../../validation/authSchemas'
import { useTranslation } from 'react-i18next'

interface EmailStepProps {
  onNext: (email: string) => void
  onForgotPassword: () => void
  isLoading: boolean
}

type FormData = EmailFormValues

export function EmailStep({ onNext, onForgotPassword, isLoading }: EmailStepProps) {
  const { t } = useTranslation()
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({
    resolver: yupResolver(emailFormSchema),
  })

  const onSubmit = async (data: FormData) => {
    // Тут має бути виклик authApi.checkEmail(data.email)
    // Для демо - симулюємо перевірку
    onNext(data.email)
  }

  return (
    <div className="space-y-6">
      <div className="text-center">
        <h2 className="text-2xl font-bold text-foreground">{t('auth.email_step_heading')}</h2>
        <p className="mt-2 text-sm text-foreground-muted">{t('auth.enter_email')}</p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label htmlFor="email" className="mb-1 block text-sm text-foreground-muted">
            Email
          </label>
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
          {isLoading ? t('auth.loading') : t('auth.continue')}
        </button>

        
      </form>

      <div className="text-center">
        <button type="button" onClick={onForgotPassword} className="text-sm text-brand hover:text-brand-light">
          {t('auth.forgot_password')}
        </button>
      </div>
    </div>
  )
}
