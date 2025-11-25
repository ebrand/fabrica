'use client'

import { useState, useEffect } from 'react'

// Inline SVG icons to avoid @heroicons dependency issues
const CheckCircleIcon = ({ className }) => (
  <svg className={className} fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
    <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>
)

const XCircleIcon = ({ className }) => (
  <svg className={className} fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
    <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 9.75l4.5 4.5m0-4.5l-4.5 4.5M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>
)

const ExclamationCircleIcon = ({ className }) => (
  <svg className={className} fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
    <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
  </svg>
)

const InformationCircleIcon = ({ className }) => (
  <svg className={className} fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
    <path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" />
  </svg>
)

const XMarkIcon = ({ className }) => (
  <svg className={className} viewBox="0 0 20 20" fill="currentColor">
    <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z" />
  </svg>
)

const ICONS = {
  success: CheckCircleIcon,
  error: XCircleIcon,
  warning: ExclamationCircleIcon,
  info: InformationCircleIcon,
}

const ICON_COLORS = {
  success: 'text-green-400',
  error: 'text-red-400',
  warning: 'text-yellow-400',
  info: 'text-blue-400',
}

export default function Toast({
  title,
  message,
  type = 'success',
  show: controlledShow,
  onClose,
  autoHide = false,
  autoHideDelay = 5000,
  position = 'top-right'
}) {
  const [internalShow, setInternalShow] = useState(true)
  const [isVisible, setIsVisible] = useState(false)

  const show = controlledShow !== undefined ? controlledShow : internalShow

  const handleClose = () => {
    if (onClose) {
      onClose()
    } else {
      setInternalShow(false)
    }
  }

  // Handle enter/exit animations
  useEffect(() => {
    if (show) {
      // Small delay to trigger CSS transition
      const timer = setTimeout(() => setIsVisible(true), 10)
      return () => clearTimeout(timer)
    } else {
      setIsVisible(false)
    }
  }, [show])

  useEffect(() => {
    if (autoHide && show) {
      const timer = setTimeout(handleClose, autoHideDelay)
      return () => clearTimeout(timer)
    }
  }, [autoHide, autoHideDelay, show])

  const Icon = ICONS[type] || CheckCircleIcon
  const iconColor = ICON_COLORS[type] || 'text-green-400'

  const getPositionClasses = () => {
    switch (position) {
      case 'top-left':
        return 'items-start sm:items-start'
      case 'top-right':
        return 'items-end sm:items-start'
      case 'bottom-left':
        return 'items-start sm:items-end'
      case 'bottom-right':
        return 'items-end sm:items-end'
      case 'top-center':
        return 'items-center sm:items-start'
      case 'bottom-center':
        return 'items-center sm:items-end'
      default:
        return 'items-end sm:items-start'
    }
  }

  if (!show && !isVisible) return null

  return (
    <div
      aria-live="assertive"
      className={`pointer-events-none fixed inset-0 flex px-4 py-6 sm:p-6 ${getPositionClasses()}`}
    >
      <div className="flex w-full flex-col items-center space-y-4 sm:items-end">
        <div
          className={`
            pointer-events-auto w-full max-w-sm rounded-lg bg-white shadow-lg ring-1 ring-black/5
            transform transition-all duration-300 ease-out
            ${isVisible ? 'opacity-100 translate-y-0 sm:translate-x-0' : 'opacity-0 translate-y-2 sm:translate-x-2 sm:translate-y-0'}
          `}
        >
          <div className="p-4">
            <div className="flex items-start">
              <div className="shrink-0">
                <Icon className={`h-6 w-6 ${iconColor}`} />
              </div>
              <div className="ml-3 w-0 flex-1 pt-0.5">
                <p className="text-sm font-medium text-gray-900">{title}</p>
                {message && (
                  <p className="mt-1 text-sm text-gray-500">
                    {message}
                  </p>
                )}
              </div>
              <div className="ml-4 flex shrink-0">
                <button
                  type="button"
                  onClick={handleClose}
                  className="inline-flex rounded-md text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
                >
                  <span className="sr-only">Close</span>
                  <XMarkIcon className="h-5 w-5" />
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
