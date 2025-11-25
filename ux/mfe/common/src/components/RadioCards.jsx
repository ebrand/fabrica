'use client'

import { useState } from 'react'

export default function RadioCards({
  label,
  helpLink,
  options = [],
  name = 'radio-option',
  value,
  onChange,
  defaultValue,
  columns = { default: 3, sm: 6 },
  displayKey = 'name',
  valueKey = 'id',
  stockKey = 'inStock'
}) {
  const [internalValue, setInternalValue] = useState(value || defaultValue)

  const selectedValue = value !== undefined ? value : internalValue

  const handleChange = (newValue) => {
    if (onChange) {
      onChange(newValue)
    } else {
      setInternalValue(newValue)
    }
  }

  const getColumnClasses = () => {
    const classes = []
    if (columns.default) classes.push(`grid-cols-${columns.default}`)
    if (columns.sm) classes.push(`sm:grid-cols-${columns.sm}`)
    return classes.join(' ')
  }

  return (
    <fieldset aria-label={label}>
      <div className="flex items-center justify-between">
        {label && (
          <div className="text-sm/6 font-medium text-gray-900 dark:text-white">
            {label}
          </div>
        )}
        {helpLink && (
          <a
            href={helpLink.href}
            className="text-sm/6 font-medium text-indigo-600 hover:text-indigo-500 dark:text-indigo-400 dark:hover:text-indigo-300"
          >
            {helpLink.text}
          </a>
        )}
      </div>
      <div className={`mt-2 grid gap-3 ${getColumnClasses()}`}>
        {options.map((option) => {
          const optionValue = typeof option === 'string' ? option : option[valueKey]
          const optionDisplay = typeof option === 'string' ? option : option[displayKey]
          const optionInStock = typeof option === 'string' ? true : option[stockKey] !== false

          return (
            <label
              key={optionValue}
              aria-label={optionDisplay}
              className="group relative flex items-center justify-center rounded-md border border-gray-300 bg-white p-3 has-checked:border-indigo-600 has-checked:bg-indigo-600 has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-indigo-600 has-disabled:border-gray-400 has-disabled:bg-gray-200 has-disabled:opacity-25 dark:border-white/10 dark:bg-gray-800/50 dark:has-checked:border-indigo-500 dark:has-checked:bg-indigo-500 dark:has-focus-visible:outline-indigo-500 dark:has-disabled:border-white/10 dark:has-disabled:bg-gray-800"
            >
              <input
                value={optionValue}
                checked={selectedValue === optionValue}
                onChange={() => handleChange(optionValue)}
                name={name}
                type="radio"
                disabled={!optionInStock}
                className="absolute inset-0 appearance-none focus:outline-none disabled:cursor-not-allowed"
              />
              <span className="text-sm font-medium text-gray-900 uppercase group-has-checked:text-white dark:text-white">
                {optionDisplay}
              </span>
            </label>
          )
        })}
      </div>
    </fieldset>
  )
}
