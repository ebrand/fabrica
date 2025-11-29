import PropTypes from 'prop-types'

/**
 * ArticleSummary - Condensed article card component
 * Used by Content factory when block="article" and variant="summary"
 */
function ArticleSummary({
  content,
  showAuthor = true,
  showImage = true,
  horizontal = false,
  className = ''
}) {
  if (!content) return null

  const { title, intro, subtitle, body, author, imageUrl, ctaText, ctaUrl } = content

  // Extract first paragraph as excerpt if no subtitle
  const excerpt = intro || subtitle || (body ? body.replace(/<[^>]*>/g, '').slice(0, 150) + '...' : '')

  if (horizontal) {
    return (
      <article className={`flex gap-4 ${className}`}>
        {showImage && imageUrl && (
          <div className="flex-shrink-0 w-32 h-32 sm:w-40 sm:h-40 rounded-lg overflow-hidden">
            <img
              src={imageUrl}
              alt={title || 'Article image'}
              className="w-full h-full object-cover"
            />
          </div>
        )}

        <div className="flex-1 min-w-0">
          {title && (
            <h3 className="text-lg font-semibold text-gray-900 mb-1 line-clamp-2">
              {ctaUrl ? (
                <a href={ctaUrl} className="hover:text-indigo-600 transition-colors">
                  {title}
                </a>
              ) : title}
            </h3>
          )}

          {excerpt && (
            <p className="text-gray-600 text-sm mb-2 line-clamp-2">
              {excerpt}
            </p>
          )}

          {showAuthor && author && (
            <p className="text-gray-400 text-xs">
              By {author}
            </p>
          )}
        </div>
      </article>
    )
  }

  return (
    <article className={`bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden ${className}`}>
      {showImage && imageUrl && (
        <div className="h-40 overflow-hidden">
          <img
            src={imageUrl}
            alt={title || 'Article image'}
            className="w-full h-full object-cover"
          />
        </div>
      )}

      <div className="p-4">
        {title && (
          <h3 className="text-lg font-semibold text-gray-900 mb-2 line-clamp-2">
            {ctaUrl ? (
              <a href={ctaUrl} className="hover:text-indigo-600 transition-colors">
                {title}
              </a>
            ) : title}
          </h3>
        )}

        {excerpt && (
          <p className="text-gray-600 text-sm mb-3 line-clamp-3">
            {excerpt}
          </p>
        )}

        <div className="flex items-center justify-between">
          {showAuthor && author && (
            <p className="text-gray-400 text-xs">
              By {author}
            </p>
          )}

          {ctaText && ctaUrl && (
            <a
              href={ctaUrl}
              className="text-indigo-600 text-sm font-medium hover:text-indigo-800 transition-colors"
            >
              {ctaText}
            </a>
          )}
        </div>
      </div>
    </article>
  )
}

ArticleSummary.propTypes = {
  content: PropTypes.shape({
    title    : PropTypes.string,
    subtitle : PropTypes.string,
    intro    : PropTypes.string,
    body     : PropTypes.string,
    author   : PropTypes.string,
    imageUrl : PropTypes.string,
    ctaText  : PropTypes.string,
    ctaUrl   : PropTypes.string
  }),
  showAuthor : PropTypes.bool,
  showImage  : PropTypes.bool,
  horizontal : PropTypes.bool,
  className  : PropTypes.string
}

export default ArticleSummary
