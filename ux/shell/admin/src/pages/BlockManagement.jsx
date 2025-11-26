import { useState, useEffect } from 'react';
import Layout from '../components/Layout';

const BFF_CONTENT_URL = import.meta.env.VITE_BFF_CONTENT_URL || 'http://localhost:3240';

function BlockManagement() {
  const [blocks, setBlocks] = useState([]);
  const [sectionTypes, setSectionTypes] = useState([]);
  const [selectedBlock, setSelectedBlock] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);

  // New block form state
  const [showNewBlockForm, setShowNewBlockForm] = useState(false);
  const [newBlockName, setNewBlockName] = useState('');
  const [newBlockSlug, setNewBlockSlug] = useState('');
  const [newBlockDescription, setNewBlockDescription] = useState('');

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [blocksRes, sectionsRes] = await Promise.all([
        fetch(`${BFF_CONTENT_URL}/api/content/block-templates`),
        fetch(`${BFF_CONTENT_URL}/api/content/section-types`)
      ]);

      if (!blocksRes.ok) throw new Error('Failed to fetch blocks');
      if (!sectionsRes.ok) throw new Error('Failed to fetch section types');

      const blocksData = await blocksRes.json();
      const sectionsData = await sectionsRes.json();

      setBlocks(blocksData);
      setSectionTypes(sectionsData);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateBlock = async (e) => {
    e.preventDefault();
    if (!newBlockName.trim() || !newBlockSlug.trim()) return;

    try {
      setSaving(true);
      const response = await fetch(`${BFF_CONTENT_URL}/api/content/block-templates`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: newBlockName.trim(),
          slug: newBlockSlug.trim().toLowerCase().replace(/\s+/g, '-'),
          description: newBlockDescription.trim() || null
        })
      });

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.error || 'Failed to create block');
      }

      const created = await response.json();

      // Add to blocks list and select it
      const newBlock = { ...created, sections: [], variants: [] };
      setBlocks([...blocks, newBlock]);
      setSelectedBlock(newBlock);

      // Reset form
      setNewBlockName('');
      setNewBlockSlug('');
      setNewBlockDescription('');
      setShowNewBlockForm(false);
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleToggleSection = async (sectionType) => {
    if (!selectedBlock) return;

    const isCurrentlyIncluded = selectedBlock.sections?.some(
      s => s.slug === sectionType.slug
    );

    try {
      setSaving(true);

      if (isCurrentlyIncluded) {
        // Remove section
        const response = await fetch(
          `${BFF_CONTENT_URL}/api/content/block-templates/${selectedBlock.blockId}/sections/${sectionType.sectionTypeId}`,
          { method: 'DELETE' }
        );

        if (!response.ok) {
          const err = await response.json();
          throw new Error(err.error || 'Failed to remove section');
        }

        // Update local state
        setSelectedBlock({
          ...selectedBlock,
          sections: selectedBlock.sections.filter(s => s.slug !== sectionType.slug)
        });
        setBlocks(blocks.map(b =>
          b.blockId === selectedBlock.blockId
            ? { ...b, sections: b.sections.filter(s => s.slug !== sectionType.slug) }
            : b
        ));
      } else {
        // Add section
        const response = await fetch(
          `${BFF_CONTENT_URL}/api/content/block-templates/${selectedBlock.blockId}/sections/${sectionType.sectionTypeId}`,
          {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ isRequired: false })
          }
        );

        if (!response.ok) {
          const err = await response.json();
          throw new Error(err.error || 'Failed to add section');
        }

        const result = await response.json();
        const newSection = {
          slug: sectionType.slug,
          name: sectionType.name,
          fieldType: sectionType.fieldType,
          isRequired: result.isRequired,
          displayOrder: result.displayOrder
        };

        // Update local state
        setSelectedBlock({
          ...selectedBlock,
          sections: [...(selectedBlock.sections || []), newSection]
        });
        setBlocks(blocks.map(b =>
          b.blockId === selectedBlock.blockId
            ? { ...b, sections: [...(b.sections || []), newSection] }
            : b
        ));
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteBlock = async (block) => {
    if (!confirm(`Delete block "${block.name}"? This cannot be undone.`)) return;

    try {
      setSaving(true);
      const response = await fetch(
        `${BFF_CONTENT_URL}/api/content/block-templates/${block.blockId}`,
        { method: 'DELETE' }
      );

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.error || 'Failed to delete block');
      }

      setBlocks(blocks.filter(b => b.blockId !== block.blockId));
      if (selectedBlock?.blockId === block.blockId) {
        setSelectedBlock(null);
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  // Auto-generate slug from name
  const handleNameChange = (value) => {
    setNewBlockName(value);
    if (!newBlockSlug || newBlockSlug === newBlockName.toLowerCase().replace(/\s+/g, '-')) {
      setNewBlockSlug(value.toLowerCase().replace(/\s+/g, '-'));
    }
  };

  if (loading) {
    return (
      <Layout>
        <div className="flex items-center justify-center p-12">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
          <p className="ml-4 text-gray-600">Loading block management...</p>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex justify-between items-center">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Block Management</h1>
            <p className="mt-1 text-sm text-gray-500">
              Create and configure content block templates with section types
            </p>
          </div>
          <button
            onClick={() => setShowNewBlockForm(true)}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors flex items-center gap-2"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            New Block
          </button>
        </div>

        {/* Error message */}
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="flex items-center">
              <svg className="w-5 h-5 text-red-400 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <span className="text-red-700">{error}</span>
              <button onClick={() => setError(null)} className="ml-auto text-red-400 hover:text-red-600">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>
        )}

        {/* Main content grid */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Block list */}
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Blocks</h2>

            {/* New Block Form */}
            {showNewBlockForm && (
              <form onSubmit={handleCreateBlock} className="mb-4 p-4 bg-blue-50 rounded-lg border border-blue-200">
                <h3 className="text-sm font-semibold text-blue-900 mb-3">Create New Block</h3>
                <div className="space-y-3">
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Name</label>
                    <input
                      type="text"
                      value={newBlockName}
                      onChange={(e) => handleNameChange(e.target.value)}
                      placeholder="e.g., Article"
                      className="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
                      autoFocus
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Slug</label>
                    <input
                      type="text"
                      value={newBlockSlug}
                      onChange={(e) => setNewBlockSlug(e.target.value.toLowerCase().replace(/\s+/g, '-'))}
                      placeholder="e.g., article"
                      className="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500 font-mono"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Description (optional)</label>
                    <input
                      type="text"
                      value={newBlockDescription}
                      onChange={(e) => setNewBlockDescription(e.target.value)}
                      placeholder="Brief description"
                      className="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
                    />
                  </div>
                  <div className="flex gap-2">
                    <button
                      type="submit"
                      disabled={saving || !newBlockName.trim() || !newBlockSlug.trim()}
                      className="flex-1 px-3 py-2 bg-blue-600 text-white text-sm rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {saving ? 'Creating...' : 'Create'}
                    </button>
                    <button
                      type="button"
                      onClick={() => {
                        setShowNewBlockForm(false);
                        setNewBlockName('');
                        setNewBlockSlug('');
                        setNewBlockDescription('');
                      }}
                      className="px-3 py-2 bg-gray-200 text-gray-700 text-sm rounded-md hover:bg-gray-300"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              </form>
            )}

            {/* Block list */}
            <div className="space-y-2">
              {blocks.map(block => (
                <div
                  key={block.blockId}
                  className={`p-3 rounded-lg border cursor-pointer transition-all ${
                    selectedBlock?.blockId === block.blockId
                      ? 'border-blue-500 bg-blue-50 ring-1 ring-blue-500'
                      : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
                  }`}
                  onClick={() => setSelectedBlock(block)}
                >
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium text-gray-900">{block.name}</p>
                      <p className="text-xs text-gray-500 font-mono">{block.slug}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="text-xs text-gray-400">
                        {block.sections?.length || 0} sections
                      </span>
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDeleteBlock(block);
                        }}
                        className="p-1 text-gray-400 hover:text-red-500 transition-colors"
                        title="Delete block"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                      </button>
                    </div>
                  </div>
                </div>
              ))}

              {blocks.length === 0 && !showNewBlockForm && (
                <p className="text-sm text-gray-500 text-center py-4">
                  No blocks yet. Create your first block to get started.
                </p>
              )}
            </div>
          </div>

          {/* Section type toggles */}
          <div className="lg:col-span-2 bg-white rounded-xl shadow-sm border border-gray-200 p-4">
            {selectedBlock ? (
              <>
                {/* Selected block header */}
                <div className="flex items-center gap-3 mb-6 pb-4 border-b border-gray-200">
                  <div className="px-4 py-2 bg-gray-100 rounded-lg">
                    <span className="text-lg font-semibold text-gray-900">{selectedBlock.slug}</span>
                  </div>
                  <div>
                    <p className="text-sm text-gray-600">{selectedBlock.name}</p>
                    {selectedBlock.description && (
                      <p className="text-xs text-gray-400">{selectedBlock.description}</p>
                    )}
                  </div>
                </div>

                {/* Section types grid */}
                <div className="space-y-2">
                  <p className="text-sm font-medium text-gray-700 mb-3">
                    Select section types to include in this block:
                  </p>

                  {sectionTypes.map(sectionType => {
                    const isIncluded = selectedBlock.sections?.some(
                      s => s.slug === sectionType.slug
                    );

                    return (
                      <div
                        key={sectionType.sectionTypeId}
                        className="flex items-center gap-3"
                      >
                        <button
                          onClick={() => handleToggleSection(sectionType)}
                          disabled={saving}
                          className={`px-3 py-1.5 text-sm font-medium rounded transition-colors min-w-[60px] ${
                            isIncluded
                              ? 'bg-red-500 text-white hover:bg-red-600'
                              : 'bg-gray-200 text-gray-500 hover:bg-gray-300'
                          } disabled:opacity-50`}
                        >
                          {isIncluded ? 'Rem' : 'Add'}
                        </button>
                        <div className={`flex-1 px-4 py-2 rounded-lg border ${
                          isIncluded
                            ? 'bg-white border-gray-300'
                            : 'bg-gray-50 border-gray-200'
                        }`}>
                          <span className={`text-sm ${
                            isIncluded ? 'font-medium text-gray-900' : 'text-gray-400'
                          }`}>
                            {sectionType.slug}
                          </span>
                          {sectionType.fieldType && sectionType.fieldType !== 'text' && (
                            <span className="ml-2 text-xs text-gray-400">
                              ({sectionType.fieldType})
                            </span>
                          )}
                        </div>
                      </div>
                    );
                  })}

                  {sectionTypes.length === 0 && (
                    <p className="text-sm text-gray-500 text-center py-4">
                      No section types available. Add section types first.
                    </p>
                  )}
                </div>
              </>
            ) : (
              <div className="flex flex-col items-center justify-center h-64 text-gray-400">
                <svg className="w-12 h-12 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
                </svg>
                <p className="text-sm">Select a block to configure its section types</p>
              </div>
            )}
          </div>
        </div>
      </div>
    </Layout>
  );
}

export default BlockManagement;
