import { useState, useEffect } from 'react'
import PropTypes from 'prop-types'

// BFF Content URL - the MFE knows its own backend
const BFF_CONTENT_URL = import.meta.env.VITE_BFF_CONTENT_URL || 'http://localhost:3240'

const variantStyles = {
  default: {
    container: 'bg-white',
    title: 'text-gray-900',
    body: 'text-gray-700'
  },
  card: {
    container: 'bg-white rounded-lg shadow-md border border-gray-200',
    title: 'text-gray-900',
    body: 'text-gray-600'
  },
  hero: {
    container: 'bg-gradient-to-r from-indigo-600 to-purple-600 text-white rounded-xl shadow-lg',
    title: 'text-white',
    body: 'text-indigo-100'
  },
  minimal: {
    container: 'bg-transparent',
    title: 'text-gray-800',
    body: 'text-gray-500'
  },
  dark: {
    container: 'bg-gray-900 rounded-lg',
    title: 'text-white',
    body: 'text-gray-300'
  },
  accent: {
    container: 'bg-indigo-50 border-l-4 border-indigo-500',
    title: 'text-indigo-900',
    body: 'text-indigo-700'
  }
}

const sizeStyles = {
  sm: {
    container: 'p-4',
    title: 'text-lg font-medium mb-2',
    body: 'text-sm leading-relaxed'
  },
  md: {
    container: 'p-6',
    title: 'text-xl font-semibold mb-3',
    body: 'text-base leading-relaxed'
  },
  lg: {
    container: 'p-8',
    title: 'text-2xl font-bold mb-4',
    body: 'text-lg leading-loose'
  },
  xl: {
    container: 'p-10',
    title: 'text-3xl font-bold mb-5',
    body: 'text-xl leading-loose'
  }
}

const alignmentStyles = {
  left: 'text-left',
  center: 'text-center',
  right: 'text-right'
}

/**
 * ContentBlock - A self-contained component that fetches and renders content blocks
 *
 * Usage:
 *   <ContentBlock code="hero-welcome" />                           // Fetch by code
 *   <ContentBlock code="hero-welcome" locale="es-ES" />            // Fetch with locale
 *   <ContentBlock code="hero-welcome" variant="dark" />            // Override variant
 *   <ContentBlock content={{title, body}} variant="card" />        // Direct content (legacy)
 *
 * API Response Structure:
 *   - block: template slug (e.g., "article", "card")
 *   - variant: selected variant slug (e.g., "hero", "dark")
 *   - sections: dictionary of all section values (e.g., { title: "...", body: "..." })
 *   - Plus flat fields for backwards compatibility (title, subtitle, body, etc.)
 */
