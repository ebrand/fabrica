import PropTypes from 'prop-types'

/**
 * CardDefault - Standard card component
 * Used by Content factory when block="card" and variant="default"
 */
function CardDefault({
  content,
  size = 'md',
  showCta = true,
  showImage = true,
  className = ''
}) {
  if (!content) return null

  const { title, subtitle, body, ctaText, ctaUrl, imageUrl, author } = content

  const sizeStyles = {
    sm: { container: 'p-4', title: 'text-lg', body: 'text-sm' },
    md: { container: 'p-6', title: 'text-xl', body: 'text-base' },
    lg: { container: 'p-8', title: 'text-2xl', body: 'text-lg' }
  }

  const sizes = sizeStyles[size] || sizeStyles.md

  return (
    <div className={`
      bg-white rounded-lg shadow-md border border-gray-200
      ${sizes.container} ${className}
    `}>
      {showImage && imageUrl && (
        <div className="-mx-6 -mt-6 mb-4 overflow-hidden rounded-t-lg">
          <img
            src={imageUrl}
            alt={title || 'Card image'}
            className="w-full h-48 object-cover"
          />
        </div>
      )}

      {title && (
        <h3 className={`font-semibold text-gray-900 mb-2 ${sizes.title}`}>
          {title}
        </h3>
      )}

      {subtitle && (
        <p className="text-gray-500 text-sm mb-3">
          {subtitle}
        </p>
      )}

      {body && (
        <div
          className={`text-gray-600 leading-relaxed mb-4 ${sizes.body}`}
          dangerouslySetInnerHTML={{ __html: body }}
        />
      )}

      {author && (
        <p className="text-gray-400 text-sm mb-4">
          By {author}
        </p>
      )}

      {showCta && ctaText && ctaUrl && (
        <a
          href={ctaUrl}
          className="inline-flex items-center px-4 py-2 bg-gray-900 text-white rounded-lg text-sm font-medium hover:bg-gray-800 transition-colors"
        >
          {ctaText}
        </a>
      )}
    </div>
  )
}

CardDefault.propTypes = {
  content: PropTypes.shape({
    title: PropTypes.string,
    subtitle: PropTypes.string,
    body: PropTypes.string,
    ctaText: PropTypes.string,
    ctaUrl: PropTypes.string,
    imageUrl: PropTypes.string,
    author: PropTypes.string
  }),
  size: PropTypes.oneOf(['sm', 'md', 'lg']),
  showCta: PropTypes.bool,
  showImage: PropTypes.bool,
  className: PropTypes.string
}

export default CardDefault
