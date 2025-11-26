import { useState, useEffect, lazy, Suspense } from 'react'
import PropTypes from 'prop-types'

const BFF_CONTENT_URL = import.meta.env.VITE_BFF_CONTENT_URL || 'http://localhost:3240'

// Component registry: maps block/variant combinations to components
// Lazy load components for code splitting
const componentRegistry = {
  // Card variants
  'card/default': lazy(() => import('./CardDefault')),
  'card/hero': lazy(() => import('./CardHero')),
  'card/minimal': lazy(() => import('./CardMinimal')),
  'card/dark': lazy(() => import('./CardDark')),
  'card/accent': lazy(() => import('./CardAccent')),

  // Article variants
  'article/long-form': lazy(() => import('./ArticleLongForm')),
  'article/summary': lazy(() => import('./ArticleSummary')),

  // Carousel variants (future)
  // 'carousel/auto-navigate': lazy(() => import('./CarouselAuto')),
  // 'carousel/manual': lazy(() => import('./CarouselManual')),

  // Fallback
  'fallback': lazy(() => import('./ContentBlock'))
}

/**
 * Content - Factory component that renders the appropriate block variant
 *
 * Usage:
 *   <Content content="welcome" />                    // Uses default variant from DB
 *   <Content content="welcome" variant="hero" />    // Override variant
 *   <Content content="note-01" variant="minimal" /> // Renders CardMinimal
 *   <Content content="prod-carousel-01" variant="auto-navigate" /> // Renders CarouselAuto
 *
 * The component automatically:
 *   1. Fetches content by slug
 *   2. Determines block type (card, article, carousel, etc.)
 *   3. Resolves variant (passed prop or default_variant_id)
 *   4. Renders the appropriate component from the registry
 */
function Content({
  content: contentSlug,
  variant: requestedVariant,
  locale = 'en-US',
  tenantId,
  className = '',
  // Pass-through props for the rendered component
  ...componentProps
}) {
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (contentSlug) {
      fetchContent()
    }
  }, [contentSlug, locale, tenantId, requestedVariant])

  const fetchContent = async () => {
    try {
      setLoading(true)
      setError(null)

      const params = new URLSearchParams({ localeCode: locale })
      if (tenantId) params.append('tenantId', tenantId)
      if (requestedVariant) params.append('variant', requestedVariant)

      const response = await fetch(`${BFF_CONTENT_URL}/api/content/blocks/code/${contentSlug}?${params}`)

      if (!response.ok) {
        throw new Error(`Content "${contentSlug}" not found`)
      }

      const result = await response.json()
      setData(result)
    } catch (err) {
      console.error('Content fetch error:', err)
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  // Loading state
  if (loading) {
    return (
      <div className={`animate-pulse bg-gray-200 rounded-lg h-32 ${className}`} />
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

  // No data
  if (!data) return null

  // Determine block type and variant
  const block = data.block?.toLowerCase() || 'card'
  const variant = data.variant?.toLowerCase() || 'default'
  const registryKey = `${block}/${variant}`

  // Get component from registry, fallback to generic ContentBlock
  const Component = componentRegistry[registryKey] || componentRegistry['fallback']

  // Build content object for the component
  const contentData = {
    title: data.title || data.sections?.title,
    subtitle: data.subtitle || data.sections?.subtitle,
    body: data.body || data.sections?.body,
    ctaText: data.ctaText || data.sections?.['cta-text'],
    ctaUrl: data.ctaUrl || data.sections?.['cta-url'],
    imageUrl: data.imageUrl || data.sections?.['image-url'],
    author: data.author || data.sections?.author,
    sections: data.sections || {}
  }

  return (
    <Suspense fallback={<div className={`animate-pulse bg-gray-200 rounded-lg h-32 ${className}`} />}>
      <Component
        content={contentData}
        className={className}
        {...componentProps}
      />
    </Suspense>
  )
}

Content.propTypes = {
  content: PropTypes.string.isRequired,
  variant: PropTypes.string,
  locale: PropTypes.string,
  tenantId: PropTypes.string,
  className: PropTypes.string
}

export default Content
