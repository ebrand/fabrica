import { useState, useEffect } from 'react'
import PropTypes from 'prop-types'

const BFF_CONTENT_URL = import.meta.env.VITE_BFF_CONTENT_URL || 'http://localhost:3240'

/**
 * CardHero - A hero-style card with gradient background for prominent content
 *
 * Usage:
 *   <CardHero code="hero-welcome" />                       // Fetch by code
 *   <CardHero code="hero-welcome" locale="es-ES" />        // Fetch with locale
 *   <CardHero content={{title, body}} />                   // Direct content
 *   <CardHero code="hero-welcome" gradient="sunset" />     // Custom gradient
 *   <CardHero code="hero-welcome" fullWidth />             // Full-width hero
 */
function CardHero({
  code,
  locale = 'en-US',
  tenantId,
  content: directContent,
  size = 'lg',
  gradient = 'indigo',
  fullWidth = false,
  centered = true,
  showCta = true,
  showImage = true,
  overlayImage = false,
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
      params.append('variant', 'hero')

      const response = await fetch(`${BFF_CONTENT_URL}/api/content/blocks/code/${code}?${params}`)

      if (!response.ok) {
        throw new Error(`Content "${code}" not found`)
      }

      const data = await response.json()
      setBlockData(data)
      onLoad?.(data)
    } catch (err) {
      console.error('CardHero fetch error:', err)
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
    sm: { container: 'py-8 px-6', title: 'text-2xl', body: 'text-base', maxWidth: 'max-w-xl' },
    md: { container: 'py-12 px-8', title: 'text-3xl', body: 'text-lg', maxWidth: 'max-w-2xl' },
    lg: { container: 'py-16 px-10', title: 'text-4xl', body: 'text-lg', maxWidth: 'max-w-3xl' },
    xl: { container: 'py-20 px-12', title: 'text-5xl', body: 'text-xl', maxWidth: 'max-w-4xl' }
  }

  const gradients = {
    indigo: 'bg-gradient-to-r from-indigo-600 to-purple-600',
    blue: 'bg-gradient-to-r from-blue-600 to-cyan-500',
    sunset: 'bg-gradient-to-r from-orange-500 via-pink-500 to-purple-600',
    ocean: 'bg-gradient-to-r from-teal-500 to-blue-600',
    forest: 'bg-gradient-to-r from-green-600 to-teal-500',
    fire: 'bg-gradient-to-r from-red-600 via-orange-500 to-yellow-500',
    night: 'bg-gradient-to-r from-gray-900 via-purple-900 to-gray-900',
    aurora: 'bg-gradient-to-r from-green-400 via-blue-500 to-purple-600'
  }

  const sizes = sizeStyles[size] || sizeStyles.lg
  const gradientClass = gradients[gradient] || gradients.indigo

  if (code && loading) {
    return (
      <div className={`
        animate-pulse ${gradientClass} rounded-2xl
        ${sizes.container} ${className}
      `}>
        <div className={`${centered ? 'mx-auto text-center' : ''} ${sizes.maxWidth}`}>
          <div className="h-10 bg-white/20 rounded-lg w-3/4 mx-auto mb-4"></div>
          <div className="h-6 bg-white/10 rounded w-full mb-2"></div>
          <div className="h-6 bg-white/10 rounded w-5/6 mx-auto"></div>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className={`
        ${gradientClass} rounded-2xl ${sizes.container} ${className}
      `}>
        <p className="text-white/80 text-center">{error}</p>
      </div>
    )
  }

  if (!content) return null

  const { title, subtitle, body, ctaText, ctaUrl, imageUrl } = content
  const hasBackgroundImage = showImage && imageUrl && overlayImage

  return (
    <div
      className={`
        relative overflow-hidden
        ${fullWidth ? '' : 'rounded-2xl'}
        ${hasBackgroundImage ? '' : gradientClass}
        shadow-xl ${className}
      `}
      style={hasBackgroundImage ? {
        backgroundImage: `url(${imageUrl})`,
        backgroundSize: 'cover',
        backgroundPosition: 'center'
      } : {}}
    >
      {hasBackgroundImage && (
        <div className={`absolute inset-0 ${gradientClass} opacity-80`}></div>
      )}

      <div className={`relative ${sizes.container}`}>
        <div className={`
          ${centered ? 'mx-auto text-center' : ''}
          ${sizes.maxWidth}
        `}>
          {subtitle && (
            <p className="text-white/80 text-sm font-medium uppercase tracking-wider mb-3">
              {subtitle}
            </p>
          )}

          {title && (
            <h1 className={`font-bold text-white mb-4 ${sizes.title} leading-tight`}>
              {title}
            </h1>
          )}

          {body && (
            <div
              className={`text-white/90 leading-relaxed mb-6 ${sizes.body}`}
              dangerouslySetInnerHTML={{ __html: body }}
            />
          )}

          {showCta && ctaText && ctaUrl && (
            <div className={`flex ${centered ? 'justify-center' : ''} gap-4 mt-8`}>
              <a
                href={ctaUrl}
                className="
                  inline-flex items-center px-6 py-3 rounded-lg
                  bg-white text-gray-900 font-semibold
                  hover:bg-gray-100 transition-colors duration-200
                  shadow-lg hover:shadow-xl
                "
              >
                {ctaText}
                <svg className="ml-2 w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7l5 5m0 0l-5 5m5-5H6" />
                </svg>
              </a>
            </div>
          )}
        </div>

        {showImage && imageUrl && !overlayImage && (
          <div className="mt-8 flex justify-center">
            <img
              src={imageUrl}
              alt={title || 'Hero image'}
              className="max-w-md w-full rounded-lg shadow-2xl"
            />
          </div>
        )}
      </div>

      <div className="absolute top-0 right-0 -mt-20 -mr-20 w-80 h-80 bg-white/5 rounded-full blur-3xl"></div>
      <div className="absolute bottom-0 left-0 -mb-20 -ml-20 w-60 h-60 bg-white/5 rounded-full blur-3xl"></div>
    </div>
  )
}

CardHero.propTypes = {
  code: PropTypes.string,
  locale: PropTypes.string,
  tenantId: PropTypes.string,
  content: PropTypes.shape({
    title: PropTypes.string,
    subtitle: PropTypes.string,
    body: PropTypes.string,
    ctaText: PropTypes.string,
    ctaUrl: PropTypes.string,
    imageUrl: PropTypes.string
  }),
  size: PropTypes.oneOf(['sm', 'md', 'lg', 'xl']),
  gradient: PropTypes.oneOf(['indigo', 'blue', 'sunset', 'ocean', 'forest', 'fire', 'night', 'aurora']),
  fullWidth: PropTypes.bool,
  centered: PropTypes.bool,
  showCta: PropTypes.bool,
  showImage: PropTypes.bool,
  overlayImage: PropTypes.bool,
  className: PropTypes.string,
  onLoad: PropTypes.func,
  onError: PropTypes.func
}

export default CardHero
