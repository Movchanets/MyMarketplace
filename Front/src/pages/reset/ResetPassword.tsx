import { useSearchParams, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { yupResolver } from '@hookform/resolvers/yup'
import * as yup from 'yup'
import { authApi } from '../../api/authApi'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

type FormData = {
  newPassword: string
  confirmPassword: string
}

export default function ResetPassword() {
  const { t } = useTranslation()
  const schema = yup.object({
    newPassword: yup.string().min(6).required(t('validation.required')),
    confirmPassword: yup.string().oneOf([yup.ref('newPassword')], t('validation.passwords_mismatch')).required(t('validation.required')),
  })
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const email = searchParams.get('email') ?? ''
  const token = searchParams.get('token') ?? ''
  const [success, setSuccess] = useState(false)

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormData>({ resolver: yupResolver(schema) })

  const onSubmit = async (data: FormData) => {
    try {
      await authApi.resetPassword({ email, token, newPassword: data.newPassword })
      setSuccess(true)
    } catch (err) {
      console.error(err)
      alert(t('reset.reset_failed'))
    }
  }

  return (
    <div className="flex min-h-[calc(100vh-4rem)] items-center justify-center p-4">
      <div className="w-full max-w-md rounded-2xl bg-surface p-8 shadow-2xl">
        {!success ? (
          <>
            <h2 className="text-2xl font-bold text-foreground">{t('reset.title')}</h2>
            <p className="mt-2 text-sm text-foreground-muted">{t('reset.enter_new_password', { email })}</p>

            <form className="mt-6 space-y-4" onSubmit={handleSubmit(onSubmit)}>
              <div>
                <label className="mb-1 block text-sm text-foreground-muted">{t('reset.new_password_label')}</label>
                <input {...register('newPassword')} type="password" autoComplete="new-password" className="w-full rounded-lg border border-foreground/20 bg-transparent px-4 py-3 text-foreground" />
                 {errors.newPassword && <p className="mt-1 text-sm text-error">{errors.newPassword.message}</p>}
              </div>
              <div>
                <label className="mb-1 block text-sm text-foreground-muted">{t('reset.confirm_password_label')}</label>
                <input {...register('confirmPassword')} type="password" autoComplete="new-password" className="w-full rounded-lg border border-foreground/20 bg-transparent px-4 py-3 text-foreground" />
                 {errors.confirmPassword && <p className="mt-1 text-sm text-error">{errors.confirmPassword.message}</p>}
              </div>

              <button type="submit" disabled={isSubmitting} className="btn-primary w-full">
                {isSubmitting ? t('reset.saving') : t('reset.save')}
              </button>
            </form>
          </>
        ) : (
          <div className="text-center">
             <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-success-light/20">
               <svg className="h-8 w-8 text-success" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-foreground mt-4">{t('reset.success_title')}</h2>
            <p className="mt-2 text-sm text-foreground-muted">{t('reset.success_text')}</p>
            <div className="mt-6">
              <button onClick={() => navigate('/auth')} className="btn-primary w-full">{t('reset.back_to_login')}</button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
