/// <reference types="vite/client" />
import React, { Suspense } from 'react'

const TurnstileLazy = React.lazy(() => import('react-turnstile'))

export default function TurnstileWidget({
  siteKey,
  onVerify,
}: {
  siteKey?: string
  onVerify?: (token: string) => void
}) {
  const key = siteKey || import.meta.env.VITE_TURNSTILE_SITEKEY
  if (!key) {
    console.warn('Turnstile site key is not configured. Set VITE_TURNSTILE_SITEKEY in your environment to enable the widget.')
    return (
      <div className="rounded-md border border-warning bg-warning-light/20 px-3 py-2 text-sm text-warning">
         Turnstile not configured — set <code>VITE_TURNSTILE_SITEKEY</code> in your frontend env to enable the widget.
       </div>
    )
  }

  return (
    <Suspense fallback={null}>
      {/* @ts-ignore allow unknown props to reach the wrapper */}
      <TurnstileLazy sitekey={key} siteKey={key} callback={onVerify} onVerify={onVerify} />
    </Suspense>
  )
}
