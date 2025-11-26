import PropTypes from 'prop-types'

/**
 * ArticleLongForm - Full article display component
 * Used by Content factory when block="article" and variant="long-form"
 */
function ArticleLongForm({
  content,
  showAuthor = true,
  showImage = true,
  className = ''
}) {
  if (!content) return null

  const { title, subtitle, body, author, imageUrl } = content

  return (
    <article className={`max-w-3xl mx-auto ${className}`}>
      {showImage && imageUrl && (
        <div className="mb-8 -mx-4 sm:mx-0 sm:rounded-xl overflow-hidden">
          <img
            src={imageUrl}
            alt={title || 'Article image'}
            className="w-full h-64 sm:h-96 object-cover"
          />
        </div>
      )}

      <header className="mb-8">
        {title && (
          <h1 className="text-3xl sm:text-4xl font-bold text-gray-900 leading-tight mb-4">
            {title}
          </h1>
        )}

        {subtitle && (
          <p className="text-xl text-gray-600 leading-relaxed">
            {subtitle}
          </p>
        )}

        {showAuthor && author && (
          <div className="flex items-center mt-6 pt-6 ">
            <div className="w-10 h-10 rounded-full bg-gray-300 flex items-center justify-center">
              <span className="text-gray-600 font-medium text-sm">
                {author.charAt(0).toUpperCase()}
              </span>
            </div>
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-900">{author}</p>
              <p className="text-sm text-gray-500">Author</p>
            </div>
          </div>
        )}
      </header>

      {body && (
        <div
          className="prose prose-lg max-w-none text-gray-700 leading-[2.0]"
          dangerouslySetInnerHTML={{ __html: body }}
        />
      )}
    </article>
  )
}

ArticleLongForm.propTypes = {
  content: PropTypes.shape({
    title   : PropTypes.string,
    subtitle: PropTypes.string,
    body    : PropTypes.string,
    author  : PropTypes.string,
    imageUrl: PropTypes.string
  }),
  showAuthor: PropTypes.bool,
  showImage : PropTypes.bool,
  className : PropTypes.string
}

export default ArticleLongForm
