import { useState, useEffect, lazy, Suspense } from 'react';
import Layout from '../components/Layout';
import TenantFilterDropdown from '../components/TenantFilterDropdown';

const BFF_CONTENT_URL = import.meta.env.VITE_BFF_CONTENT_URL || 'http://localhost:3240';

// Lazy load components from MFEs
const ConfirmModal = lazy(() => import('commonMfe/ConfirmModal'));

function LanguageManagement() {
  const [languages, setLanguages] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [filterTenantId, setFilterTenantId] = useState(null);

  // Editor state
  const [editorMode, setEditorMode] = useState(null); // 'create' | 'edit' | null
  const [editingLanguage, setEditingLanguage] = useState(null);

  // Form state
  const [formData, setFormData] = useState({
    localeCode: '',
    languageCode: '',
    name: '',
    nativeName: '',
    isDefault: false,
    isActive: true,
    direction: 'ltr',
    dateFormat: '',
    currencyCode: '',
    displayOrder: 0
  });

  // Confirm modal state
  const [confirmModal, setConfirmModal] = useState({ show: false, languageId: null, languageName: '' });

  useEffect(() => {
    fetchLanguages();
  }, [filterTenantId]);

  const handleTenantFilterChange = (tenantId) => {
    setFilterTenantId(tenantId);
  };

  const fetchLanguages = async () => {
    try {
      setLoading(true);
      const tenantParam = filterTenantId ? `?tenantId=${filterTenantId}` : '';
      const response = await fetch(`${BFF_CONTENT_URL}/api/content/languages${tenantParam}`, {
        credentials: 'include'
      });

      if (!response.ok) throw new Error('Failed to fetch languages');

      const data = await response.json();
      setLanguages(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateClick = () => {
    setFormData({
      localeCode: '',
      languageCode: '',
      name: '',
      nativeName: '',
      isDefault: false,
      isActive: true,
      direction: 'ltr',
      dateFormat: '',
      currencyCode: '',
      displayOrder: languages.length + 1
    });
    setEditingLanguage(null);
    setEditorMode('create');
  };

  const handleEditClick = (language) => {
    setFormData({
      localeCode: language.localeCode || '',
      languageCode: language.languageCode || '',
      name: language.name || '',
      nativeName: language.nativeName || '',
      isDefault: language.isDefault || false,
      isActive: language.isActive !== false,
      direction: language.direction || 'ltr',
      dateFormat: language.dateFormat || '',
      currencyCode: language.currencyCode || '',
      displayOrder: language.displayOrder || 0
    });
    setEditingLanguage(language);
    setEditorMode('edit');
  };

  const handleDeleteClick = (language) => {
    setConfirmModal({ show: true, languageId: language.id, languageName: language.name });
  };

  const handleDeleteConfirm = async () => {
    const { languageId } = confirmModal;
    setConfirmModal({ show: false, languageId: null, languageName: '' });

    try {
      const response = await fetch(`${BFF_CONTENT_URL}/api/content/languages/${languageId}`, {
        method: 'DELETE',
        credentials: 'include'
      });

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.error || 'Failed to delete language');
      }

      setLanguages(languages.filter(l => l.id !== languageId));
    } catch (err) {
      setError(err.message);
    }
  };

  const handleDeleteCancel = () => {
    setConfirmModal({ show: false, languageId: null, languageName: '' });
  };

  const handleFormChange = (field, value) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSave = async () => {
    try {
      const url = editorMode === 'create'
        ? `${BFF_CONTENT_URL}/api/content/languages`
        : `${BFF_CONTENT_URL}/api/content/languages/${editingLanguage.id}`;

      const method = editorMode === 'create' ? 'POST' : 'PUT';

      const response = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(formData)
      });

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.error || `Failed to ${editorMode} language`);
      }

      // Refresh data and close editor
      await fetchLanguages();
      setEditorMode(null);
      setEditingLanguage(null);
    } catch (err) {
      setError(err.message);
    }
  };

  const handleCancel = () => {
    setEditorMode(null);
    setEditingLanguage(null);
  };

  if (loading) {
    return (
      <Layout>
        <div className="flex items-center justify-center p-12">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
          <p className="ml-4 text-gray-600">Loading languages...</p>
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
          title="Delete Language"
          message={`Are you sure you want to delete "${confirmModal.languageName}"? This action cannot be undone.`}
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
              <h1 className="text-2xl font-bold text-gray-900">Language Management</h1>
              <p className="mt-1 text-sm text-gray-500">
                Configure available languages for content localization
              </p>
            </div>
            {!editorMode && (
              <button
                onClick={handleCreateClick}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors flex items-center gap-2"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                </svg>
                Add Language
              </button>
            )}
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
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
              {editorMode === 'create' ? 'Add New Language' : 'Edit Language'}
            </h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* Locale Code */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Locale Code *
                </label>
                <input
                  type="text"
                  value={formData.localeCode}
                  onChange={(e) => handleFormChange('localeCode', e.target.value)}
                  placeholder="e.g., en-US, es-ES"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  disabled={editorMode === 'edit'}
                />
              </div>

              {/* Language Code */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Language Code *
                </label>
                <input
                  type="text"
                  value={formData.languageCode}
                  onChange={(e) => handleFormChange('languageCode', e.target.value)}
                  placeholder="e.g., en, es, fr"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              {/* Name */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Name *
                </label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => handleFormChange('name', e.target.value)}
                  placeholder="e.g., English (US)"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              {/* Native Name */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Native Name
                </label>
                <input
                  type="text"
                  value={formData.nativeName}
                  onChange={(e) => handleFormChange('nativeName', e.target.value)}
                  placeholder="e.g., English, Espanol"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              {/* Direction */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Text Direction
                </label>
                <select
                  value={formData.direction}
                  onChange={(e) => handleFormChange('direction', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                >
                  <option value="ltr">Left to Right (LTR)</option>
                  <option value="rtl">Right to Left (RTL)</option>
                </select>
              </div>

              {/* Currency Code */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Currency Code
                </label>
                <input
                  type="text"
                  value={formData.currencyCode}
                  onChange={(e) => handleFormChange('currencyCode', e.target.value.toUpperCase())}
                  placeholder="e.g., USD, EUR"
                  maxLength={3}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              {/* Date Format */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Date Format
                </label>
                <input
                  type="text"
                  value={formData.dateFormat}
                  onChange={(e) => handleFormChange('dateFormat', e.target.value)}
                  placeholder="e.g., MM/dd/yyyy"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              {/* Display Order */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Display Order
                </label>
                <input
                  type="number"
                  value={formData.displayOrder}
                  onChange={(e) => handleFormChange('displayOrder', parseInt(e.target.value) || 0)}
                  min={0}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              {/* Checkboxes */}
              <div className="md:col-span-2 flex flex-wrap gap-6">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={formData.isActive}
                    onChange={(e) => handleFormChange('isActive', e.target.checked)}
                    className="w-4 h-4 text-blue-600 rounded border-gray-300 focus:ring-blue-500"
                  />
                  <span className="text-sm text-gray-700">Active</span>
                </label>

                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={formData.isDefault}
                    onChange={(e) => handleFormChange('isDefault', e.target.checked)}
                    className="w-4 h-4 text-blue-600 rounded border-gray-300 focus:ring-blue-500"
                  />
                  <span className="text-sm text-gray-700">Default Language</span>
                </label>
              </div>
            </div>

            {/* Actions */}
            <div className="mt-6 flex items-center gap-3">
              <button
                onClick={handleSave}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
              >
                {editorMode === 'create' ? 'Create Language' : 'Save Changes'}
              </button>
              <button
                onClick={handleCancel}
                className="px-4 py-2 text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors"
              >
                Cancel
              </button>
            </div>
          </div>
        )}

        {/* Languages List */}
        {!editorMode && (
          <>
            <div className="text-sm text-gray-500">
              {languages.length} language(s) configured
            </div>

            {languages.length === 0 ? (
              <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
                <svg className="w-12 h-12 text-gray-300 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 5h12M9 3v2m1.048 9.5A18.022 18.022 0 016.412 9m6.088 9h7M11 21l5-10 5 10M12.751 5C11.783 10.77 8.07 15.61 3 18.129" />
                </svg>
                <p className="text-gray-500">No languages configured.</p>
                <button
                  onClick={handleCreateClick}
                  className="mt-4 text-blue-600 hover:text-blue-700 font-medium"
                >
                  Add your first language
                </button>
              </div>
            ) : (
              <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Language
                      </th>
                      <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Locale
                      </th>
                      <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Direction
                      </th>
                      <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Currency
                      </th>
                      <th scope="col" className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Status
                      </th>
                      <th scope="col" className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {languages.map((language) => (
                      <tr key={language.id} className="hover:bg-gray-50">
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="flex items-center">
                            <div>
                              <div className="text-sm font-medium text-gray-900">
                                {language.name}
                                {language.isDefault && (
                                  <span className="ml-2 px-2 py-0.5 text-xs rounded-full bg-blue-100 text-blue-700">
                                    Default
                                  </span>
                                )}
                              </div>
                              {language.nativeName && language.nativeName !== language.name && (
                                <div className="text-sm text-gray-500">{language.nativeName}</div>
                              )}
                            </div>
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm font-mono text-gray-900">{language.localeCode}</div>
                          <div className="text-xs text-gray-500">{language.languageCode}</div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {language.direction === 'rtl' ? 'RTL' : 'LTR'}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {language.currencyCode || '-'}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-center">
                          {language.isActive !== false ? (
                            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                              Active
                            </span>
                          ) : (
                            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-600">
                              Inactive
                            </span>
                          )}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                          <div className="flex items-center justify-end gap-2">
                            <button
                              onClick={() => handleEditClick(language)}
                              className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded transition-colors"
                              title="Edit"
                            >
                              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                              </svg>
                            </button>
                            <button
                              onClick={() => handleDeleteClick(language)}
                              className="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
                              title="Delete"
                              disabled={language.isDefault}
                            >
                              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                              </svg>
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )}
      </div>
    </Layout>
  );
}

export default LanguageManagement;
