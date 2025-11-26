import { useState, useEffect } from 'react'
import PropTypes from 'prop-types'

const BFF_CONTENT_URL = import.meta.env.VITE_BFF_CONTENT_URL || 'http://localhost:3240'

/**
 * CardMinimal - A clean, minimal card component for content display
 *
 * Usage:
 *   <CardMinimal code="my-content" />                  // Fetch by code
 *   <CardMinimal code="my-content" locale="es-ES" />   // Fetch with locale
 *   <CardMinimal content={{title, body}} />            // Direct content
 *   <CardMinimal code="my-content" bordered />         // With border
 */
function CardMinimal({
  code,
  locale = 'en-US',
  tenantId,
  content: directContent,
  size = 'md',
  bordered = false,
  showDivider = true,
  showCta = true,
  showImage = true,
  imagePosition = 'left',
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
      params.append('variant', 'minimal')

      const response = await fetch(`${BFF_CONTENT_URL}/api/content/blocks/code/${code}?${params}`)

      if (!response.ok) {
        throw new Error(`Content "${code}" not found`)
      }

      const data = await response.json()
      setBlockData(data)
      onLoad?.(data)
    } catch (err) {
      console.error('CardMinimal fetch error:', err)
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
    sm: { container: 'py-3', title: 'text-base', body: 'text-sm', gap: 'gap-3' },
    md: { container: 'py-4', title: 'text-lg', body: 'text-base', gap: 'gap-4' },
    lg: { container: 'py-6', title: 'text-xl', body: 'text-base', gap: 'gap-5' },
    xl: { container: 'py-8', title: 'text-2xl', body: 'text-lg', gap: 'gap-6' }
  }

  const sizes = sizeStyles[size] || sizeStyles.md

  if (code && loading) {
    return (
      <div className={`animate-pulse ${sizes.container} ${className}`}>
        <div className="h-5 bg-gray-200 rounded w-2/3 mb-3"></div>
        <div className="h-4 bg-gray-100 rounded w-full mb-2"></div>
        <div className="h-4 bg-gray-100 rounded w-4/5"></div>
      </div>
    )
  }

  if (error) {
    return (
      <div className={`${sizes.container} ${className}`}>
        <p className="text-red-500 text-sm">{error}</p>
      </div>
    )
  }

  if (!content) return null

  const { title, subtitle, body, ctaText, ctaUrl, imageUrl, author } = content
  const hasImage = showImage && imageUrl
  const isHorizontal = hasImage && (imagePosition === 'left' || imagePosition === 'right')

  return (
    <div className={`
      ${bordered ? 'border border-gray-200 rounded-lg px-4' : ''}
      ${sizes.container} ${className}
    `}>
      <div className={`
        ${isHorizontal ? `flex ${sizes.gap} ${imagePosition === 'right' ? 'flex-row-reverse' : ''}` : ''}
      `}>
        {hasImage && (
          <div className={`
            ${isHorizontal ? 'flex-shrink-0 w-24 h-24' : 'mb-4'}
          `}>
            <img
              src={imageUrl}
              alt={title || 'Card image'}
              className={`
                ${isHorizontal ? 'w-24 h-24 rounded-lg' : 'w-full h-40 rounded-lg'}
                object-cover
              `}
            />
          </div>
        )}

        <div className="flex-1 min-w-0">
          {title && (
            <h3 className={`font-semibold text-gray-900 ${sizes.title}`}>
              {title}
            </h3>
          )}

          {subtitle && (
            <p className="text-gray-500 text-sm mt-1">
              {subtitle}
            </p>
          )}

          {showDivider && (title || subtitle) && body && (
            <div className="w-12 h-px bg-gray-200 my-3"></div>
          )}

          {body && (
            <div
              className={`text-gray-600 leading-relaxed ${sizes.body}`}
              dangerouslySetInnerHTML={{ __html: body }}
            />
          )}

          {author && (
            <p className="text-gray-400 text-xs mt-3 uppercase tracking-wide">
              {author}
            </p>
          )}

          {showCta && ctaText && ctaUrl && (
            <a
              href={ctaUrl}
              className="inline-flex items-center text-gray-900 font-medium text-sm mt-4 group"
            >
              {ctaText}
              <svg
                className="ml-1 w-4 h-4 transform group-hover:translate-x-1 transition-transform"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 8l4 4m0 0l-4 4m4-4H3" />
              </svg>
            </a>
          )}
        </div>
      </div>
    </div>
  )
}

CardMinimal.propTypes = {
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
  bordered: PropTypes.bool,
  showDivider: PropTypes.bool,
  showCta: PropTypes.bool,
  showImage: PropTypes.bool,
  imagePosition: PropTypes.oneOf(['top', 'left', 'right']),
  className: PropTypes.string,
  onLoad: PropTypes.func,
  onError: PropTypes.func
}

export default CardMinimal
