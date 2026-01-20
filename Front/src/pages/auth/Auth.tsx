import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { EmailStep } from '../../components/auth/EmailStep'
import { LoginStep } from '../../components/auth/LoginStep'
import { RegisterStep } from '../../components/auth/RegisterStep'
import { ForgotPasswordStep } from '../../components/auth/ForgotPasswordStep'
import { authApi } from '../../api/authApi'
import TurnstileWidget from '../../components/ui/TurnstileWidget'
import { useAuthStore } from '../../store/authStore'

type Step = 'email' | 'login' | 'register' | 'forgot'

export default function Auth() {
  const [step, setStep] = useState<Step>('email')
  const [email, setEmail] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const { t } = useTranslation()
  const setAuth = useAuthStore((state) => state.setAuth)
  const navigate = useNavigate()
  const [turnstileToken, setTurnstileToken] = useState<string | null>(null)

  const handleEmailNext = async (enteredEmail: string) => {
    setEmail(enteredEmail)
    setIsLoading(true)
    setError(null)

    try {
      const result = await authApi.checkEmail(enteredEmail, turnstileToken ?? undefined)
      setStep(result.exists ? 'login' : 'register')
    } catch {
      setError(t('validation.server_error'))
    } finally {
      setIsLoading(false)
    }
  }

  const handleLogin = async (password: string) => {
    setIsLoading(true)
    setError(null)

    try {
  const result = await authApi.login({ email, password, turnstileToken: turnstileToken ?? undefined })
      setAuth(result.accessToken, result.refreshToken)
      navigate('/')
    } catch {
      setError(t('auth.invalid_password'))
    } finally {
      setIsLoading(false)
    }
  }

  const handleRegister = async (name: string, surname: string, password: string, confirmPassword: string) => {
    setIsLoading(true)
    setError(null)

    try {
  const result = await authApi.register({ email, name, surname, password, confirmPassword, turnstileToken: turnstileToken ?? undefined })
      setAuth(result.accessToken, result.refreshToken)
      navigate('/')
    } catch {
      setError(t('auth.register_error'))
    } finally {
      setIsLoading(false)
    }
  }

  const handleGoogleLogin = async () => {
    setError(null)
    try {
      alert(t('auth.google_oauth_notice'))
    } catch {
      setError(t('auth.google_error'))
    }
  }


  const handleBack = () => {
    setStep('email')
    setError(null)
  }

  const handleRequestPasswordReset = async (emailToReset: string) => {
    setIsLoading(true)
    setError(null)
    try {
      await authApi.requestPasswordReset({ email: emailToReset, turnstileToken: turnstileToken ?? undefined })
      setStep('email')
    } catch {
      setError(t('auth.reset_error'))
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="flex min-h-[calc(100vh-4rem)] items-center justify-center p-4">
      <div className="w-full max-w-md rounded-2xl bg-surface p-8 shadow-2xl">
        {error && (
          <div className="mb-4 rounded-lg border border-red-500/20 bg-red-500/10 p-3 text-sm text-red-500">
            {error}
          </div>
        )}

        {step === 'email' && (
          <EmailStep
            onNext={handleEmailNext}
            onForgotPassword={() => setStep('forgot')}
            onGoogleLogin={handleGoogleLogin}
            isLoading={isLoading}
          />
        )}

        {step === 'login' && (
          <LoginStep email={email} onBack={handleBack} onSubmit={handleLogin} isLoading={isLoading} />
        )}

        {step === 'register' && (
          <RegisterStep
            email={email}
            onBack={handleBack}
            onSubmit={handleRegister}
            isLoading={isLoading}
          />
        )}

  {step === 'forgot' && <ForgotPasswordStep onBack={handleBack} onSubmit={handleRequestPasswordReset} />}
  {(step === 'login' || step === 'register' || step === 'forgot') && (
    <div className="mt-4">
      <TurnstileWidget onVerify={(t: string) => setTurnstileToken(t)} />
    </div>
  )}
      </div>
    </div>
  )
}
