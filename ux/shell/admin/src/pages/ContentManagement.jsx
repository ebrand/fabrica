import { useState, useEffect, lazy, Suspense } from 'react';
import Layout from '../components/Layout';
import TenantFilterDropdown from '../components/TenantFilterDropdown';

const BFF_CONTENT_URL = import.meta.env.VITE_BFF_CONTENT_URL || 'http://localhost:3240';

// Lazy load components from MFEs
const BlockContentEditor = lazy(() => import('contentMfe/BlockContentEditor'));
const Select = lazy(() => import('commonMfe/Select'));
const ConfirmModal = lazy(() => import('commonMfe/ConfirmModal'));

function ContentManagement() {
  const [contents, setContents] = useState([]);
  const [blocks, setBlocks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [filterTenantId, setFilterTenantId] = useState(null);

  // Editor state
  const [editorMode, setEditorMode] = useState(null); // 'create' | 'edit' | null
  const [selectedContentId, setSelectedContentId] = useState(null);
  const [selectedBlockId, setSelectedBlockId] = useState(null);
  const [selectedBlock, setSelectedBlock] = useState(null); // Full block object for create mode

  // Block selection modal
  const [showBlockSelector, setShowBlockSelector] = useState(false);

  // Filter state
  const [filterBlock, setFilterBlock] = useState('');

  // Confirm modal state
  const [confirmModal, setConfirmModal] = useState({ show: false, contentId: null, contentName: '' });

  useEffect(() => {
    fetchData();
  }, [filterTenantId]);

  const fetchData = async () => {
    try {
      setLoading(true);
      const tenantParam = filterTenantId ? `?tenantId=${filterTenantId}` : '';
      const [contentsRes, blocksRes] = await Promise.all([
        fetch(`${BFF_CONTENT_URL}/api/content/blocks${tenantParam}`, { credentials: 'include' }),
        fetch(`${BFF_CONTENT_URL}/api/content/block-templates${tenantParam}`, { credentials: 'include' })
      ]);

      if (!contentsRes.ok) throw new Error('Failed to fetch content');
      if (!blocksRes.ok) throw new Error('Failed to fetch block templates');

      const contentsData = await contentsRes.json();
      const blocksData = await blocksRes.json();

      setContents(contentsData);
      setBlocks(blocksData);
      // Clear selection when filter changes
      setEditorMode(null);
      setSelectedContentId(null);
      setSelectedBlockId(null);
      setSelectedBlock(null);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleTenantFilterChange = (tenantId) => {
    setFilterTenantId(tenantId);
  };

  const handleCreateClick = () => {
    if (blocks.length === 0) {
      setError('No block templates available. Create block templates first.');
      return;
    }
    setShowBlockSelector(true);
  };

  const handleSelectBlock = (block) => {
    setSelectedBlockId(block.blockId);
    setSelectedBlock(block); // Pass full block object to avoid re-fetch
    setSelectedContentId(null);
    setEditorMode('create');
    setShowBlockSelector(false);
  };

  const handleEditClick = (content) => {
    setSelectedContentId(content.contentId);
    setSelectedBlockId(null);
    setEditorMode('edit');
  };

  const handleDeleteClick = (content) => {
    setConfirmModal({ show: true, contentId: content.contentId, contentName: content.name });
  };

  const handleDeleteConfirm = async () => {
    const { contentId } = confirmModal;
    setConfirmModal({ show: false, contentId: null, contentName: '' });

    try {
      const response = await fetch(`${BFF_CONTENT_URL}/api/content/blocks/${contentId}`, {
        method: 'DELETE',
        credentials: 'include'
      });

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.error || 'Failed to delete content');
      }

      setContents(contents.filter(c => c.contentId !== contentId));
    } catch (err) {
      setError(err.message);
    }
  };

  const handleDeleteCancel = () => {
    setConfirmModal({ show: false, contentId: null, contentName: '' });
  };

  const handleEditorSave = (savedContent) => {
    if (editorMode === 'create') {
      setContents([...contents, savedContent]);
    } else {
      setContents(contents.map(c =>
        c.contentId === savedContent.contentId ? { ...c, ...savedContent } : c
      ));
    }
    setEditorMode(null);
    setSelectedContentId(null);
    setSelectedBlockId(null);
    setSelectedBlock(null);
    // Refresh to get full data
    fetchData();
  };

  const handleEditorCancel = () => {
    setEditorMode(null);
    setSelectedContentId(null);
    setSelectedBlockId(null);
    setSelectedBlock(null);
  };

  // Filter contents
  const filteredContents = filterBlock
    ? contents.filter(c => c.block?.slug === filterBlock)
    : contents;

  if (loading) {
    return (
      <Layout>
        <div className="flex items-center justify-center p-12">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
          <p className="ml-4 text-gray-600">Loading content management...</p>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      {/* Delete Confirmation Modal */}
      <Suspense fallback={null}>
        <ConfirmModal
          show={confirmModal.show}
          title="Delete Content"
          message={`Are you sure you want to delete "${confirmModal.contentName}"? This action cannot be undone.`}
          type="danger"
          confirmText="Delete"
          cancelText="Cancel"
          onConfirm={handleDeleteConfirm}
          onCancel={handleDeleteCancel}
        />
      </Suspense>

      <div className="space-y-6">
        {/* Header */}
        <div className="flex flex-col gap-4">
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-2xl font-bold text-gray-900">Content Management</h1>
              <p className="mt-1 text-sm text-gray-500">
                Create and manage content blocks
              </p>
            </div>
            <button
              onClick={handleCreateClick}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors flex items-center gap-2"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              New Content
            </button>
          </div>
          <TenantFilterDropdown
            selectedTenantId={filterTenantId}
            onTenantChange={handleTenantFilterChange}
          />
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

        {/* Editor Mode */}
        {editorMode && (
          <Suspense fallback={
            <div className="flex items-center justify-center p-12 bg-white rounded-xl shadow-sm border">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
              <p className="ml-4 text-gray-600">Loading editor...</p>
            </div>
          }>
            <BlockContentEditor
              contentId={selectedContentId}
              blockId={selectedBlockId}
              block={selectedBlock}
              onSave={handleEditorSave}
              onCancel={handleEditorCancel}
            />
          </Suspense>
        )}

        {/* Content List (hidden when editor is open) */}
        {!editorMode && (
          <>
            {/* Filters */}
            <div className="flex items-center gap-4">
              <div className="w-64">
                <Suspense fallback={<div className="h-10 bg-gray-100 rounded animate-pulse" />}>
                  <Select
                    label="Filter by block"
                    options={[{ blockId: '', name: 'All blocks', slug: '' }, ...blocks]}
                    value={blocks.find(b => b.slug === filterBlock) || { blockId: '', name: 'All blocks', slug: '' }}
                    onChange={(selected) => setFilterBlock(selected?.slug || '')}
                    displayKey="name"
                    valueKey="slug"
                  />
                </Suspense>
              </div>
              <span className="text-sm text-gray-400 pt-6">
                {filteredContents.length} content item(s)
              </span>
            </div>

            {/* Content Grid */}
            {filteredContents.length === 0 ? (
              <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
                <svg className="w-12 h-12 text-gray-300 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
                </svg>
                <p className="text-gray-500">No content found.</p>
                <button
                  onClick={handleCreateClick}
                  className="mt-4 text-blue-600 hover:text-blue-700 font-medium"
                >
                  Create your first content
                </button>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {filteredContents.map(content => (
                  <div
                    key={content.contentId}
                    className="bg-white rounded-xl shadow-sm border border-gray-200 p-4 hover:shadow-md transition-shadow cursor-pointer"
                    onDoubleClick={() => handleEditClick(content)}
                  >
                    <div className="flex items-start justify-between mb-3">
                      <div className="flex-1 min-w-0">
                        <h3 className="font-semibold text-gray-900 truncate">{content.name}</h3>
                        <p className="text-xs text-gray-500 font-mono truncate">{content.slug}</p>
                      </div>
                      <span className="ml-2 px-2 py-0.5 text-xs rounded-full bg-blue-100 text-blue-700">
                        {content.block?.name || content.block?.slug}
                      </span>
                    </div>

                    {content.description && (
                      <p className="text-sm text-gray-600 mb-3 line-clamp-2">{content.description}</p>
                    )}

                    {/* Section preview */}
                    {content.sections && Object.keys(content.sections).length > 0 && (
                      <div className="text-xs text-gray-400 mb-3">
                        <span className="font-medium">Sections:</span>{' '}
                        {Object.keys(content.sections).slice(0, 3).join(', ')}
                        {Object.keys(content.sections).length > 3 && '...'}
                      </div>
                    )}

                    <div className="flex items-center justify-between pt-3 border-t border-gray-100">
                      <div className="flex items-center gap-2">
                        {content.isActive !== false ? (
                          <span className="flex items-center text-xs text-green-600">
                            <span className="w-1.5 h-1.5 bg-green-500 rounded-full mr-1"></span>
                            Active
                          </span>
                        ) : (
                          <span className="flex items-center text-xs text-gray-400">
                            <span className="w-1.5 h-1.5 bg-gray-300 rounded-full mr-1"></span>
                            Inactive
                          </span>
                        )}
                      </div>
                      <div className="flex items-center gap-1">
                        <button
                          onClick={() => handleEditClick(content)}
                          className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded transition-colors"
                          title="Edit"
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                          </svg>
                        </button>
                        <button
                          onClick={() => handleDeleteClick(content)}
                          className="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
                          title="Delete"
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                          </svg>
                        </button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </>
        )}

        {/* Block Selector Modal */}
        {showBlockSelector && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
            <div className="bg-white rounded-xl shadow-xl max-w-lg w-full mx-4 max-h-[80vh] overflow-hidden">
              <div className="px-6 py-4 border-b border-gray-200">
                <div className="flex items-center justify-between">
                  <h3 className="text-lg font-semibold text-gray-900">Select Block Type</h3>
                  <button
                    onClick={() => setShowBlockSelector(false)}
                    className="text-gray-400 hover:text-gray-600"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
                <p className="text-sm text-gray-500 mt-1">
                  Choose a block template for your new content
                </p>
              </div>
              <div className="p-4 overflow-y-auto max-h-96">
                <div className="grid grid-cols-2 gap-3">
                  {blocks.map(block => (
                    <button
                      key={block.blockId}
                      onClick={() => handleSelectBlock(block)}
                      className="p-4 text-left border border-gray-200 rounded-lg hover:border-blue-500 hover:bg-blue-50 transition-colors"
                    >
                      <p className="font-medium text-gray-900">{block.name}</p>
                      <p className="text-xs text-gray-500 font-mono">{block.slug}</p>
                      {block.description && (
                        <p className="text-xs text-gray-400 mt-1 line-clamp-2">{block.description}</p>
                      )}
                      <p className="text-xs text-gray-400 mt-2">
                        {block.sections?.length || 0} section(s) | {block.variants?.length || 0} variant(s)
                      </p>
                    </button>
                  ))}
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </Layout>
  );
}

export default ContentManagement;
