import PropTypes from 'prop-types'

/**
 * CardAccent - Accented sidebar card component
 * Used by Content factory when block="card" and variant="accent"
 */
function CardAccent({
  content,
  size = 'md',
  accentColor = 'indigo',
  showCta = true,
  className = ''
}) {
  if (!content) return null

  const { title, subtitle, body, ctaText, ctaUrl, author } = content

  const sizeStyles = {
    sm: { container: 'p-4', title: 'text-base', body: 'text-sm' },
    md: { container: 'p-5', title: 'text-lg', body: 'text-base' },
    lg: { container: 'p-6', title: 'text-xl', body: 'text-base' }
  }

  const accentColors = {
    indigo: {
      bg: 'bg-indigo-50',
      border: 'border-indigo-500',
      title: 'text-indigo-900',
      body: 'text-indigo-700',
      link: 'text-indigo-600 hover:text-indigo-800'
    },
    blue: {
      bg: 'bg-blue-50',
      border: 'border-blue-500',
      title: 'text-blue-900',
      body: 'text-blue-700',
      link: 'text-blue-600 hover:text-blue-800'
    },
    green: {
      bg: 'bg-green-50',
      border: 'border-green-500',
      title: 'text-green-900',
      body: 'text-green-700',
      link: 'text-green-600 hover:text-green-800'
    },
    amber: {
      bg: 'bg-amber-50',
      border: 'border-amber-500',
      title: 'text-amber-900',
      body: 'text-amber-700',
      link: 'text-amber-600 hover:text-amber-800'
    },
    rose: {
      bg: 'bg-rose-50',
      border: 'border-rose-500',
      title: 'text-rose-900',
      body: 'text-rose-700',
      link: 'text-rose-600 hover:text-rose-800'
    }
  }

  const sizes = sizeStyles[size] || sizeStyles.md
  const accent = accentColors[accentColor] || accentColors.indigo

  return (
    <div className={`
      ${accent.bg} border-l-4 ${accent.border} rounded-r-lg
      ${sizes.container} ${className}
    `}>
      {subtitle && (
        <p className={`text-xs font-semibold uppercase tracking-wider mb-1 ${accent.body} opacity-70`}>
          {subtitle}
        </p>
      )}

      {title && (
        <h3 className={`font-bold ${accent.title} mb-2 ${sizes.title}`}>
          {title}
        </h3>
      )}

      {body && (
        <div
          className={`${accent.body} leading-relaxed ${sizes.body}`}
          dangerouslySetInnerHTML={{ __html: body }}
        />
      )}

      {author && (
        <p className={`${accent.body} text-sm mt-3 opacity-70`}>
          — {author}
        </p>
      )}

      {showCta && ctaText && ctaUrl && (
        <a
          href={ctaUrl}
          className={`inline-flex items-center font-medium text-sm mt-4 ${accent.link} transition-colors`}
        >
          {ctaText}
          <svg className="ml-1 w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
          </svg>
        </a>
      )}
    </div>
  )
}

CardAccent.propTypes = {
  content: PropTypes.shape({
    title: PropTypes.string,
    subtitle: PropTypes.string,
    body: PropTypes.string,
    ctaText: PropTypes.string,
    ctaUrl: PropTypes.string,
    author: PropTypes.string
  }),
  size: PropTypes.oneOf(['sm', 'md', 'lg']),
  accentColor: PropTypes.oneOf(['indigo', 'blue', 'green', 'amber', 'rose']),
  showCta: PropTypes.bool,
  className: PropTypes.string
}

export default CardAccent
