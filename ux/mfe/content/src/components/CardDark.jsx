import { useState, useEffect } from 'react'
import PropTypes from 'prop-types'

const BFF_CONTENT_URL = import.meta.env.VITE_BFF_CONTENT_URL || 'http://localhost:3240'

/**
 * CardDark - A dark-themed card component for content display
 *
 * Usage:
 *   <CardDark code="my-content" />                     // Fetch by code
 *   <CardDark code="my-content" locale="es-ES" />      // Fetch with locale
 *   <CardDark content={{title, body}} />               // Direct content
 *   <CardDark code="my-content" accentColor="indigo" /> // Custom accent
 */
function CardDark({
  code,
  locale = 'en-US',
  tenantId,
  content: directContent,
  size = 'md',
  accentColor = 'indigo',
  showCta = true,
  showImage = true,
  imagePosition = 'top',
  className = '',
  onLoad,
  onError
}) {
  const [blockData, setBlockData] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (code) {
      fetchContent()
    }
  }, [code, locale, tenantId])

  const fetchContent = async () => {
    try {
      setLoading(true)
      setError(null)

      const params = new URLSearchParams({ localeCode: locale })
      if (tenantId) params.append('tenantId', tenantId)
      params.append('variant', 'dark')

      const response = await fetch(`${BFF_CONTENT_URL}/api/content/blocks/code/${code}?${params}`)

      if (!response.ok) {
        throw new Error(`Content "${code}" not found`)
      }

      const data = await response.json()
      setBlockData(data)
      onLoad?.(data)
    } catch (err) {
      console.error('CardDark fetch error:', err)
      setError(err.message)
      onError?.(err)
    } finally {
      setLoading(false)
    }
  }

  const content = directContent || (blockData ? {
    title: blockData.title || blockData.sections?.title,
    subtitle: blockData.subtitle || blockData.sections?.subtitle,
    body: blockData.body || blockData.sections?.body,
    ctaText: blockData.ctaText || blockData.sections?.['cta-text'],
    ctaUrl: blockData.ctaUrl || blockData.sections?.['cta-url'],
    imageUrl: blockData.imageUrl || blockData.sections?.['image-url'],
    author: blockData.author || blockData.sections?.author
  } : null)

  const sizeStyles = {
    sm: { container: 'p-4', title: 'text-lg', body: 'text-sm' },
    md: { container: 'p-6', title: 'text-xl', body: 'text-base' },
    lg: { container: 'p-8', title: 'text-2xl', body: 'text-lg' },
    xl: { container: 'p-10', title: 'text-3xl', body: 'text-xl' }
  }

  const accentColors = {
    indigo: { ring: 'ring-indigo-500', button: 'bg-indigo-600 hover:bg-indigo-500', glow: 'shadow-indigo-500/20' },
    purple: { ring: 'ring-purple-500', button: 'bg-purple-600 hover:bg-purple-500', glow: 'shadow-purple-500/20' },
    blue: { ring: 'ring-blue-500', button: 'bg-blue-600 hover:bg-blue-500', glow: 'shadow-blue-500/20' },
    emerald: { ring: 'ring-emerald-500', button: 'bg-emerald-600 hover:bg-emerald-500', glow: 'shadow-emerald-500/20' },
    amber: { ring: 'ring-amber-500', button: 'bg-amber-600 hover:bg-amber-500', glow: 'shadow-amber-500/20' },
    rose: { ring: 'ring-rose-500', button: 'bg-rose-600 hover:bg-rose-500', glow: 'shadow-rose-500/20' }
  }

  const sizes = sizeStyles[size] || sizeStyles.md
  const accent = accentColors[accentColor] || accentColors.indigo

  if (code && loading) {
    return (
      <div className={`animate-pulse bg-gray-900 rounded-xl ${sizes.container} ${className}`}>
        <div className="h-6 bg-gray-700 rounded w-3/4 mb-4"></div>
        <div className="h-4 bg-gray-700 rounded w-full mb-2"></div>
        <div className="h-4 bg-gray-700 rounded w-5/6"></div>
      </div>
    )
  }

  if (error) {
    return (
      <div className={`bg-gray-900 border border-red-500/50 rounded-xl ${sizes.container} ${className}`}>
        <p className="text-red-400 text-sm">{error}</p>
      </div>
    )
  }

  if (!content) return null

  const { title, subtitle, body, ctaText, ctaUrl, imageUrl, author } = content

  return (
    <div className={`
      bg-gray-900 rounded-xl shadow-xl ${accent.glow}
      ring-1 ${accent.ring}/20
      ${sizes.container} ${className}
    `}>
      {showImage && imageUrl && imagePosition === 'top' && (
        <div className="-mx-6 -mt-6 mb-6 overflow-hidden rounded-t-xl">
          <img
            src={imageUrl}
            alt={title || 'Card image'}
            className="w-full h-48 object-cover"
          />
        </div>
      )}

      {title && (
        <h3 className={`font-bold text-white mb-2 ${sizes.title}`}>
          {title}
        </h3>
      )}

      {subtitle && (
        <p className="text-gray-400 text-sm mb-3">
          {subtitle}
        </p>
      )}

      {body && (
        <div
          className={`text-gray-300 leading-relaxed mb-4 ${sizes.body}`}
          dangerouslySetInnerHTML={{ __html: body }}
        />
      )}

      {author && (
        <p className="text-gray-500 text-sm italic mb-4">
          By {author}
        </p>
      )}

      {showImage && imageUrl && imagePosition === 'bottom' && (
        <div className="-mx-6 -mb-6 mt-6 overflow-hidden rounded-b-xl">
          <img
            src={imageUrl}
            alt={title || 'Card image'}
            className="w-full h-48 object-cover"
          />
        </div>
      )}

      {showCta && ctaText && ctaUrl && (
        <a
          href={ctaUrl}
          className={`
            inline-flex items-center px-4 py-2 rounded-lg
            text-white font-medium text-sm
            ${accent.button}
            transition-colors duration-200
          `}
        >
          {ctaText}
          <svg className="ml-2 w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
          </svg>
        </a>
      )}
    </div>
  )
}

CardDark.propTypes = {
  code: PropTypes.string,
  locale: PropTypes.string,
  tenantId: PropTypes.string,
  content: PropTypes.shape({
    title: PropTypes.string,
    subtitle: PropTypes.string,
    body: PropTypes.string,
    ctaText: PropTypes.string,
    ctaUrl: PropTypes.string,
    imageUrl: PropTypes.string,
    author: PropTypes.string
  }),
  size: PropTypes.oneOf(['sm', 'md', 'lg', 'xl']),
  accentColor: PropTypes.oneOf(['indigo', 'purple', 'blue', 'emerald', 'amber', 'rose']),
  showCta: PropTypes.bool,
  showImage: PropTypes.bool,
  imagePosition: PropTypes.oneOf(['top', 'bottom']),
  className: PropTypes.string,
  onLoad: PropTypes.func,
  onError: PropTypes.func
}

export default CardDark
