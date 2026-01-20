import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { EmailStep } from './EmailStep'
import { LoginStep } from './LoginStep'
import { RegisterStep } from './RegisterStep'
import { ForgotPasswordStep } from './ForgotPasswordStep'
import { authApi } from '../../api/authApi'
import TurnstileWidget from '../ui/TurnstileWidget'
import { useAuthStore } from '../../store/authStore'

type Step = 'email' | 'login' | 'register' | 'forgot'

interface AuthModalProps {
  isOpen: boolean
  onClose: () => void
}

export function AuthModal({ isOpen, onClose }: AuthModalProps) {
  const [step, setStep] = useState<Step>('email')
  const [email, setEmail] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [turnstileToken, setTurnstileToken] = useState<string | null>(null)
  const setAuth = useAuthStore((state) => state.setAuth)
  const { t } = useTranslation()

  if (!isOpen) return null

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
      onClose()
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
      onClose()
    } catch {
      setError(t('auth.register_error'))
    } finally {
      setIsLoading(false)
    }
  }

  const handleGoogleLogin = async () => {
    setError(null)
    try {
      // TODO: Integrate real Google OAuth
      alert(t('auth.google_oauth_notice'))
    } catch {
      setError(t('auth.google_error'))
    }
  }

  const handleBack = () => {
    setStep('email')
    setError(null)
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="relative w-full max-w-md rounded-2xl bg-surface p-8 shadow-2xl">
        <button
          onClick={onClose}
          className="absolute right-4 top-4 text-foreground-muted hover:text-foreground"
        >
          <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>

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
          <LoginStep
            email={email}
            onBack={handleBack}
            onSubmit={handleLogin}
            isLoading={isLoading}
          />
        )}

        {step === 'register' && (
          <RegisterStep
            email={email}
            onBack={handleBack}
            onSubmit={handleRegister}
            isLoading={isLoading}
          />
        )}

        {step === 'forgot' && <ForgotPasswordStep onBack={handleBack} onSubmit={async (email) => {
          setIsLoading(true)
          setError(null)
          try {
            await authApi.requestPasswordReset({ email, turnstileToken: turnstileToken ?? undefined })
            setStep('email')
          } catch {
            setError(t('auth.reset_error'))
          } finally {
            setIsLoading(false)
          }
        }} />}

        {(step === 'login' || step === 'register' || step === 'forgot') && (
          <div className="mt-4">
            <TurnstileWidget onVerify={(t) => setTurnstileToken(t)} />
          </div>
        )}
      </div>
    </div>
  )
}

export default AuthModal
