import { useState, useEffect } from 'react'
import ContentBlock from './components/ContentBlock'

function App() {
  const [blocks, setBlocks] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [locale, setLocale] = useState('en-US')

  const BFF_URL = import.meta.env.VITE_BFF_URL || 'http://localhost:3240'

  useEffect(() => {
    fetchContentBlocks()
  }, [locale])

  const fetchContentBlocks = async () => {
    try {
      setLoading(true)
      const response = await fetch(`${BFF_URL}/api/content/blocks?localeCode=${locale}`)
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }
      const data = await response.json()
      setBlocks(data)
      setError(null)
    } catch (err) {
      console.error('Error fetching content blocks:', err)
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  // Map database block to ContentBlock component format
  const mapBlockToContent = (block) => ({
    title: block.translation?.title || block.title,
    subtitle: block.translation?.subtitle || block.subtitle,
    body: block.translation?.body || block.body,
    ctaText: block.translation?.ctaText || block.ctaText,
    ctaUrl: block.translation?.ctaUrl || block.ctaUrl,
    media: null
  })

  // Map blockType to variant
  const getVariant = (blockType) => {
    const variantMap = {
      'hero': 'hero',
      'card': 'card',
      'accent': 'accent',
      'minimal': 'minimal',
      'dark': 'dark'
    }
    return variantMap[blockType] || 'default'
  }

  return (
    <div className="min-h-screen bg-gray-100 p-8">
      <div className="max-w-4xl mx-auto space-y-8">
        <div className="flex justify-between items-center">
          <h1 className="text-3xl font-bold text-gray-900">Content MFE - Content Blocks</h1>
          <div className="flex items-center gap-2">
            <label className="text-sm text-gray-600">Locale:</label>
            <select
              value={locale}
              onChange={(e) => setLocale(e.target.value)}
              className="border rounded px-3 py-1 text-sm"
            >
              <option value="en-US">English (US)</option>
              <option value="es-ES">Spanish (ES)</option>
            </select>
            <button
              onClick={fetchContentBlocks}
              className="ml-2 px-3 py-1 bg-indigo-600 text-white rounded text-sm hover:bg-indigo-700"
            >
              Refresh
            </button>
          </div>
        </div>

        {loading && (
          <div className="text-center py-8">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600 mx-auto"></div>
            <p className="text-gray-500 mt-2">Loading content blocks...</p>
          </div>
        )}

        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4">
            <p className="text-red-600">Error: {error}</p>
            <p className="text-sm text-red-500 mt-1">Make sure bff-content is running on {BFF_URL}</p>
          </div>
        )}

        {!loading && !error && blocks.length === 0 && (
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
            <p className="text-yellow-700">No content blocks found. Create some in the database!</p>
          </div>
        )}

        {!loading && !error && blocks.length > 0 && (
          <div className="space-y-6">
            <p className="text-sm text-gray-500">Showing {blocks.length} content block(s) in {locale}</p>

            {blocks.map((block) => (
              <div key={block.id} className="space-y-2">
                <div className="flex justify-between items-center text-xs text-gray-400">
                  <span>Code: {block.code}</span>
                  <span>Type: {block.blockType}</span>
                </div>
                <ContentBlock
                  content={mapBlockToContent(block)}
                  variant={getVariant(block.blockType)}
                  size="md"
                />
              </div>
            ))}
          </div>
        )}

        <hr className="border-gray-200" />

        <div className="space-y-4">
          <h2 className="text-xl font-semibold text-gray-700">Static Preview Examples</h2>
          <p className="text-sm text-gray-500">These are hardcoded examples showing different variants:</p>

          <div className="grid grid-cols-2 gap-4">
            <ContentBlock
              content={{ title: 'Default', body: '<p>Default variant example</p>' }}
              variant="default"
              size="sm"
            />
            <ContentBlock
              content={{ title: 'Card', body: '<p>Card variant example</p>' }}
              variant="card"
              size="sm"
            />
            <ContentBlock
              content={{ title: 'Hero', body: '<p>Hero variant example</p>' }}
              variant="hero"
              size="sm"
            />
            <ContentBlock
              content={{ title: 'Accent', body: '<p>Accent variant example</p>' }}
              variant="accent"
              size="sm"
            />
            <ContentBlock
              content={{ title: 'Minimal', body: '<p>Minimal variant example</p>' }}
              variant="minimal"
              size="sm"
            />
            <ContentBlock
              content={{ title: 'Dark', body: '<p>Dark variant example</p>' }}
              variant="dark"
              size="sm"
            />
          </div>
        </div>
      </div>
    </div>
  )
}

export default App