function ContentBlock({
  code,
  locale = 'en-US',
  tenantId,
  variant: requestedVariant,
  content: directContent,
  size: overrideSize,
  alignment: overrideAlignment,
  className = '',
  showMedia = true,
  mediaPosition = 'top',
  onLoad,
  onError
}) {
  const [blockData, setBlockData] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  // Fetch content block when code changes
  useEffect(() => {
    if (code) {
      fetchContentBlock()
    }
  }, [code, locale, tenantId, requestedVariant])

  const fetchContentBlock = async () => {
    try {
      setLoading(true)
      setError(null)

      const params = new URLSearchParams({ localeCode: locale })
      if (tenantId) params.append('tenantId', tenantId)
      if (requestedVariant) params.append('variant', requestedVariant)

      const response = await fetch(`${BFF_CONTENT_URL}/api/content/blocks/code/${code}?${params}`)

      if (!response.ok) {
        throw new Error(`Content block "${code}" not found`)
      }

      const data = await response.json()
      setBlockData(data)
      onLoad?.(data)
    } catch (err) {
      console.error('ContentBlock fetch error:', err)
      setError(err.message)
      onError?.(err)
    } finally {
      setLoading(false)
    }
  }

  // Use direct content or fetched block data
  // The API returns both 'sections' dictionary and flat fields for backwards compatibility
  const content = directContent || (blockData ? {
    title: blockData.title || blockData.sections?.title,
    subtitle: blockData.subtitle || blockData.sections?.subtitle,
    body: blockData.body || blockData.sections?.body,
    ctaText: blockData.ctaText || blockData.sections?.['cta-text'],
    ctaUrl: blockData.ctaUrl || blockData.sections?.['cta-url'],
    imageUrl: blockData.imageUrl || blockData.sections?.['image-url'],
    author: blockData.author || blockData.sections?.author,
    // Expose all sections for custom rendering
    sections: blockData.sections || {}
  } : null)

  // Get variant from API response (which respects requested variant or uses default)
  // The API returns the actual variant being used in blockData.variant
  const variant = blockData?.variant || requestedVariant || 'default'
  const size = overrideSize || 'md'
  const alignment = overrideAlignment || 'left'

  // Loading state
  if (code && loading) {
    return (
      <div className={`animate-pulse ${className}`}>
        <div className="bg-gray-200 rounded-lg h-32"></div>
      </div>
    )
  }

  // Error state
  if (error) {
    return (
      <div className={`bg-red-50 border border-red-200 rounded-lg p-4 ${className}`}>
        <p className="text-red-600 text-sm">{error}</p>
      </div>
    )
  }

  // No content
  if (!content) {
    return null
  }

  const { title, subtitle, body, ctaText, ctaUrl, media } = content
  const vStyles = variantStyles[variant] || variantStyles.default
  const sStyles = sizeStyles[size] || sizeStyles.md
  const aStyles = alignmentStyles[alignment] || alignmentStyles.left

  const containerClasses = [
    vStyles.container,
    sStyles.container,
    aStyles,
    className
  ].filter(Boolean).join(' ')

  const renderMedia = () => {
    if (!showMedia || !media) return null

    const mediaClasses = alignment === 'center' ? 'mx-auto' : ''

    if (media.type === 'image') {
      return (
        <div className={`${mediaPosition === 'top' ? 'mb-4' : 'mt-4'}`}>
          <img
            src={media.url}
            alt={media.alt || title || 'Content media'}
            className={`max-w-full h-auto rounded ${mediaClasses}`}
            style={media.width ? { maxWidth: media.width } : {}}
          />
          {media.caption && (
            <p className="text-sm text-gray-500 mt-2 italic">{media.caption}</p>
          )}
        </div>
      )
    }

    if (media.type === 'video') {
      return (
        <div className={`${mediaPosition === 'top' ? 'mb-4' : 'mt-4'}`}>
          <video
            src={media.url}
            controls
            className={`max-w-full h-auto rounded ${mediaClasses}`}
            style={media.width ? { maxWidth: media.width } : {}}
          >
            Your browser does not support the video tag.
          </video>
          {media.caption && (
            <p className="text-sm text-gray-500 mt-2 italic">{media.caption}</p>
          )}
        </div>
      )
    }

    return null
  }

  return (
    <div className={containerClasses}>
      {mediaPosition === 'top' && renderMedia()}

      {title && (
        <h2 className={`${vStyles.title} ${sStyles.title}`}>
          {title}
        </h2>
      )}

      {subtitle && (
        <p className={`${vStyles.body} text-lg mb-2 opacity-80`}>
          {subtitle}
        </p>
      )}

      {body && (
        <div
          className={`${vStyles.body} ${sStyles.body} prose max-w-none`}
          dangerouslySetInnerHTML={{ __html: body }}
        />
      )}

      {ctaText && ctaUrl && (
        <div className="mt-4">
          <a
            href={ctaUrl}
            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
          >
            {ctaText}
          </a>
        </div>
      )}

      {mediaPosition === 'bottom' && renderMedia()}
    </div>
  )
}

ContentBlock.propTypes = {
  // Fetch mode - provide code to auto-fetch
  code: PropTypes.string,
  locale: PropTypes.string,
  tenantId: PropTypes.string,

  // Variant - passed to API, which returns content's default if not specified
  // The API determines the actual variant used (stored in blockData.variant)
  variant: PropTypes.string,

  // Direct content mode (legacy/override)
  content: PropTypes.shape({
    title: PropTypes.string,
    subtitle: PropTypes.string,
    body: PropTypes.string,
    ctaText: PropTypes.string,
    ctaUrl: PropTypes.string,
    imageUrl: PropTypes.string,
    author: PropTypes.string,
    sections: PropTypes.object,
    media: PropTypes.shape({
      type: PropTypes.oneOf(['image', 'video']),
      url: PropTypes.string.isRequired,
      alt: PropTypes.string,
      caption: PropTypes.string,
      width: PropTypes.string
    })
  }),

  // Style overrides
  size: PropTypes.oneOf(['sm', 'md', 'lg', 'xl']),
  alignment: PropTypes.oneOf(['left', 'center', 'right']),
  className: PropTypes.string,
  showMedia: PropTypes.bool,
  mediaPosition: PropTypes.oneOf(['top', 'bottom']),

  // Callbacks
  onLoad: PropTypes.func,
  onError: PropTypes.func
}

export default ContentBlock
