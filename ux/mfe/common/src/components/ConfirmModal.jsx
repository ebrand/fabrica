'use client'

import { useState, useEffect } from 'react'

// Warning triangle icon
const WarningIcon = ({ style }) => (
  <svg style={style} fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
    <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z" />
  </svg>
)

export default function ConfirmModal({
  show,
  title,
  message,
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  onConfirm,
  onCancel,
}) {
  const [isVisible, setIsVisible] = useState(false)
  const [cancelHover, setCancelHover] = useState(false)
  const [confirmHover, setConfirmHover] = useState(false)

  useEffect(() => {
    if (show) {
      const timer = setTimeout(() => setIsVisible(true), 10)
      return () => clearTimeout(timer)
    } else {
      setIsVisible(false)
    }
  }, [show])

  const handleConfirm = () => {
    if (onConfirm) onConfirm()
  }

  const handleCancel = () => {
    if (onCancel) onCancel()
  }

  const handleBackdropClick = (e) => {
    if (e.target === e.currentTarget) {
      handleCancel()
    }
  }

  if (!show && !isVisible) return null

  return (
    <div
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        zIndex: 9999,
        overflow: 'auto'
      }}
    >
      {/* Backdrop */}
      <div
        onClick={handleBackdropClick}
        style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: 'rgba(107, 114, 128, 0.75)',
          transition: 'opacity 200ms',
          opacity: isVisible ? 1 : 0
        }}
      />

      {/* Modal centering container */}
      <div
        style={{
          display: 'flex',
          minHeight: '100%',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '1rem'
        }}
      >
        {/* Modal dialog */}
        <div
          style={{
            position: 'relative',
            backgroundColor: 'white',
            borderRadius: '0.75rem',
            boxShadow: '0 25px 50px -12px rgba(0, 0, 0, 0.25)',
            width: '100%',
            maxWidth: '28rem',
            transition: 'all 200ms',
            opacity: isVisible ? 1 : 0,
            transform: isVisible ? 'scale(1)' : 'scale(0.95)'
          }}
        >
          <div style={{ padding: '1.5rem' }}>
            {/* Icon and content */}
            <div style={{ display: 'flex', alignItems: 'flex-start', gap: '1rem' }}>
              <div
                style={{
                  flexShrink: 0,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  width: '2.5rem',
                  height: '2.5rem',
                  borderRadius: '9999px',
                  backgroundColor: '#FEE2E2' // red-100
                }}
              >
                <WarningIcon style={{ height: '1.25rem', width: '1.25rem', color: '#DC2626' }} />
              </div>
              <div style={{ flex: 1, minWidth: 0 }}>
                <h3
                  style={{
                    fontSize: '1.125rem',
                    fontWeight: 600,
                    color: '#111827', // gray-900
                    margin: 0
                  }}
                >
                  {title}
                </h3>
                <p
                  style={{
                    marginTop: '0.5rem',
                    fontSize: '0.875rem',
                    color: '#6B7280', // gray-500
                    margin: '0.5rem 0 0 0'
                  }}
                >
                  {message}
                </p>
              </div>
            </div>

            {/* Buttons */}
            <div style={{ marginTop: '1.5rem', display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button
                type="button"
                onClick={handleCancel}
                onMouseEnter={() => setCancelHover(true)}
                onMouseLeave={() => setCancelHover(false)}
                style={{
                  padding: '0.5rem 1rem',
                  fontSize: '0.875rem',
                  fontWeight: 500,
                  color: '#374151', // gray-700
                  backgroundColor: cancelHover ? '#F9FAFB' : 'white', // hover:bg-gray-50
                  border: '1px solid #D1D5DB', // border-gray-300
                  borderRadius: '0.5rem',
                  cursor: 'pointer',
                  outline: 'none'
                }}
              >
                {cancelText}
              </button>
              <button
                type="button"
                onClick={handleConfirm}
                onMouseEnter={() => setConfirmHover(true)}
                onMouseLeave={() => setConfirmHover(false)}
                style={{
                  padding: '0.5rem 1rem',
                  fontSize: '0.875rem',
                  fontWeight: 500,
                  color: 'white',
                  backgroundColor: confirmHover ? '#B91C1C' : '#DC2626', // red-600 / hover:red-700
                  border: '1px solid transparent',
                  borderRadius: '0.5rem',
                  cursor: 'pointer',
                  outline: 'none'
                }}
              >
                {confirmText}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
