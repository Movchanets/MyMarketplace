import { useTranslation } from 'react-i18next'
import { useEffect, useRef, useState } from 'react'

function FlagUA() {
  return (
    <svg width="18" height="12" viewBox="0 0 18 12" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden>
      <rect width="18" height="6" fill="#005BBB" />
      <rect y="6" width="18" height="6" fill="#FFD500" />
    </svg>
  )
}

function FlagGB() {
  return (
    <svg width="18" height="12" viewBox="0 0 60 30" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden>
      <rect width="60" height="30" fill="#012169" />
      <path d="M0 0 L60 30 M60 0 L0 30" stroke="#fff" strokeWidth="6" />
      <path d="M0 0 L60 30 M60 0 L0 30" stroke="#C8102E" strokeWidth="4" />
      <path d="M30 0 L30 30 M0 15 L60 15" stroke="#fff" strokeWidth="10" />
      <path d="M30 0 L30 30 M0 15 L60 15" stroke="#C8102E" strokeWidth="6" />
    </svg>
  )
}

const LANGS: { code: string; labelKey: string; labelFallback: string; Flag: () => React.ReactNode }[] = [
  { code: 'uk', labelKey: 'ukrainian', labelFallback: 'Українська', Flag: FlagUA },
  { code: 'en', labelKey: 'english', labelFallback: 'English', Flag: FlagGB },
]

export default function LanguageSelector({ className, align = 'right', dropUp = false }: { className?: string; align?: 'left' | 'right'; dropUp?: boolean }) {
  const { i18n, t } = useTranslation()
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    const onDoc = (e: MouseEvent) => {
      if (!ref.current) return
      if (e.target instanceof Node && !ref.current.contains(e.target)) setOpen(false)
    }
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false)
    }
    document.addEventListener('click', onDoc)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('click', onDoc)
      document.removeEventListener('keydown', onKey)
    }
  }, [])

  const change = (lng: string) => {
    i18n.changeLanguage(lng)
    try {
      localStorage.setItem('lang', lng)
    } catch (err) {
      void err
    }
    try {
      document.documentElement.lang = lng
    } catch (err) {
      void err
    }
    setOpen(false)
  }

  const current = LANGS.find((l) => l.code === (i18n.language || 'uk')) || LANGS[0]

  return (
    <div className={className} ref={ref}>
      <div className="relative inline-block text-left">
         <button
           type="button"
           aria-haspopup="true"
           aria-expanded={open}
           onClick={() => setOpen((s) => !s)}
           className="inline-flex items-center gap-2 rounded-md border border-border bg-surface px-3 py-1 text-sm shadow-sm hover:shadow-md text-foreground"
         >
          <span className="text-xl" aria-hidden>
            <current.Flag />
          </span>
          <span className="truncate font-medium">{t(current.labelKey) || current.labelFallback}</span>
          <svg className={`h-4 w-4 transition-transform ${open ? 'rotate-180' : ''}`} viewBox="0 0 20 20" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden>
            <path d="M6 8l4 4 4-4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </button>

        {open && (
           <div
             role="menu"
             aria-orientation="vertical"
             className={`absolute ${
               dropUp ? `bottom-full mb-2 origin-bottom-${align === 'right' ? 'right' : 'left'}` : `mt-2 origin-top-${align === 'right' ? 'right' : 'left'}`
             } w-44 rounded-md bg-surface border border-border shadow-lg ring-1 ring-black/5 focus:outline-none z-50 ${
               align === 'right' ? 'right-0' : 'left-0'
             }`}
           >
            <div className="py-1">
              {LANGS.map((l) => (
                <button
                  key={l.code}
                  role="menuitem"
                  onClick={() => change(l.code)}
                   className={`w-full text-left px-3 py-2 text-sm hover:bg-surface-hover flex items-center gap-3 text-foreground ${
                     i18n.language === l.code ? 'font-semibold bg-surface-hover' : 'font-normal'
                   }`}
                >
                   <span className="inline-flex h-7 w-7 items-center justify-center rounded-md bg-surface-hover/50">
                     <l.Flag />
                   </span>
                  <span className="flex-1">{t(l.labelKey) || l.labelFallback}</span>
                </button>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
