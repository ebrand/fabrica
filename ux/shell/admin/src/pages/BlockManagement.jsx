import { useState, useEffect, Suspense, lazy } from 'react';
import Layout from '../components/Layout';
import TenantFilterDropdown from '../components/TenantFilterDropdown';

const BFF_CONTENT_URL = import.meta.env.VITE_BFF_CONTENT_URL || 'http://localhost:3240';

// Lazy load common components
const ConfirmModal = lazy(() => import('commonMfe/ConfirmModal'));

function BlockManagement() {
  const [blocks, setBlocks] = useState([]);
  const [sectionTypes, setSectionTypes] = useState([]);
  const [selectedBlock, setSelectedBlock] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [filterTenantId, setFilterTenantId] = useState(null);

  // New block form state
  const [showNewBlockForm, setShowNewBlockForm] = useState(false);
  const [newBlockName, setNewBlockName] = useState('');
  const [newBlockSlug, setNewBlockSlug] = useState('');
  const [newBlockDescription, setNewBlockDescription] = useState('');

  // Confirm modal state - generic for block, variant, section type deletions
  const [confirmModal, setConfirmModal] = useState({
    show: false,
    type: null, // 'block' | 'variant' | 'sectionType'
    id: null,
    name: '',
    data: null // additional data like blockId for variants
  });

  // Variant modal state
  const [showVariantModal, setShowVariantModal] = useState(false);
  const [variantForm, setVariantForm] = useState({
    name: '',
    slug: '',
    description: '',
    cssClass: '',
    isDefault: false
  });
  const [savingVariant, setSavingVariant] = useState(false);

  // Section type modal state
  const [showSectionTypeModal, setShowSectionTypeModal] = useState(false);
  const [sectionTypeForm, setSectionTypeForm] = useState({
    name: '',
    slug: '',
    description: '',
    fieldType: 'text'
  });
  const [savingSectionType, setSavingSectionType] = useState(false);

  useEffect(() => {
    fetchData();
  }, [filterTenantId]);

  const fetchData = async () => {
    try {
      setLoading(true);
      const tenantParam = filterTenantId ? `?tenantId=${filterTenantId}` : '';
      const [blocksRes, sectionsRes] = await Promise.all([
        fetch(`${BFF_CONTENT_URL}/api/content/block-templates${tenantParam}`, { credentials: 'include' }),
        fetch(`${BFF_CONTENT_URL}/api/content/section-types${tenantParam}`, { credentials: 'include' })
      ]);

      if (!blocksRes.ok) throw new Error('Failed to fetch blocks');
      if (!sectionsRes.ok) throw new Error('Failed to fetch section types');

      const blocksData = await blocksRes.json();
      const sectionsData = await sectionsRes.json();

      setBlocks(blocksData);
      setSectionTypes(sectionsData);
      setSelectedBlock(null); // Clear selection when filter changes
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleTenantFilterChange = (tenantId) => {
    setFilterTenantId(tenantId);
  };

  const handleCreateBlock = async (e) => {
    e.preventDefault();
    if (!newBlockName.trim() || !newBlockSlug.trim()) return;

    try {
      setSaving(true);
      const response = await fetch(`${BFF_CONTENT_URL}/api/content/block-templates`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
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
          { method: 'DELETE', credentials: 'include' }
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
            credentials: 'include',
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

  const handleDeleteClick = (block) => {
    setConfirmModal({
      show: true,
      type: 'block',
      id: block.blockId,
      name: block.name,
      data: null
    });
  };

  const handleDeleteVariantClick = (variant) => {
    setConfirmModal({
      show: true,
      type: 'variant',
      id: variant.variantId,
      name: variant.name,
      data: { blockId: selectedBlock?.blockId }
    });
  };

  const handleDeleteSectionTypeClick = (sectionType) => {
    setConfirmModal({
      show: true,
      type: 'sectionType',
      id: sectionType.sectionTypeId,
      name: sectionType.name,
      data: null
    });
  };

  const handleDeleteConfirm = async () => {
    const { type, id, data } = confirmModal;
    setConfirmModal({ show: false, type: null, id: null, name: '', data: null });

    try {
      setSaving(true);

      if (type === 'block') {
        const response = await fetch(
          `${BFF_CONTENT_URL}/api/content/block-templates/${id}`,
          { method: 'DELETE', credentials: 'include' }
        );

        if (!response.ok) {
          const err = await response.json();
          throw new Error(err.error || 'Failed to delete block');
        }

        setBlocks(blocks.filter(b => b.blockId !== id));
        if (selectedBlock?.blockId === id) {
          setSelectedBlock(null);
        }
      } else if (type === 'variant') {
        const response = await fetch(
          `${BFF_CONTENT_URL}/api/content/block-templates/${data.blockId}/variants/${id}`,
          { method: 'DELETE', credentials: 'include' }
        );

        if (!response.ok) {
          const err = await response.json();
          throw new Error(err.error || 'Failed to delete variant');
        }

        const updatedVariants = selectedBlock.variants.filter(v => v.variantId !== id);
        setSelectedBlock({ ...selectedBlock, variants: updatedVariants });
        setBlocks(blocks.map(b =>
          b.blockId === data.blockId
            ? { ...b, variants: updatedVariants }
            : b
        ));
      } else if (type === 'sectionType') {
        const response = await fetch(
          `${BFF_CONTENT_URL}/api/content/section-types/${id}`,
          { method: 'DELETE', credentials: 'include' }
        );

        if (!response.ok) {
          const err = await response.json();
          throw new Error(err.error || 'Failed to delete section type');
        }

        setSectionTypes(sectionTypes.filter(st => st.sectionTypeId !== id));
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteCancel = () => {
    setConfirmModal({ show: false, type: null, id: null, name: '', data: null });
  };

  // Auto-generate slug from name
  const handleNameChange = (value) => {
    setNewBlockName(value);
    if (!newBlockSlug || newBlockSlug === newBlockName.toLowerCase().replace(/\s+/g, '-')) {
      setNewBlockSlug(value.toLowerCase().replace(/\s+/g, '-'));
    }
  };

  // Variant handlers
  const handleOpenVariantModal = () => {
    setVariantForm({
      name: '',
      slug: '',
      description: '',
      cssClass: '',
      isDefault: false
    });
    setShowVariantModal(true);
  };

  const handleVariantNameChange = (value) => {
    setVariantForm(prev => ({
      ...prev,
      name: value,
      slug: prev.slug === prev.name.toLowerCase().replace(/\s+/g, '-') || !prev.slug
        ? value.toLowerCase().replace(/\s+/g, '-')
        : prev.slug
    }));
  };

  const handleCreateVariant = async (e) => {
    e.preventDefault();
    if (!selectedBlock || !variantForm.name.trim() || !variantForm.slug.trim()) return;

    try {
      setSavingVariant(true);
      const response = await fetch(
        `${BFF_CONTENT_URL}/api/content/block-templates/${selectedBlock.blockId}/variants`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify({
            name: variantForm.name.trim(),
            slug: variantForm.slug.trim().toLowerCase().replace(/\s+/g, '-'),
            description: variantForm.description.trim() || null,
            cssClass: variantForm.cssClass.trim() || null,
            isDefault: variantForm.isDefault
          })
        }
      );

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.error || 'Failed to create variant');
      }

      const created = await response.json();

      // Update local state
      const updatedVariants = [...(selectedBlock.variants || []), created];
      setSelectedBlock({ ...selectedBlock, variants: updatedVariants });
      setBlocks(blocks.map(b =>
        b.blockId === selectedBlock.blockId
          ? { ...b, variants: updatedVariants }
          : b
      ));

      setShowVariantModal(false);
    } catch (err) {
      setError(err.message);
    } finally {
      setSavingVariant(false);
    }
  };

  // Section type handlers
  const handleOpenSectionTypeModal = () => {
    setSectionTypeForm({
      name: '',
      slug: '',
      description: '',
      fieldType: 'text'
    });
    setShowSectionTypeModal(true);
  };

  const handleSectionTypeNameChange = (value) => {
    setSectionTypeForm(prev => ({
      ...prev,
      name: value,
      slug: prev.slug === prev.name.toLowerCase().replace(/\s+/g, '-') || !prev.slug
        ? value.toLowerCase().replace(/\s+/g, '-')
        : prev.slug
    }));
  };

  const handleCreateSectionType = async (e) => {
    e.preventDefault();
    if (!sectionTypeForm.name.trim() || !sectionTypeForm.slug.trim()) return;

    try {
      setSavingSectionType(true);
      const response = await fetch(
        `${BFF_CONTENT_URL}/api/content/section-types`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify({
            name: sectionTypeForm.name.trim(),
            slug: sectionTypeForm.slug.trim().toLowerCase().replace(/\s+/g, '-'),
            description: sectionTypeForm.description.trim() || null,
            fieldType: sectionTypeForm.fieldType
          })
        }
      );

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.error || 'Failed to create section type');
      }

      const created = await response.json();
      setSectionTypes([...sectionTypes, created]);
      setShowSectionTypeModal(false);
    } catch (err) {
      setError(err.message);
    } finally {
      setSavingSectionType(false);
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
      {/* Delete Confirmation Modal */}
      <Suspense fallback={null}>
        <ConfirmModal
          show={confirmModal.show}
          title={
            confirmModal.type === 'block' ? 'Delete Block' :
            confirmModal.type === 'variant' ? 'Delete Variant' :
            confirmModal.type === 'sectionType' ? 'Delete Section Type' : 'Confirm Delete'
          }
          message={
            confirmModal.type === 'block'
              ? `Are you sure you want to delete the block "${confirmModal.name}"? This will also delete all associated variants. This action cannot be undone.`
              : confirmModal.type === 'variant'
              ? `Are you sure you want to delete the variant "${confirmModal.name}"? This action cannot be undone.`
              : confirmModal.type === 'sectionType'
              ? `Are you sure you want to delete the section type "${confirmModal.name}"? This action cannot be undone.`
              : 'Are you sure you want to proceed?'
          }
          type="danger"
          confirmText="Delete"
          cancelText="Cancel"
          onConfirm={handleDeleteConfirm}
          onCancel={handleDeleteCancel}
        />
      </Suspense>

      {/* New Variant Modal */}
      {showVariantModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-md mx-4">
            <div className="px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">Create Variant</h3>
              <p className="text-sm text-gray-500">Add a new variant for {selectedBlock?.name}</p>
            </div>
            <form onSubmit={handleCreateVariant} className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Name *</label>
                <input
                  type="text"
                  value={variantForm.name}
                  onChange={(e) => handleVariantNameChange(e.target.value)}
                  placeholder="e.g., Hero Style"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
                  autoFocus
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Slug *</label>
                <input
                  type="text"
                  value={variantForm.slug}
                  onChange={(e) => setVariantForm(prev => ({ ...prev, slug: e.target.value.toLowerCase().replace(/\s+/g, '-') }))}
                  placeholder="e.g., hero-style"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500 font-mono"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                <input
                  type="text"
                  value={variantForm.description}
                  onChange={(e) => setVariantForm(prev => ({ ...prev, description: e.target.value }))}
                  placeholder="Brief description"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">CSS Class</label>
                <input
                  type="text"
                  value={variantForm.cssClass}
                  onChange={(e) => setVariantForm(prev => ({ ...prev, cssClass: e.target.value }))}
                  placeholder="e.g., card-hero"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500 font-mono"
                />
              </div>
              <label className="flex items-center">
                <input
                  type="checkbox"
                  checked={variantForm.isDefault}
                  onChange={(e) => setVariantForm(prev => ({ ...prev, isDefault: e.target.checked }))}
                  className="h-4 w-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                />
                <span className="ml-2 text-sm text-gray-700">Set as default variant</span>
              </label>
              <div className="flex gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setShowVariantModal(false)}
                  className="flex-1 px-4 py-2 bg-gray-200 text-gray-700 rounded-md hover:bg-gray-300 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={savingVariant || !variantForm.name.trim() || !variantForm.slug.trim()}
                  className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors disabled:opacity-50"
                >
                  {savingVariant ? 'Creating...' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* New Section Type Modal */}
      {showSectionTypeModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-md mx-4">
            <div className="px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">Create Section Type</h3>
              <p className="text-sm text-gray-500">Add a new section type for content blocks</p>
            </div>
            <form onSubmit={handleCreateSectionType} className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Name *</label>
                <input
                  type="text"
                  value={sectionTypeForm.name}
                  onChange={(e) => handleSectionTypeNameChange(e.target.value)}
                  placeholder="e.g., Author Bio"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
                  autoFocus
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Slug *</label>
                <input
                  type="text"
                  value={sectionTypeForm.slug}
                  onChange={(e) => setSectionTypeForm(prev => ({ ...prev, slug: e.target.value.toLowerCase().replace(/\s+/g, '-') }))}
                  placeholder="e.g., author-bio"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500 font-mono"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Field Type</label>
                <select
                  value={sectionTypeForm.fieldType}
                  onChange={(e) => setSectionTypeForm(prev => ({ ...prev, fieldType: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
                >
                  <option value="text">Text</option>
                  <option value="richtext">Rich Text</option>
                  <option value="url">URL</option>
                  <option value="image">Image</option>
                  <option value="number">Number</option>
                  <option value="date">Date</option>
                  <option value="boolean">Boolean</option>
                  <option value="json">JSON</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                <input
                  type="text"
                  value={sectionTypeForm.description}
                  onChange={(e) => setSectionTypeForm(prev => ({ ...prev, description: e.target.value }))}
                  placeholder="Brief description"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
              <div className="flex gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setShowSectionTypeModal(false)}
                  className="flex-1 px-4 py-2 bg-gray-200 text-gray-700 rounded-md hover:bg-gray-300 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={savingSectionType || !sectionTypeForm.name.trim() || !sectionTypeForm.slug.trim()}
                  className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors disabled:opacity-50"
                >
                  {savingSectionType ? 'Creating...' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      <div className="space-y-6">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Block Management</h1>
            <p className="mt-1 text-sm text-gray-500">
              Create and configure content block templates with variants and section types
            </p>
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

        {/* Main content grid - 3 columns */}
        <div className="grid grid-cols-1 lg:grid-cols-4 gap-4">
          {/* Variants list - first column */}
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-gray-900">Variants</h2>
              {selectedBlock && (
                <button
                  onClick={handleOpenVariantModal}
                  className="px-2 py-1 bg-blue-600 text-white text-xs rounded hover:bg-blue-700 transition-colors"
                >
                  + New
                </button>
              )}
            </div>

            {selectedBlock ? (
              <div className="space-y-1">
                {(selectedBlock.variants || []).map(variant => (
                  <div
                    key={variant.variantId}
                    className="p-2 rounded-lg border border-gray-200 bg-gray-50"
                  >
                    <div className="flex items-center justify-between">
                      <div className="min-w-0">
                        <div className="flex items-center gap-1.5">
                          <p className="font-medium text-gray-900 text-sm truncate">{variant.name}</p>
                          {variant.isDefault && (
                            <span className="px-1.5 py-0.5 bg-green-100 text-green-700 text-xs rounded">
                              default
                            </span>
                          )}
                        </div>
                        <p className="text-xs text-gray-500 font-mono truncate">{variant.slug}</p>
                        {variant.cssClass && (
                          <p className="text-xs text-gray-400 truncate">.{variant.cssClass}</p>
                        )}
                      </div>
                      <button
                        onClick={() => handleDeleteVariantClick(variant)}
                        disabled={saving}
                        className="p-1 text-gray-400 hover:text-red-500 transition-colors flex-shrink-0 disabled:opacity-50"
                        title="Delete variant"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                      </button>
                    </div>
                  </div>
                ))}

                {(!selectedBlock.variants || selectedBlock.variants.length === 0) && (
                  <p className="text-xs text-gray-500 text-center py-4">
                    No variants. Click "+ New" to add one.
                  </p>
                )}
              </div>
            ) : (
              <div className="flex items-center justify-center h-32 text-gray-400">
                <p className="text-xs text-center">Select a block to manage variants</p>
              </div>
            )}
          </div>

          {/* Block list - second column */}
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-gray-900">Blocks</h2>
              <button
                onClick={() => setShowNewBlockForm(true)}
                className="px-2 py-1 bg-blue-600 text-white text-xs rounded hover:bg-blue-700 transition-colors"
              >
                + New
              </button>
            </div>

            {/* New Block Form */}
            {showNewBlockForm && (
              <form onSubmit={handleCreateBlock} className="mb-4 p-3 bg-blue-50 rounded-lg border border-blue-200">
                <h3 className="text-sm font-semibold text-blue-900 mb-2">Create New Block</h3>
                <div className="space-y-2">
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Name</label>
                    <input
                      type="text"
                      value={newBlockName}
                      onChange={(e) => handleNameChange(e.target.value)}
                      placeholder="e.g., Article"
                      className="w-full px-2 py-1.5 text-sm border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
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
                      className="w-full px-2 py-1.5 text-sm border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500 font-mono"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Description</label>
                    <input
                      type="text"
                      value={newBlockDescription}
                      onChange={(e) => setNewBlockDescription(e.target.value)}
                      placeholder="Brief description"
                      className="w-full px-2 py-1.5 text-sm border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
                    />
                  </div>
                  <div className="flex gap-2">
                    <button
                      type="submit"
                      disabled={saving || !newBlockName.trim() || !newBlockSlug.trim()}
                      className="flex-1 px-2 py-1.5 bg-blue-600 text-white text-xs rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
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
                      className="px-2 py-1.5 bg-gray-200 text-gray-700 text-xs rounded-md hover:bg-gray-300"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              </form>
            )}

            {/* Block list */}
            <div className="space-y-1">
              {blocks.map(block => (
                <div
                  key={block.blockId}
                  className={`p-2 rounded-lg border cursor-pointer transition-all ${
                    selectedBlock?.blockId === block.blockId
                      ? 'border-blue-500 bg-blue-50 ring-1 ring-blue-500'
                      : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
                  }`}
                  onClick={() => setSelectedBlock(block)}
                >
                  <div className="flex items-center justify-between">
                    <div className="min-w-0">
                      <p className="font-medium text-gray-900 text-sm truncate">{block.name}</p>
                      <p className="text-xs text-gray-500 font-mono truncate">{block.slug}</p>
                    </div>
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        handleDeleteClick(block);
                      }}
                      className="p-1 text-gray-400 hover:text-red-500 transition-colors flex-shrink-0"
                      title="Delete block"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    </button>
                  </div>
                </div>
              ))}

              {blocks.length === 0 && !showNewBlockForm && (
                <p className="text-xs text-gray-500 text-center py-4">
                  No blocks yet. Create your first block.
                </p>
              )}
            </div>
          </div>

          {/* Section type toggles - spans 2 columns */}
          <div className="lg:col-span-2 bg-white rounded-xl shadow-sm border border-gray-200 p-4">
            {/* Section Types Header */}
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-gray-900">Sections</h2>
              <button
                onClick={handleOpenSectionTypeModal}
                className="px-2 py-1 bg-blue-600 text-white text-xs rounded hover:bg-blue-700 transition-colors"
              >
                + New
              </button>
            </div>

            {selectedBlock ? (
              <>
                {/* Selected block header */}
                <div className="flex items-center gap-3 mb-4 pb-3 border-b border-gray-200">
                  <div className="px-3 py-1.5 bg-gray-100 rounded-lg">
                    <span className="text-base font-semibold text-gray-900">{selectedBlock.slug}</span>
                  </div>
                  <div>
                    <p className="text-sm text-gray-600">{selectedBlock.name}</p>
                    {selectedBlock.description && (
                      <p className="text-xs text-gray-400">{selectedBlock.description}</p>
                    )}
                  </div>
                  <div className="ml-auto text-xs text-gray-400">
                    {selectedBlock.sections?.length || 0} sections &bull; {selectedBlock.variants?.length || 0} variants
                  </div>
                </div>

                {/* Section types grid */}
                <div className="space-y-1.5">
                  <p className="text-sm font-medium text-gray-700 mb-2">
                    Section types for this block:
                  </p>

                  {sectionTypes.map(sectionType => {
                    const isIncluded = selectedBlock.sections?.some(
                      s => s.slug === sectionType.slug
                    );

                    return (
                      <div
                        key={sectionType.sectionTypeId}
                        className="flex items-center gap-2"
                      >
                        <button
                          onClick={() => handleToggleSection(sectionType)}
                          disabled={saving}
                          className={`px-2 py-1 text-xs font-medium rounded transition-colors min-w-[50px] ${
                            isIncluded
                              ? 'bg-red-500 text-white hover:bg-red-600'
                              : 'bg-gray-200 text-gray-500 hover:bg-gray-300'
                          } disabled:opacity-50`}
                        >
                          {isIncluded ? 'Rem' : 'Add'}
                        </button>
                        <div className={`flex-1 px-3 py-1.5 rounded-lg border ${
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
                        <button
                          onClick={() => handleDeleteSectionTypeClick(sectionType)}
                          disabled={saving || isIncluded}
                          className="p-1 text-gray-400 hover:text-red-500 transition-colors flex-shrink-0 disabled:opacity-30 disabled:cursor-not-allowed"
                          title={isIncluded ? "Remove from block first" : "Delete section type"}
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                          </svg>
                        </button>
                      </div>
                    );
                  })}

                  {sectionTypes.length === 0 && (
                    <p className="text-sm text-gray-500 text-center py-4">
                      No section types available. Click "+ New" to create one.
                    </p>
                  )}
                </div>
              </>
            ) : (
              <div className="space-y-1.5">
                <p className="text-sm font-medium text-gray-700 mb-2">
                  All section types:
                </p>
                {sectionTypes.map(sectionType => (
                  <div
                    key={sectionType.sectionTypeId}
                    className="flex items-center gap-2"
                  >
                    <div className="flex-1 px-3 py-1.5 rounded-lg border bg-gray-50 border-gray-200">
                      <span className="text-sm text-gray-600">{sectionType.slug}</span>
                      {sectionType.fieldType && sectionType.fieldType !== 'text' && (
                        <span className="ml-2 text-xs text-gray-400">({sectionType.fieldType})</span>
                      )}
                    </div>
                    <button
                      onClick={() => handleDeleteSectionTypeClick(sectionType)}
                      disabled={saving}
                      className="p-1 text-gray-400 hover:text-red-500 transition-colors flex-shrink-0 disabled:opacity-50"
                      title="Delete section type"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                ))}
                {sectionTypes.length === 0 && (
                  <p className="text-sm text-gray-500 text-center py-4">
                    No section types available. Click "+ New" to create one.
                  </p>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    </Layout>
  );
}

export default BlockManagement;
