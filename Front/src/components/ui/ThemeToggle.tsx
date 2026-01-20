import { useTheme } from '../../contexts/ThemeContext'
import { useTranslation } from 'react-i18next'

export function ThemeToggle() {
  const { theme, setTheme, actualTheme } = useTheme()
  const { t } = useTranslation()

  const toggleTheme = () => {
    if (theme === 'light') {
      setTheme('dark')
    } else if (theme === 'dark') {
      setTheme('system')
    } else {
      setTheme('light')
    }
  }

  const getThemeIcon = () => {
    if (theme === 'system') {
      return actualTheme === 'dark' ? '🌙' : '☀️'
    }
    return theme === 'dark' ? '🌙' : '☀️'
  }

  const getThemeLabel = () => {
    if (theme === 'system') {
      return t('theme.system', { defaultValue: 'System' })
    }
    return theme === 'dark' ? t('theme.dark', { defaultValue: 'Dark' }) : t('theme.light', { defaultValue: 'Light' })
  }

  return (
    <button
      onClick={toggleTheme}
      className="relative group inline-flex flex-col items-center no-underline px-1 py-0.5 rounded-md hover:bg-surface transition-colors"
      title={getThemeLabel()}
      aria-label={getThemeLabel()}
    >
      <span className="h-9 w-9 inline-flex items-center justify-center text-lg text-foreground-muted group-hover:text-foreground transition-colors">
        {getThemeIcon()}
      </span>
      <span className="mt-1 text-xs text-foreground-muted hidden sm:block transition-colors duration-150 group-hover:text-foreground group-hover:font-medium">
        {getThemeLabel()}
      </span>
    </button>
  )
}