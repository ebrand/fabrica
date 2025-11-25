import PropTypes from 'prop-types'

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

function ContentBlock({
  content,
  variant = 'default',
  size = 'md',
  alignment = 'left',
  className = '',
  showMedia = true,
  mediaPosition = 'top'
}) {
  if (!content) {
    return null
  }

  const { title, body, media } = content
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

      {body && (
        <div
          className={`${vStyles.body} ${sStyles.body} prose max-w-none`}
          dangerouslySetInnerHTML={{ __html: body }}
        />
      )}

      {mediaPosition === 'bottom' && renderMedia()}
    </div>
  )
}

ContentBlock.propTypes = {
  content: PropTypes.shape({
    title: PropTypes.string,
    body: PropTypes.string,
    media: PropTypes.shape({
      type: PropTypes.oneOf(['image', 'video']),
      url: PropTypes.string.isRequired,
      alt: PropTypes.string,
      caption: PropTypes.string,
      width: PropTypes.string
    })
  }),
  variant: PropTypes.oneOf(['default', 'card', 'hero', 'minimal', 'dark', 'accent']),
  size: PropTypes.oneOf(['sm', 'md', 'lg', 'xl']),
  alignment: PropTypes.oneOf(['left', 'center', 'right']),
  className: PropTypes.string,
  showMedia: PropTypes.bool,
  mediaPosition: PropTypes.oneOf(['top', 'bottom'])
}

export default ContentBlock
