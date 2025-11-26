import { useState, useEffect, lazy, Suspense } from 'react';

const BFF_URL = import.meta.env.VITE_BFF_URL || 'http://localhost:3240';

// Import Select from common MFE
const Select = lazy(() => import('commonMfe/Select'));

// Import content variant components for preview
const CardDefault = lazy(() => import('./CardDefault'));
const CardHero = lazy(() => import('./CardHero'));
const CardMinimal = lazy(() => import('./CardMinimal'));
const CardDark = lazy(() => import('./CardDark'));
const CardAccent = lazy(() => import('./CardAccent'));
const ArticleLongForm = lazy(() => import('./ArticleLongForm'));
const ArticleSummary = lazy(() => import('./ArticleSummary'));
const ContentBlock = lazy(() => import('./ContentBlock'));

// Preview component registry
const previewRegistry = {
  'card/default': CardDefault,
  'card/hero': CardHero,
  'card/minimal': CardMinimal,
  'card/dark': CardDark,
  'card/accent': CardAccent,
  'article/long-form': ArticleLongForm,
  'article/summary': ArticleSummary,
  'fallback': ContentBlock
};

/**
 * BlockContentEditor - Form component for creating/editing block content
 *
 * Props:
 * - contentId: UUID (optional) - If provided, loads existing content for editing
 * - blockId: UUID (required for create) - The block template to use
 * - onSave: function(content) - Called after successful save
 * - onCancel: function() - Called when user cancels
 * - defaultLocale: string - Default locale code (default: 'en-US')
 */
