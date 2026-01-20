import { useEffect, useState, useRef } from 'react'
import type { Resolver } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useProfileStore } from '../../store/profileStore'
import { userApi } from '../../api/userApi'
import ImageCropper from '../ImageCropper'
import { useForm } from 'react-hook-form'
import { yupResolver } from '@hookform/resolvers/yup'
import * as yup from 'yup'

type InfoFormValues = { name: string; surname: string; username: string }
type PhoneFormValues = { phone: string }
type EmailFormValues = { email: string }

export default function ProfileForm() {
  const { t } = useTranslation()
  const { profile, fetchProfile, updateInfo, updatePhone, updateEmail } = useProfileStore()
  const [success, setSuccess] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [imageSrc, setImageSrc] = useState<string | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  // build validation schema using localized messages
  const infoSchema = yup.object({
    name: yup.string().required(t('validation.required')).min(2, t('validation.min_2')),
    surname: yup.string().required(t('validation.required')).min(2, t('validation.min_2')),
    username: yup.string().required(t('validation.required')).min(2, t('validation.min_2')),
  })

  const phoneSchema = yup.object({
    phone: yup
      .string()
      .required(t('validation.required'))
      .matches(/^[+]?[0-9]{7,15}$/, t('validation.phone')),
  })

  const emailSchema = yup.object({
    email: yup.string().required(t('validation.required')).email(t('validation.email')),
  })

  const infoDefaults: InfoFormValues = {
    name: typeof profile?.name === 'string' ? profile!.name : '',
    surname: typeof profile?.surname === 'string' ? profile!.surname : '',
    username: (profile as {username?: string})?.username || '',
  }
  const phoneDefaults: PhoneFormValues = { phone: typeof profile?.phoneNumber === 'string' ? profile!.phoneNumber : '' }
  const emailDefaults: EmailFormValues = { email: (profile as {email?: string})?.email || '' }

  const {
    register: registerInfo,
    handleSubmit: handleSubmitInfo,
    reset: resetInfo,
    formState: { errors: infoErrors, isSubmitting: infoSubmitting },
  } = useForm<InfoFormValues>({ resolver: yupResolver(infoSchema) as unknown as Resolver<InfoFormValues>, defaultValues: infoDefaults })

  const {
    register: registerPhone,
    handleSubmit: handleSubmitPhone,
    reset: resetPhone,
    formState: { errors: phoneErrors, isSubmitting: phoneSubmitting },
  } = useForm<PhoneFormValues>({ resolver: yupResolver(phoneSchema) as unknown as Resolver<PhoneFormValues>, defaultValues: phoneDefaults })

  const {
    register: registerEmail,
    handleSubmit: handleSubmitEmail,
    reset: resetEmail,
    formState: { errors: emailErrors, isSubmitting: emailSubmitting },
  } = useForm<EmailFormValues>({ resolver: yupResolver(emailSchema) as unknown as Resolver<EmailFormValues>, defaultValues: emailDefaults })

  // Always fetch fresh profile when component mounts
  useEffect(() => {
    fetchProfile().catch(() => setError(t('errors.fetch_failed')))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // When profile becomes available (or changes), ensure form values are in sync.
  // We'll call reset as a fallback for already-mounted forms, but also
  // force a remount via formKey so defaultValues are applied on mount.
  useEffect(() => {
    if (!profile) return
    resetInfo(infoDefaults)
    resetPhone(phoneDefaults)
    resetEmail(emailDefaults)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [profile])

  const onSubmitInfo = async (data: InfoFormValues) => {
    setError(null)
    setSuccess(null)
    try {
      const updated = await updateInfo({ name: data.name, surname: data.surname, username: data.username })
      if (updated) resetInfo({ ...data })
      setSuccess(t('settings.profile.save_success'))
      setTimeout(() => setSuccess(null), 3000)
    } catch (err) {
      let msg = t('errors.save_failed')
      if (err instanceof Error) msg = err.message
      else if (typeof err === 'string') msg = err
      setError(msg)
    }
  }

  const onSubmitPhone = async (data: PhoneFormValues) => {
    setError(null)
    setSuccess(null)
    try {
      const updated = await updatePhone(data.phone)
      if (updated) resetPhone({ ...data })
      setSuccess(t('settings.profile.save_success'))
      setTimeout(() => setSuccess(null), 3000)
    } catch (err) {
      let msg = t('errors.save_failed')
      if (err instanceof Error) msg = err.message
      else if (typeof err === 'string') msg = err
      setError(msg)
    }
  }

  const onSubmitEmail = async (data: EmailFormValues) => {
    setError(null)
    setSuccess(null)
    try {
      const updated = await updateEmail(data.email)
      if (updated) resetEmail({ ...data })
      setSuccess(t('settings.profile.save_success'))
      setTimeout(() => setSuccess(null), 3000)
    } catch (err) {
      let msg = t('errors.save_failed')
      if (err instanceof Error) msg = err.message
      else if (typeof err === 'string') msg = err
      setError(msg)
    }
  }

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    // Validate file type
    if (!file.type.startsWith('image/')) {
      setError('Please select an image file')
      return
    }

    // Create preview URL
    const reader = new FileReader()
    reader.onload = () => {
      setImageSrc(reader.result as string)
    }
    reader.readAsDataURL(file)
  }

  const handleCropComplete = async (croppedBlob: Blob) => {
    setError(null)
    setSuccess(null)
    setIsUploading(true)

    try {
      // Convert blob to file
      const file = new File([croppedBlob], 'profile-picture.jpg', { type: 'image/jpeg' })
      
      // Upload
      await userApi.uploadProfilePicture(file)
      
      // Refresh profile to get new avatar URL
      await fetchProfile()
      
      // Refresh auth token to update avatarUrl in header
      try {
        const { authApi } = await import('../../api/authApi')
        const { useAuthStore } = await import('../../store/authStore')
        const tokens = await authApi.refreshTokens()
        const setAuth = useAuthStore.getState().setAuth
        if (setAuth) setAuth(tokens.accessToken || '', tokens.refreshToken || '')
      } catch {
        // token refresh optional; ignore
      }
      
      setSuccess(t('profile.picture_updated'))
      setTimeout(() => setSuccess(null), 3000)
    } catch (err) {
      let msg = t('errors.save_failed')
      if (err instanceof Error) msg = err.message
      else if (typeof err === 'string') msg = err
      setError(msg)
    } finally {
      setIsUploading(false)
      setImageSrc(null)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  const handleDeletePicture = async () => {
    if (!window.confirm(t('profile.delete_picture') + '?')) return

    setError(null)
    setSuccess(null)
    setIsDeleting(true)

    try {
      await userApi.deleteProfilePicture()
      await fetchProfile()
      
      // Refresh auth token to update avatarUrl in header
      try {
        const { authApi } = await import('../../api/authApi')
        const { useAuthStore } = await import('../../store/authStore')
        const tokens = await authApi.refreshTokens()
        const setAuth = useAuthStore.getState().setAuth
        if (setAuth) setAuth(tokens.accessToken || '', tokens.refreshToken || '')
      } catch {
        // token refresh optional; ignore
      }
      
      setSuccess(t('profile.picture_deleted'))
      setTimeout(() => setSuccess(null), 3000)
    } catch (err) {
      let msg = t('errors.save_failed')
      if (err instanceof Error) msg = err.message
      else if (typeof err === 'string') msg = err
      setError(msg)
    } finally {
      setIsDeleting(false)
    }
  }

  if (!profile && !error) return <div className="text-sm text-foreground-muted">{t('auth.loading')}</div>

  const profileIdOrEmail = profile ? ((profile as { id?: string; email?: string }).id ?? (profile as { id?: string; email?: string }).email ?? 'me') : 'empty'
  const infoFormKey = `profile-info-${profileIdOrEmail}`
  const phoneFormKey = `profile-phone-${profileIdOrEmail}`
  const emailFormKey = `profile-email-${profileIdOrEmail}`

  return (
    <div className="space-y-8">
      {/* Profile Picture Section */}
      <div className="space-y-4">
        <h3 className="text-lg font-semibold text-foreground dark:text-white">{t('profile.picture')}</h3>
        {error && <div className="text-sm text-red-600">{error}</div>}
        {success && <div className="text-sm text-green-600">{success}</div>}
        
        <div className="flex items-center gap-4">
          {/* Avatar preview */}
          <div className="h-24 w-24 rounded-full overflow-hidden bg-violet-200 dark:bg-violet-600 flex items-center justify-center">
            {profile?.avatarUrl ? (
              <img 
                src={profile.avatarUrl} 
                alt="Profile" 
                className="h-full w-full object-cover" 
              />
            ) : (
              <span className="text-2xl font-semibold text-white">
                {profile?.name?.[0] || profile?.email?.[0] || '?'}
              </span>
            )}
          </div>

          {/* Upload/Delete buttons */}
          <div className="flex flex-col gap-2">
            <input
              ref={fileInputRef}
              type="file"
              accept="image/*"
              onChange={handleFileSelect}
              className="hidden"
            />
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              disabled={isUploading || isDeleting}
              className="px-4 py-2 bg-brand text-white rounded-md hover:bg-brand/90 disabled:opacity-50 text-sm"
            >
              {profile?.avatarUrl ? t('profile.change_picture') : t('profile.upload_picture')}
            </button>
            {profile?.avatarUrl && (
              <button
                type="button"
                onClick={handleDeletePicture}
                disabled={isUploading || isDeleting}
                className="px-4 py-2 border border-red-500 text-red-500 rounded-md hover:bg-red-50 dark:hover:bg-red-900/20 disabled:opacity-50 text-sm"
              >
                {isDeleting ? t('profile.deleting') : t('profile.delete_picture')}
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Image Cropper Modal */}
      {imageSrc && (
        <ImageCropper
          imageSrc={imageSrc}
          onCropComplete={handleCropComplete}
          onCancel={() => {
            setImageSrc(null)
            if (fileInputRef.current) fileInputRef.current.value = ''
          }}
        />
      )}

      <form key={infoFormKey} onSubmit={handleSubmitInfo(onSubmitInfo)} className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-1">{t('settings.profile.name')}</label>
          <input {...registerInfo('name')} className="w-full px-3 py-2 border rounded-md" />
          {infoErrors.name && <div className="text-sm text-red-600">{infoErrors.name.message}</div>}
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">{t('settings.profile.surname')}</label>
          <input {...registerInfo('surname')} className="w-full px-3 py-2 border rounded-md" />
          {infoErrors.surname && <div className="text-sm text-red-600">{infoErrors.surname.message}</div>}
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">{t('settings.profile.username')}</label>
          <input {...registerInfo('username')} className="w-full px-3 py-2 border rounded-md" />
          {infoErrors.username && <div className="text-sm text-red-600">{infoErrors.username.message}</div>}
        </div>
        <div className="flex items-center gap-2">
          <button type="submit" disabled={infoSubmitting} className="px-4 py-2 bg-brand text-white rounded-md disabled:opacity-60">
            {infoSubmitting ? t('saving') : t('settings.profile.save')}
          </button>
        </div>
      </form>

      <form key={phoneFormKey} onSubmit={handleSubmitPhone(onSubmitPhone)} className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-1">{t('settings.profile.phone')}</label>
          <input {...registerPhone('phone')} className="w-full px-3 py-2 border rounded-md" />
          {phoneErrors.phone && <div className="text-sm text-red-600">{phoneErrors.phone.message}</div>}
        </div>
        <div className="flex items-center gap-2">
          <button type="submit" disabled={phoneSubmitting} className="px-4 py-2 bg-brand text-white rounded-md disabled:opacity-60">
            {phoneSubmitting ? t('saving') : t('settings.profile.save')}
          </button>
        </div>
      </form>

      <form key={emailFormKey} onSubmit={handleSubmitEmail(onSubmitEmail)} className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-1">{t('settings.profile.email')}</label>
          <input {...registerEmail('email')} className="w-full px-3 py-2 border rounded-md" />
          {emailErrors.email && <div className="text-sm text-red-600">{emailErrors.email.message}</div>}
        </div>
        <div className="flex items-center gap-2">
          <button type="submit" disabled={emailSubmitting} className="px-4 py-2 bg-brand text-white rounded-md disabled:opacity-60">
            {emailSubmitting ? t('saving') : t('settings.profile.save')}
          </button>
        </div>
      </form>
    </div>
    
  )
}
