import { useState, useRef, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import type { AttributeDefinitionDto } from '../../api/attributeDefinitionsApi'

interface AttributeField {
  key: string
  value: string
}

interface AttributeSelectorProps {
  attributes: AttributeField[]
  availableAttributes: AttributeDefinitionDto[]
  onChange: (attributes: AttributeField[]) => void
  showVariantOnly?: boolean
  disabled?: boolean
}

export default function AttributeSelector({
  attributes,
  availableAttributes,
  onChange,
  showVariantOnly = false,
  disabled = false,
}: AttributeSelectorProps) {
  const { t } = useTranslation()
  const [openDropdownIndex, setOpenDropdownIndex] = useState<number | null>(null)
  const [keySearch, setKeySearch] = useState('')
  const dropdownRef = useRef<HTMLDivElement>(null)

  // Filter available attributes based on settings
  const filteredDefinitions = availableAttributes.filter((attr) => {
    if (showVariantOnly && !attr.isVariant) return false
    if (!attr.isActive) return false
    return true
  })

  // Get unused attribute definitions (not already selected)
  const getUnusedAttributes = (currentKey: string) => {
    const usedKeys = attributes
      .map((a) => a.key.trim().toLowerCase())
      .filter((k) => k && k !== currentKey.toLowerCase())
    return filteredDefinitions.filter(
      (def) =>
        !usedKeys.includes(def.code.toLowerCase()) &&
        (keySearch === '' ||
          def.name.toLowerCase().includes(keySearch.toLowerCase()) ||
          def.code.toLowerCase().includes(keySearch.toLowerCase()))
    )
  }

  // Get definition by code
  const getDefinition = (code: string) =>
    filteredDefinitions.find((d) => d.code.toLowerCase() === code.toLowerCase())

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setOpenDropdownIndex(null)
        setKeySearch('')
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const handleAddAttribute = () => {
    onChange([...attributes, { key: '', value: '' }])
  }

  const handleRemoveAttribute = (index: number) => {
    onChange(attributes.filter((_, i) => i !== index))
  }

  const handleKeyChange = (index: number, code: string) => {
    const newAttributes = [...attributes]
    newAttributes[index] = { ...newAttributes[index], key: code, value: '' }
    onChange(newAttributes)
    setOpenDropdownIndex(null)
    setKeySearch('')
  }

  const handleValueChange = (index: number, value: string) => {
    const newAttributes = [...attributes]
    newAttributes[index] = { ...newAttributes[index], value }
    onChange(newAttributes)
  }

  const renderValueInput = (attr: AttributeField, index: number) => {
    const definition = getDefinition(attr.key)

    if (!definition) {
      // No definition found - show regular text input
      return (
        <input
          type="text"
          value={attr.value}
          onChange={(e) => handleValueChange(index, e.target.value)}
          placeholder={t('sku.attribute_value')}
          disabled={disabled}
          className="flex-1 px-3 py-2 border border-border rounded-lg bg-background text-text text-sm 
            focus:outline-none focus:ring-2 focus:ring-brand disabled:opacity-50"
        />
      )
    }

    // Has allowed values - show dropdown
    if (definition.allowedValues && definition.allowedValues.length > 0) {
      return (
        <select
          value={attr.value}
          onChange={(e) => handleValueChange(index, e.target.value)}
          disabled={disabled}
          className="flex-1 px-3 py-2 border border-border rounded-lg bg-background text-text text-sm 
            focus:outline-none focus:ring-2 focus:ring-brand disabled:opacity-50
            [&>option]:bg-surface [&>option]:text-text"
        >
          <option value="">{t('attribute_selector.select_value')}</option>
          {definition.allowedValues.map((val) => (
            <option key={val} value={val}>
              {val}
            </option>
          ))}
        </select>
      )
    }

    // Based on data type
    switch (definition.dataType) {
      case 'boolean':
        return (
          <select
            value={attr.value}
            onChange={(e) => handleValueChange(index, e.target.value)}
            disabled={disabled}
            className="flex-1 px-3 py-2 border border-border rounded-lg bg-background text-text text-sm 
              focus:outline-none focus:ring-2 focus:ring-brand disabled:opacity-50
              [&>option]:bg-surface [&>option]:text-text"
          >
            <option value="">{t('attribute_selector.select_value')}</option>
            <option value="true">{t('common.yes')}</option>
            <option value="false">{t('common.no')}</option>
          </select>
        )

      case 'number':
        return (
          <div className="flex-1 flex items-center gap-1">
            <input
              type="number"
              step="any"
              value={attr.value}
              onChange={(e) => handleValueChange(index, e.target.value)}
              placeholder="0"
              disabled={disabled}
              className="flex-1 px-3 py-2 border border-border rounded-lg bg-background text-text text-sm 
                focus:outline-none focus:ring-2 focus:ring-brand disabled:opacity-50"
            />
            {definition.unit && (
              <span className="text-text-muted text-sm px-2">{definition.unit}</span>
            )}
          </div>
        )

      default:
        return (
          <input
            type="text"
            value={attr.value}
            onChange={(e) => handleValueChange(index, e.target.value)}
            placeholder={definition.unit ? `${t('sku.attribute_value')} (${definition.unit})` : t('sku.attribute_value')}
            disabled={disabled}
            className="flex-1 px-3 py-2 border border-border rounded-lg bg-background text-text text-sm 
              focus:outline-none focus:ring-2 focus:ring-brand disabled:opacity-50"
          />
        )
    }
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <label className="text-sm font-medium text-text">
          {showVariantOnly ? t('attribute_selector.variant_attributes') : t('sku.attributes')}
        </label>
        <button
          type="button"
          onClick={handleAddAttribute}
          disabled={disabled || getUnusedAttributes('').length === 0}
          className="text-brand hover:text-brand-hover text-sm flex items-center gap-1 
            disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          {t('sku.add_attribute')}
        </button>
      </div>

      {showVariantOnly && (
        <p className="text-xs text-text-muted">{t('attribute_selector.variant_hint')}</p>
      )}

      {attributes.length === 0 ? (
        <p className="text-text-muted text-sm italic">{t('sku.no_attributes')}</p>
      ) : (
        <div className="space-y-2" ref={dropdownRef}>
          {attributes.map((attr, index) => {
            const definition = getDefinition(attr.key)
            const unusedAttrs = getUnusedAttributes(attr.key)

            return (
              <div key={index} className="flex gap-2 items-start">
                {/* Attribute Key Selector */}
                <div className="relative flex-1">
                  <button
                    type="button"
                    onClick={() => {
                      if (!disabled) {
                        setOpenDropdownIndex(openDropdownIndex === index ? null : index)
                        setKeySearch('')
                      }
                    }}
                    disabled={disabled}
                    className={`w-full px-3 py-2 border rounded-lg bg-background text-left text-sm 
                      focus:outline-none focus:ring-2 focus:ring-brand disabled:opacity-50
                      ${attr.key ? 'text-text' : 'text-text-muted'}
                      ${definition?.isRequired ? 'border-amber-500' : 'border-border'}
                      ${definition?.isVariant ? 'border-l-4 border-l-brand' : ''}`}
                  >
                    <span className="flex items-center justify-between">
                      <span>
                        {definition ? (
                          <>
                            {definition.name}
                            {definition.isVariant && (
                              <span className="ml-1 text-xs text-brand">(V)</span>
                            )}
                          </>
                        ) : attr.key ? (
                          attr.key
                        ) : (
                          t('attribute_selector.select_attribute')
                        )}
                      </span>
                      <svg
                        className="w-4 h-4 text-text-muted"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M19 9l-7 7-7-7"
                        />
                      </svg>
                    </span>
                  </button>

                  {openDropdownIndex === index && (
                    <div className="absolute z-50 w-full mt-1 bg-surface border border-border rounded-lg shadow-lg max-h-60 overflow-auto">
                      <div className="p-2 sticky top-0 bg-surface border-b border-border">
                        <input
                          type="text"
                          value={keySearch}
                          onChange={(e) => setKeySearch(e.target.value)}
                          placeholder={t('common.search')}
                          className="w-full px-3 py-2 border border-border rounded-lg bg-surface text-text text-sm"
                          autoFocus
                        />
                      </div>
                      {unusedAttrs.length === 0 ? (
                        <div className="p-3 text-text-muted text-sm">{t('common.no_results')}</div>
                      ) : (
                        unusedAttrs.map((def) => (
                          <button
                            key={def.id}
                            type="button"
                            onClick={() => handleKeyChange(index, def.code)}
                            className="w-full px-4 py-2 hover:bg-surface-secondary text-left flex items-center justify-between group"
                          >
                            <div>
                              <span className="text-text">{def.name}</span>
                              <span className="ml-2 text-xs text-text-muted">({def.code})</span>
                              {def.description && (
                                <p className="text-xs text-text-muted truncate max-w-[200px]">
                                  {def.description}
                                </p>
                              )}
                            </div>
                            <div className="flex gap-1">
                              {def.isRequired && (
                                <span className="px-1.5 py-0.5 rounded bg-amber-500/20 text-amber-500 text-xs">
                                  R
                                </span>
                              )}
                              {def.isVariant && (
                                <span className="px-1.5 py-0.5 rounded bg-brand/20 text-brand text-xs">
                                  V
                                </span>
                              )}
                            </div>
                          </button>
                        ))
                      )}
                    </div>
                  )}
                </div>

                {/* Value Input */}
                {renderValueInput(attr, index)}

                {/* Remove Button */}
                <button
                  type="button"
                  onClick={() => handleRemoveAttribute(index)}
                  disabled={disabled}
                  className="p-2 text-red-500 hover:text-red-700 transition-colors disabled:opacity-50"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M6 18L18 6M6 6l12 12"
                    />
                  </svg>
                </button>
              </div>
            )
          })}
        </div>
      )}

      {/* Legend */}
      <div className="flex gap-4 text-xs text-text-muted mt-2">
        <span className="flex items-center gap-1">
          <span className="w-2 h-2 rounded bg-brand"></span>
          {t('attribute_selector.variant_marker')}
        </span>
        <span className="flex items-center gap-1">
          <span className="w-2 h-2 rounded bg-amber-500"></span>
          {t('attribute_selector.required_marker')}
        </span>
      </div>
    </div>
  )
}
