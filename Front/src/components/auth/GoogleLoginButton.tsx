import { GoogleLogin, GoogleOAuthProvider } from '@react-oauth/google'
import { useTranslation } from 'react-i18next'

interface GoogleLoginButtonProps {
  onSuccess: (idToken: string) => void
  onError: (error: string) => void
}

const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID || ''

export function GoogleLoginButton({ onSuccess, onError }: GoogleLoginButtonProps) {
  const { t } = useTranslation()

  if (!GOOGLE_CLIENT_ID) {
    console.warn('Google Client ID not configured')
    return null
  }

  return (
    <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>
      <div className="w-full">
        <GoogleLogin
          onSuccess={(credentialResponse) => {
            if (credentialResponse.credential) {
              onSuccess(credentialResponse.credential)
            } else {
              onError(t('auth.google_error'))
            }
          }}
          onError={() => {
            onError(t('auth.google_error'))
          }}
          useOneTap={false}
          text="continue_with"
          shape="rectangular"
          size="large"
          width="100%"
          theme="outline"
        />
      </div>
    </GoogleOAuthProvider>
  )
}
