import { useState, useRef, useEffect } from 'react'
import { useTranslation } from 'react-i18next'

interface EmojiPickerProps {
  value: string | null
  onChange: (emoji: string | null) => void
  className?: string
}

const POPULAR_EMOJIS = [
  // Об'єкти та покупки
  '🛍️', '🛒', '📱', '💻', '⌚', '📷', '🎮', '🎧', '🎸', '⚽',
  // Одяг та аксесуари
  '👕', '👔', '👗', '👠', '👟', '👜', '🎒', '👓', '🕶️', '💍',
  // Їжа та напої
  '🍕', '🍔', '☕', '🍰', '🍎', '🥗', '🍜', '🥤', '🍺', '🍷',
  // Дім та побут
  '🏠', '🛋️', '🛏️', '🪴', '💡', '🔌', '🧹', '🧺', '🪟', '🚪',
  // Спорт та фітнес
  '⚽', '🏀', '🎾', '🏋️', '🚴', '🏊', '⛷️', '🏃', '🧘', '🤸',
  // Краса та здоров'я
  '💄', '💅', '💆', '💇', '🧴', '🧼', '🪥', '🧖', '💊', '🩺',
  // Транспорт
  '🚗', '🚙', '🚕', '🚌', '🚎', '🏍️', '🚲', '🛴', '✈️', '🚁',
  // Книги та освіта
  '📚', '📖', '✏️', '📝', '🎓', '🧮', '📐', '🔬', '🔭', '🧪',
  // Іграшки та дитяче
  '🧸', '🪀', '🎯', '🎲', '🎪', '🎨', '🖍️', '🎭', '🎪', '🎡',
  // Інші
  '⭐', '❤️', '🔥', '✨', '🎁', '🎉', '🎈', '🏆', '🔔', '⚡'
]

export default function EmojiPicker({ value, onChange, className = '' }: EmojiPickerProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)
  const [customEmoji, setCustomEmoji] = useState('')
  const dropdownRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside)
      return () => document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isOpen])

  const handleEmojiSelect = (emoji: string) => {
    onChange(emoji)
    setIsOpen(false)
    setCustomEmoji('')
  }

  const handleCustomEmojiSubmit = () => {
    if (customEmoji.trim()) {
      onChange(customEmoji.trim())
      setCustomEmoji('')
      setIsOpen(false)
    }
  }

  const handleClear = () => {
    onChange(null)
    setIsOpen(false)
  }

  return (
    <div className={`relative ${className}`} ref={dropdownRef}>
      <label className="block text-sm font-medium text-text mb-1">
        {t('admin.catalog.emoji')}
      </label>
      
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        className="w-full px-3 py-2 rounded-lg border border-gray-600 bg-surface text-text 
          hover:border-brand focus:outline-none focus:ring-2 focus:ring-brand focus:border-transparent
          transition-colors flex items-center justify-between"
      >
        <span className="flex items-center gap-2">
          {value ? (
            <>
              <span className="text-2xl">{value}</span>
              <span className="text-sm text-text-muted">{t('admin.catalog.emoji_selected')}</span>
            </>
          ) : (
            <span className="text-text-muted">{t('admin.catalog.select_emoji')}</span>
          )}
        </span>
        <svg 
          className={`w-5 h-5 text-text-muted transition-transform ${isOpen ? 'rotate-180' : ''}`} 
          fill="none" 
          stroke="currentColor" 
          viewBox="0 0 24 24"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
        </svg>
      </button>

      {isOpen && (
        <div className="absolute z-50 mt-2 w-full bg-surface border border-gray-600 rounded-lg shadow-lg max-h-96 overflow-y-auto">
          <div className="p-3">
            <div className="mb-3">
              <p className="text-xs font-medium text-text-muted mb-2 uppercase">
                {t('admin.catalog.popular_emojis')}
              </p>
              <div className="grid grid-cols-10 gap-1">
                {POPULAR_EMOJIS.map((emoji, index) => (
                  <button
                    key={index}
                    type="button"
                    onClick={() => handleEmojiSelect(emoji)}
                    className="text-2xl hover:bg-surface-secondary rounded p-1 transition-colors
                      focus:outline-none focus:ring-2 focus:ring-brand"
                    title={emoji}
                  >
                    {emoji}
                  </button>
                ))}
              </div>
            </div>

            <div className="border-t border-border pt-3 mb-3">
              <p className="text-xs font-medium text-text-muted mb-2 uppercase">
                {t('admin.catalog.custom_emoji')}
              </p>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={customEmoji}
                  onChange={(e) => setCustomEmoji(e.target.value)}
                  placeholder={t('admin.catalog.enter_emoji')}
                  maxLength={10}
                  className="flex-1 px-3 py-2 rounded-lg border border-gray-600 bg-surface text-text 
                    focus:outline-none focus:ring-2 focus:ring-brand focus:border-transparent text-sm"
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      e.preventDefault()
                      handleCustomEmojiSubmit()
                    }
                  }}
                />
                <button
                  type="button"
                  onClick={handleCustomEmojiSubmit}
                  disabled={!customEmoji.trim()}
                  className="px-3 py-2 bg-brand text-white rounded-lg hover:bg-brand-dark 
                    transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm"
                >
                  {t('admin.catalog.add')}
                </button>
              </div>
            </div>

            {value && (
              <button
                type="button"
                onClick={handleClear}
                className="w-full px-3 py-2 text-sm text-red-500 hover:bg-red-500/10 
                  rounded-lg transition-colors"
              >
                {t('admin.catalog.clear_emoji')}
              </button>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
