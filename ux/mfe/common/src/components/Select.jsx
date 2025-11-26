'use client'

import { useState, useRef, useEffect } from 'react'

export default function Select({
  label,
  options = [],
  value,
  onChange,
  defaultIndex = 0,
  displayKey = 'name',
  valueKey = 'id',
  placeholder = 'Select...',
  className = ''
}) {
  const [isOpen, setIsOpen] = useState(false)
  const [internalValue, setInternalValue] = useState(value || options[defaultIndex] || null)
  const containerRef = useRef(null)

  const selectedValue = value !== undefined ? value : internalValue
  const handleChange = onChange || setInternalValue

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (containerRef.current && !containerRef.current.contains(event.target)) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const getDisplayValue = (item) => {
    if (!item) return ''
    return typeof item === 'string' ? item : item[displayKey]
  }

  const getKeyValue = (item) => {
    if (!item) return ''
    return typeof item === 'string' ? item : item[valueKey]
  }

  const handleSelect = (option) => {
    handleChange(option)
    setIsOpen(false)
  }

  const isSelected = (option) => {
    if (!selectedValue) return false
    return getKeyValue(option) === getKeyValue(selectedValue)
  }

  return (
    <div ref={containerRef} className={`relative ${className}`}>
      {label && (
        <label className="block text-sm font-medium text-gray-700 mb-1">
          {label}
        </label>
      )}

      {/* Select Button */}
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        className={`
          relative w-full cursor-pointer rounded-md bg-white py-2 pl-3 pr-10 text-left text-gray-900
          border shadow-sm text-sm
          ${isOpen ? 'border-blue-500 ring-1 ring-blue-500' : 'border-gray-300'}
          focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500
          hover:border-gray-400
        `}
      >
        <span className="block truncate">
          {getDisplayValue(selectedValue) || placeholder}
        </span>
        <span className="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-2">
          <svg className="h-5 w-5 text-gray-400" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M10 3a.75.75 0 01.55.24l3.25 3.5a.75.75 0 11-1.1 1.02L10 4.852 7.3 7.76a.75.75 0 01-1.1-1.02l3.25-3.5A.75.75 0 0110 3zm-3.76 9.2a.75.75 0 011.06.04l2.7 2.908 2.7-2.908a.75.75 0 111.1 1.02l-3.25 3.5a.75.75 0 01-1.1 0l-3.25-3.5a.75.75 0 01.04-1.06z" clipRule="evenodd" />
          </svg>
        </span>
      </button>

      {/* Dropdown Options */}
      {isOpen && (
        <ul className="absolute z-10 mt-1 max-h-60 w-full overflow-auto rounded-md bg-white py-1 text-base shadow-lg ring-1 ring-black/5 focus:outline-none text-sm">
          {options.map((option, index) => {
            const key = getKeyValue(option) ?? index
            const display = getDisplayValue(option)
            const selected = isSelected(option)

            return (
              <li
                key={key}
                onClick={() => handleSelect(option)}
                className="group relative cursor-pointer select-none py-2 pl-10 pr-4 text-gray-900 hover:bg-blue-600 hover:text-white"
              >
                <span className={`block truncate ${selected ? 'font-semibold' : 'font-normal'}`}>
                  {display}
                </span>
                {selected && (
                  <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-blue-600 group-hover:text-white">
                    <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                      <path fillRule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clipRule="evenodd" />
                    </svg>
                  </span>
                )}
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}