export default function BlockContentEditor({
  contentId,
  blockId,
  onSave,
  onCancel,
  defaultLocale = 'en-US'
}) {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);

  // Block template data
  const [block, setBlock] = useState(null);
  const [languages, setLanguages] = useState([]);
  const [selectedLocale, setSelectedLocale] = useState(defaultLocale);

  // Form state
  const [formData, setFormData] = useState({
    name: '',
    slug: '',
    description: '',
    defaultVariantId: null,
    accessControl: 'everyone',
    visibility: 'public',
    isActive: true
  });

  // Translations state: { [localeCode]: { [sectionSlug]: value } }
  const [translations, setTranslations] = useState({});

  // Modal state for rich text editing
  const [modalField, setModalField] = useState(null);

  // Preview state
  const [previewVariant, setPreviewVariant] = useState(null);

  // Load data on mount
  useEffect(() => {
    loadData();
  }, [contentId, blockId]);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);

      if (contentId) {
        // Edit mode - load existing content
        const response = await fetch(`${BFF_URL}/api/content/blocks/${contentId}/edit`);
        if (!response.ok) {
          const err = await response.json();
          throw new Error(err.error || 'Failed to load content');
        }
        const data = await response.json();

        setBlock(data.block);
        setLanguages(data.languages || []);
        setFormData({
          name: data.name || '',
          slug: data.slug || '',
          description: data.description || '',
          defaultVariantId: data.defaultVariantId || null,
          accessControl: data.accessControl || 'everyone',
          visibility: data.visibility || 'public',
          isActive: data.isActive ?? true
        });
        setTranslations(data.translations || {});

        // Set default locale if we have languages
        if (data.languages?.length > 0) {
          const defaultLang = data.languages.find(l => l.isDefault) || data.languages[0];
          setSelectedLocale(defaultLang.localeCode);
        }
      } else if (blockId) {
        // Create mode - load block template and languages
        const [blockRes, langRes] = await Promise.all([
          fetch(`${BFF_URL}/api/content/block-templates`),
          fetch(`${BFF_URL}/api/content/languages`)
        ]);

        if (!blockRes.ok) throw new Error('Failed to load block templates');
        if (!langRes.ok) throw new Error('Failed to load languages');

        const blocks = await blockRes.json();
        const langs = await langRes.json();

        const selectedBlock = blocks.find(b => b.blockId === blockId);
        if (!selectedBlock) {
          throw new Error('Block template not found');
        }

        setBlock(selectedBlock);
        setLanguages(langs);

        // Initialize empty translations for default language
        const defaultLang = langs.find(l => l.isDefault) || langs[0];
        if (defaultLang) {
          setSelectedLocale(defaultLang.localeCode);
          initializeTranslationsForLocale(defaultLang.localeCode, selectedBlock.sections || []);
        }

        // Set default variant if block has one
        const defaultVariant = selectedBlock.variants?.find(v => v.isDefault);
        if (defaultVariant) {
          setFormData(prev => ({ ...prev, defaultVariantId: defaultVariant.variantId }));
        }
      } else {
        throw new Error('Either contentId or blockId must be provided');
      }
    } catch (err) {
      console.error('Error loading data:', err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const initializeTranslationsForLocale = (localeCode, sections) => {
    setTranslations(prev => {
      if (prev[localeCode]) return prev;
      const sectionValues = {};
      sections.forEach(section => {
        sectionValues[section.slug] = section.defaultValue || '';
      });
      return { ...prev, [localeCode]: sectionValues };
    });
  };

  const handleFormChange = (field, value) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleNameChange = (value) => {
    setFormData(prev => {
      const newSlug = !prev.slug || prev.slug === generateSlug(prev.name)
        ? generateSlug(value)
        : prev.slug;
      return { ...prev, name: value, slug: newSlug };
    });
  };

  const generateSlug = (name) => {
    return name
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
  };

  const handleSectionChange = (sectionSlug, value) => {
    setTranslations(prev => ({
      ...prev,
      [selectedLocale]: {
        ...(prev[selectedLocale] || {}),
        [sectionSlug]: value
      }
    }));
  };

  const handleLocaleChange = (localeCode) => {
    setSelectedLocale(localeCode);
    // Initialize translations for this locale if not exists
    if (!translations[localeCode] && block?.sections) {
      initializeTranslationsForLocale(localeCode, block.sections);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim() || !formData.slug.trim()) {
      setError('Name and slug are required');
      return;
    }

    try {
      setSaving(true);
      setError(null);

      // Build translations array for API
      const translationsArray = Object.entries(translations).map(([localeCode, sections]) => ({
        localeCode,
        sections
      }));

      const payload = {
        ...formData,
        blockId: block?.blockId,
        translations: translationsArray
      };

      let response;
      if (contentId) {
        // Update existing
        response = await fetch(`${BFF_URL}/api/content/blocks/${contentId}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload)
        });
      } else {
        // Create new
        response = await fetch(`${BFF_URL}/api/content/blocks`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload)
        });
      }

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.error || 'Failed to save content');
      }

      const result = await response.json();
      onSave?.(result);
    } catch (err) {
      console.error('Error saving content:', err);
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  // Check if field type needs a modal editor
  const needsModalEditor = (fieldType) => {
    return ['longtext', 'richtext', 'textarea'].includes(fieldType);
  };

  // Render field based on fieldType
  const renderField = (section) => {
    const value = translations[selectedLocale]?.[section.slug] || '';
    const fieldId = `section-${section.slug}`;
    const showModalButton = needsModalEditor(section.fieldType);

    // For fields that need modal editing, show read-only input with edit button
    if (showModalButton) {
      return (
        <div className="flex">
          <input
            type="text"
            id={fieldId}
            value={value}
            readOnly
            className="flex-1 px-3 py-2 border border-gray-300 rounded-l-lg border-r-0 bg-white text-gray-400 cursor-pointer"
            placeholder={`Click to edit ${section.name.toLowerCase()}...`}
            onClick={() => setModalField(section)}
          />
          <button
            type="button"
            onClick={() => setModalField(section)}
            className="px-3 py-2 border border-gray-300 rounded-r-lg bg-gray-100 hover:bg-gray-200 transition-colors cursor-pointer"
            title="Edit in modal"
          >
            <svg className="w-5 h-5 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 12h.01M12 12h.01M19 12h.01M6 12a1 1 0 11-2 0 1 1 0 012 0zm7 0a1 1 0 11-2 0 1 1 0 012 0zm7 0a1 1 0 11-2 0 1 1 0 012 0z" />
            </svg>
          </button>
        </div>
      );
    }

    switch (section.fieldType) {
      case 'url':
        return (
          <input
            type="url"
            id={fieldId}
            value={value}
            onChange={(e) => handleSectionChange(section.slug, e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            placeholder="https://..."
          />
        );

      case 'image':
        return (
          <div className="flex gap-2">
            <input
              type="url"
              id={fieldId}
              value={value}
              onChange={(e) => handleSectionChange(section.slug, e.target.value)}
              className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Image URL..."
            />
            {value && (
              <img
                src={value}
                alt="Preview"
                className="h-10 w-10 object-cover rounded border"
                onError={(e) => e.target.style.display = 'none'}
              />
            )}
          </div>
        );

      case 'date':
        return (
          <input
            type="date"
            id={fieldId}
            value={value}
            onChange={(e) => handleSectionChange(section.slug, e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />
        );

      default: // text
        return (
          <input
            type="text"
            id={fieldId}
            value={value}
            onChange={(e) => handleSectionChange(section.slug, e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            placeholder={`Enter ${section.name.toLowerCase()}...`}
          />
        );
    }
  };

  // Render modal editor content based on field type
  const renderModalEditor = () => {
    if (!modalField) return null;
    const value = translations[selectedLocale]?.[modalField.slug] || '';

    return (
      <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
        <div className="bg-white rounded-xl shadow-xl w-full max-w-2xl mx-4 max-h-[80vh] flex flex-col">
          {/* Modal Header */}
          <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
            <div>
              <h3 className="text-lg font-semibold text-gray-900">{modalField.name}</h3>
              <p className="text-sm text-gray-500">
                {modalField.fieldType === 'richtext' ? 'Rich text editor (Markdown/HTML supported)' :
                 modalField.fieldType === 'longtext' ? 'Long text area' : 'Text area'}
              </p>
            </div>
            <button
              type="button"
              onClick={() => setModalField(null)}
              className="text-gray-400 hover:text-gray-600"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          {/* Modal Content */}
          <div className="p-6 flex-1 overflow-y-auto">
            <textarea
              value={value}
              onChange={(e) => handleSectionChange(modalField.slug, e.target.value)}
              rows={12}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 font-mono text-sm resize-none"
              placeholder={`Enter ${modalField.name.toLowerCase()}...`}
              autoFocus
            />
            {modalField.fieldType === 'richtext' && (
              <p className="mt-2 text-xs text-gray-400">
                Supports Markdown formatting: **bold**, *italic*, [links](url), # headings
              </p>
            )}
          </div>

          {/* Modal Footer */}
          <div className="px-6 py-4 border-t border-gray-200 flex justify-end">
            <button
              type="button"
              onClick={() => setModalField(null)}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
            >
              Done
            </button>
          </div>
        </div>
      </div>
    );
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center p-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        <span className="ml-3 text-gray-600">Loading...</span>
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto">
      {/* Error display */}
      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
          <div className="flex items-center">
            <svg className="w-5 h-5 text-red-400 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span className="text-sm text-red-700">{error}</span>
            <button onClick={() => setError(null)} className="ml-auto text-red-400 hover:text-red-600">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Left Panel - Block Metadata */}
          <div className="bg-white border border-gray-300 rounded-xl p-6">
            {/* Block Header */}
            <div className="text-center mb-6 pb-4 border-b border-gray-300">
              <p className="text-sm text-gray-600">
                Block: <span className="font-semibold text-gray-900">{block?.name}</span>
                <span className="ml-1 text-gray-500">({block?.slug})</span>
              </p>
            </div>

            <div className="space-y-4">
              {/* Name */}
              <div>
                <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
                  Name
                </label>
                <input
                  type="text"
                  id="name"
                  value={formData.name}
                  onChange={(e) => handleNameChange(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg bg-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  placeholder="Content name"
                  required
                />
              </div>

              {/* Slug */}
              <div>
                <label htmlFor="slug" className="block text-sm font-medium text-gray-700 mb-1">
                  Slug
                </label>
                <input
                  type="text"
                  id="slug"
                  value={formData.slug}
                  onChange={(e) => handleFormChange('slug', generateSlug(e.target.value))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg bg-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 font-mono text-sm"
                  placeholder="content-slug"
                  required
                />
              </div>

              {/* Description */}
              <div>
                <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
                  Description
                </label>
                <input
                  type="text"
                  id="description"
                  value={formData.description}
                  onChange={(e) => handleFormChange('description', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg bg-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  placeholder="Brief description..."
                />
              </div>

              {/* Default Variant */}
              {block?.variants?.length > 0 && (
                <Suspense fallback={<div className="h-10 bg-gray-100 rounded-lg animate-pulse" />}>
                  <Select
                    label="Default variant"
                    options={block.variants}
                    value={block.variants.find(v => v.variantId === formData.defaultVariantId) || null}
                    onChange={(v) => handleFormChange('defaultVariantId', v?.variantId || null)}
                    displayKey="name"
                    valueKey="variantId"
                    placeholder="-- Select variant --"
                  />
                </Suspense>
              )}

              {/* Access Control */}
              <Suspense fallback={<div className="h-10 bg-gray-100 rounded-lg animate-pulse" />}>
                <Select
                  label="Access control"
                  options={[
                    { id: 'everyone', name: 'Everyone' },
                    { id: 'authenticated', name: 'Authenticated Users' },
                    { id: 'admin', name: 'Admin Only' }
                  ]}
                  value={{ id: formData.accessControl, name: formData.accessControl }}
                  onChange={(v) => handleFormChange('accessControl', v?.id || 'everyone')}
                  placeholder="Select access level"
                />
              </Suspense>

              {/* Status (Visibility) */}
              <Suspense fallback={<div className="h-10 bg-gray-100 rounded-lg animate-pulse" />}>
                <Select
                  label="Status"
                  options={[
                    { id: 'public', name: 'Published' },
                    { id: 'draft', name: 'Draft' },
                    { id: 'archived', name: 'Archived' }
                  ]}
                  value={{ id: formData.visibility, name: formData.visibility }}
                  onChange={(v) => handleFormChange('visibility', v?.id || 'public')}
                  placeholder="Select status"
                />
              </Suspense>

              {/* Active Checkbox */}
              <div className="pt-2">
                <label className="flex items-center gap-2 cursor-pointer">
                  <span className="text-sm font-medium text-gray-700">Active</span>
                  <input
                    type="checkbox"
                    checked={formData.isActive}
                    onChange={(e) => handleFormChange('isActive', e.target.checked)}
                    className="w-5 h-5 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                  />
                </label>
              </div>
            </div>
          </div>

          {/* Right Panel - Content Sections */}
          <div className="bg-white border border-gray-300 rounded-xl p-6">
            {/* Section Header with Language Selector */}
            <div className="text-center mb-6 pb-4 border-b border-gray-300 flex items-center justify-center gap-3">
              <p className="text-sm text-gray-600">
                Content Sections
              </p>
              {languages.length > 0 && (
                <div className="w-32">
                  <Suspense fallback={<div className="h-8 bg-gray-100 rounded animate-pulse" />}>
                    <Select
                      options={languages}
                      value={languages.find(l => l.localeCode === selectedLocale) || languages[0]}
                      onChange={(lang) => handleLocaleChange(lang?.localeCode || defaultLocale)}
                      displayKey="localeCode"
                      valueKey="localeCode"
                      className="w-32"
                    />
                  </Suspense>
                </div>
              )}
            </div>

            {/* Section Fields */}
            <div className="space-y-4 max-h-[500px] overflow-y-auto pr-2">
              {block?.sections?.length > 0 ? (
                block.sections.map(section => (
                  <div key={section.slug}>
                    <label
                      htmlFor={`section-${section.slug}`}
                      className="block text-sm font-medium text-gray-700 mb-1"
                    >
                      {section.name}
                      {section.isRequired && <span className="text-red-500 ml-1">*</span>}
                      <span className="ml-2 text-xs text-gray-400 font-normal">({section.slug})</span>
                    </label>
                    {renderField(section)}
                  </div>
                ))
              ) : (
                <p className="text-sm text-gray-500 text-center py-8">
                  No sections defined for this block.
                </p>
              )}
            </div>
          </div>
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-3 mt-6">
          <button
            type="button"
            onClick={onCancel}
            className="px-6 py-2.5 text-gray-700 bg-gray-200 rounded-lg hover:bg-gray-300 transition-colors font-medium"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={saving}
            className="px-6 py-2.5 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium flex items-center gap-2"
          >
            {saving && (
              <div className="animate-spin rounded-full h-4 w-4 border-2 border-white border-t-transparent"></div>
            )}
            {saving ? 'Saving...' : (contentId ? 'Update' : 'Create')}
          </button>
        </div>
      </form>

      {/* Preview Section */}
      {block && (
        <div className="mt-8 border-t border-gray-200 pt-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">Preview</h2>
            {block.variants?.length > 0 && (
              <div className="w-48">
                <Suspense fallback={<div className="h-10 bg-gray-100 rounded-lg animate-pulse" />}>
                  <Select
                    label=""
                    options={block.variants}
                    value={block.variants.find(v => v.variantId === (previewVariant || formData.defaultVariantId)) || block.variants[0]}
                    onChange={(v) => setPreviewVariant(v?.variantId || null)}
                    displayKey="name"
                    valueKey="variantId"
                    placeholder="Select variant..."
                    className="w-48 mt-6"
                  />
                </Suspense>
              </div>
            )}
          </div>

          {/* Preview Container */}
          <div className="bg-gray-100 rounded-xl p-6 min-h-[200px]">
            <Suspense fallback={<div className="animate-pulse bg-gray-200 rounded-lg h-32" />}>
              {(() => {
                // Build content data from current translations
                const currentTranslations = translations[selectedLocale] || {};
                const contentData = {
                  title: currentTranslations.title || currentTranslations.headline || '',
                  subtitle: currentTranslations.subtitle || currentTranslations.subheadline || '',
                  body: currentTranslations.body || currentTranslations.content || '',
                  ctaText: currentTranslations['cta-text'] || currentTranslations.ctaText || '',
                  ctaUrl: currentTranslations['cta-url'] || currentTranslations.ctaUrl || '',
                  imageUrl: currentTranslations['image-url'] || currentTranslations.imageUrl || '',
                  author: currentTranslations.author || '',
                  sections: currentTranslations
                };

                // Determine variant slug
                const selectedVariant = block.variants?.find(v => v.variantId === (previewVariant || formData.defaultVariantId));
                const variantSlug = selectedVariant?.slug || 'default';
                const blockSlug = block.slug || 'card';
                const registryKey = `${blockSlug}/${variantSlug}`;

                // Get component from registry
                const PreviewComponent = previewRegistry[registryKey] || previewRegistry['fallback'];

                return <PreviewComponent content={contentData} />;
              })()}
            </Suspense>
          </div>

          <p className="text-xs text-gray-500 mt-2 text-center">
            Preview updates as you edit. Some styles may vary in production.
          </p>
        </div>
      )}

      {/* Modal for rich text editing */}
      {renderModalEditor()}
    </div>
  );
}
