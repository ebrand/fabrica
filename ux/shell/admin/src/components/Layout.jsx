import { useState, lazy, Suspense } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { Popover, PopoverButton, PopoverPanel } from '@headlessui/react';
import { ChevronDownIcon } from '@heroicons/react/20/solid';
import { useAuth } from '../context/AuthContext';
import axios from 'axios';
import configService from '../services/config';
import AdminToolsDrawer from './AdminToolsDrawer';

// Dynamically import the UserEditor component from the Admin MFE
const UserEditor = lazy(() => import('adminMfe/UserEditor'));

const Layout = ({ children }) => {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, syncedUser, logout, refreshUser } = useAuth();
  const [showProfileModal, setShowProfileModal] = useState(false);
  const [showAdminTools, setShowAdminTools] = useState(false);

  // Use syncedUser (from our DB) for display, fall back to Stytch user
  const displayName = syncedUser?.displayName ||
    `${syncedUser?.firstName || user?.name?.first_name || ''} ${syncedUser?.lastName || user?.name?.last_name || ''}`.trim() ||
    'User';
  const firstName = syncedUser?.firstName || user?.name?.first_name || '';
  const userInitial = firstName?.[0]?.toUpperCase() || 'U';

  // Hierarchical navigation structure
  const navigationSections = [
    {
      id: 'user-maintenance',
      name: 'User Maintenance',
      icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z',
      items: [
        { name: 'Users', href: '/users', icon: 'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z' },
        { name: 'Roles', href: '/roles', icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z' },
        { name: 'Permissions', href: '/permissions', icon: 'M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z' }
      ]
    },
    {
      id: 'product-management',
      name: 'Product Management',
      icon: 'M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4',
      items: [
        { name: 'Products', href: '/products', icon: 'M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4' },
        { name: 'Categories', href: '/categories', icon: 'M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z' }
      ]
    },
    {
      id: 'system',
      name: 'System',
      icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z',
      items: [
        { name: 'API Documentation', href: '/api-docs', icon: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z' },
        { name: 'Admin Tools', icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z', isDrawer: 'adminTools' },
        { name: 'Consul UI', href: 'http://localhost:8500', external: true, icon: 'M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z' },
        { name: 'Vault UI', href: 'http://localhost:8200', external: true, icon: 'M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z' }
      ]
    }
  ];

  // Get breadcrumb path based on current location
  const getBreadcrumbs = () => {
    const breadcrumbs = [{ name: 'Home', href: '/' }];

    for (const section of navigationSections) {
      const activeItem = section.items.find(item => item.href === location.pathname);
      if (activeItem) {
        breadcrumbs.push({ name: section.name, href: '#' });
        breadcrumbs.push({ name: activeItem.name, href: activeItem.href });
        break;
      }
    }

    return breadcrumbs;
  };

  const isInSection = (section) => {
    return section.items.some(item => item.href === location.pathname);
  };

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  // Build user data for profile editor
  const profileUserData = {
    userId       : syncedUser?.userId,
    email        : syncedUser?.email || user?.emails?.[0]?.email || '',
    firstName    : syncedUser?.firstName || user?.name?.first_name || '',
    lastName     : syncedUser?.lastName || user?.name?.last_name || '',
    displayName  : syncedUser?.displayName || displayName,
    isActive     : syncedUser?.isActive ?? true,
    isSystemAdmin: syncedUser?.isSystemAdmin ?? false,
    lastLoginAt  : syncedUser?.lastLoginAt
  };

  const handleSaveProfile = async (formData) => {
    if (!syncedUser?.userId) {
      throw new Error('User ID not found. Please refresh the page.');
    }

    const bffUrl = await configService.getBffAdminUrl();
    await axios.put(`${bffUrl}/api/users/${syncedUser.userId}`, formData);

    // Close modal and refresh user data in header
    setShowProfileModal(false);
    if (refreshUser) {
      refreshUser();
    }
  };

  const breadcrumbs = getBreadcrumbs();

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Top Navigation */}
      <nav className="bg-white border-b border-gray-200">
        <div className="mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            
            <div className="flex">
              
              {/* Logo */}
              <div className="flex-shrink-0 flex items-center">
                <Link to="/" className="flex items-center">
                  <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-blue-700 rounded-lg flex items-center justify-center">
                    <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                    </svg>
                  </div>
                  <span className="ml-3 text-xl font-bold text-gray-900">Fabrica Admin</span>
                </Link>
              </div>

              {/* Main Navigation Sections */}
              <div className="hidden sm:ml-8 sm:flex sm:space-x-2">
                {navigationSections.map((section) => {
                  const isActive = isInSection(section);

                  // If section has no items, render as a simple button
                  if (section.items.length === 0) {
                    return (
                      <div
                        key={section.id}
                        className={`inline-flex items-center px-4 py-2 h-16 text-sm font-medium border-b-2 transition-colors cursor-pointer ${
                          isActive
                            ? 'border-blue-500 text-blue-700'
                            : 'border-transparent text-gray-700 hover:text-gray-900'
                        }`}
                      >
                        <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={section.icon} />
                        </svg>
                        {section.name}
                      </div>
                    );
                  }

                  // Render Popover for sections with items
                  return (
                    <Popover key={section.id} className="relative">
                      {({ open, close }) => (
                        <div
                          onMouseEnter={(e) => {
                            const button = e.currentTarget.querySelector('button');
                            if (button && !open) {
                              button.click();
                            }
                          }}
                          onMouseLeave={() => {
                            if (open) {
                              close();
                            }
                          }}
                        >
                          <PopoverButton
                            className={`inline-flex items-center gap-x-1 px-4 py-2 h-16 text-sm font-medium border-b-2 transition-colors focus:outline-none cursor-pointer ${
                              isActive || open
                                ? 'border-blue-500 text-blue-700'
                                : 'border-transparent text-gray-700 hover:text-gray-900'
                            }`}
                          >
                            <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={section.icon} />
                            </svg>
                            <span>{section.name}</span>
                            <ChevronDownIcon
                              aria-hidden="true"
                              className={`w-4 h-4 transition-transform ${open ? 'rotate-180' : ''}`}
                            />
                          </PopoverButton>

                          <PopoverPanel
                            transition
                            className="absolute left-0 z-10 -mt-2 w-screen max-w-sm transition data-closed:translate-y-1 data-closed:opacity-0 data-enter:duration-200 data-enter:ease-out data-leave:duration-150 data-leave:ease-in"
                          >
                            <div className="overflow-hidden rounded-lg shadow-lg ring-1 ring-gray-200 ring-opacity-50 bg-white">
                              <div className="p-2">
                                {section.items.map((item) => {
                                  const isItemActive = !item.external && !item.isDrawer && location.pathname === item.href;

                                  // Handle drawer items
                                  if (item.isDrawer) {
                                    const handleDrawerClick = () => {
                                      close();
                                      if (item.isDrawer === 'adminTools') {
                                        setShowAdminTools(true);
                                      }
                                    };
                                    return (
                                      <button
                                        key={item.name}
                                        onClick={handleDrawerClick}
                                        className="w-full group flex items-center gap-x-3 rounded-lg p-3 transition-colors cursor-pointer text-gray-700 hover:bg-gray-50"
                                      >
                                        <div className="flex-none rounded-lg p-2 bg-gray-50 group-hover:bg-white">
                                          <svg className="w-5 h-5 text-gray-600 group-hover:text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={item.icon} />
                                          </svg>
                                        </div>
                                        <div className="flex-auto text-left">
                                          <span className="font-semibold">{item.name}</span>
                                        </div>
                                      </button>
                                    );
                                  }

                                  const ItemComponent = item.external ? 'a' : Link;
                                  const itemProps = item.external
                                    ? { href: item.href, target: '_blank', rel: 'noopener noreferrer' }
                                    : { to: item.href };

                                  return (
                                    <ItemComponent
                                      key={item.name}
                                      {...itemProps}
                                      className={`group flex items-center gap-x-3 rounded-lg p-3 transition-colors cursor-pointer ${
                                        isItemActive
                                          ? 'bg-blue-50 text-blue-700'
                                          : 'text-gray-700 hover:bg-gray-50'
                                      }`}
                                    >
                                      <div className={`flex-none rounded-lg p-2 ${
                                        isItemActive
                                          ? 'bg-blue-100'
                                          : 'bg-gray-50 group-hover:bg-white'
                                      }`}>
                                        <svg className={`w-5 h-5 ${
                                          isItemActive
                                            ? 'text-blue-600'
                                            : 'text-gray-600 group-hover:text-blue-600'
                                        }`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={item.icon} />
                                        </svg>
                                      </div>
                                      <div className="flex-auto">
                                        <span className="font-semibold">{item.name}</span>
                                        {item.external && (
                                          <svg className="inline-block w-3 h-3 ml-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                                          </svg>
                                        )}
                                      </div>
                                    </ItemComponent>
                                  );
                                })}
                              </div>
                            </div>
                          </PopoverPanel>
                        </div>
                      )}
                    </Popover>
                  );
                })}
              </div>
            
            </div>

            {/* User Menu */}
            <div className="flex items-center">
              <Popover className="relative">
                {({ open, close }) => (
                  <div
                    onMouseEnter={(e) => {
                      const button = e.currentTarget.querySelector('button');
                      if (button && !open) {
                        button.click();
                      }
                    }}
                    onMouseLeave={() => {
                      if (open) {
                        close();
                      }
                    }}
                  >
                    <PopoverButton className="flex items-center gap-3 focus:outline-none">
                      <div className="hidden sm:flex sm:flex-col sm:items-end">
                        <p className="text-sm font-medium text-gray-900">
                          {displayName}
                        </p>
                        <p className="text-xs text-gray-500">
                          {syncedUser?.isSystemAdmin ? 'System Administrator' : 'User'}
                        </p>
                      </div>
                      <div className="w-10 h-10 bg-gradient-to-br from-blue-400 to-blue-600 rounded-full flex items-center justify-center">
                        <span className="text-white font-semibold text-sm">
                          {userInitial}
                        </span>
                      </div>
                      <ChevronDownIcon
                        aria-hidden="true"
                        className={`w-4 h-4 text-gray-400 transition-transform ${open ? 'rotate-180' : ''}`}
                      />
                    </PopoverButton>

                    <PopoverPanel
                      transition
                      className="absolute right-0 z-10 mt-2 w-56 transition data-closed:translate-y-1 data-closed:opacity-0 data-enter:duration-200 data-enter:ease-out data-leave:duration-150 data-leave:ease-in"
                    >
                      <div className="overflow-hidden rounded-lg shadow-lg ring-1 ring-gray-200 ring-opacity-50 bg-white">
                        <div className="p-2">
                          <button
                            onClick={() => {
                              close();
                              setShowProfileModal(true);
                            }}
                            className="w-full group flex items-center gap-x-3 rounded-lg p-3 transition-colors text-gray-700 hover:bg-gray-50"
                          >
                            <div className="flex-none rounded-lg p-2 bg-gray-50 group-hover:bg-white">
                              <svg className="w-5 h-5 text-gray-600 group-hover:text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                              </svg>
                            </div>
                            <span className="font-semibold">My Profile</span>
                          </button>
                          <button
                            onClick={handleLogout}
                            className="w-full group flex items-center gap-x-3 rounded-lg p-3 transition-colors text-gray-700 hover:bg-gray-50"
                          >
                            <div className="flex-none rounded-lg p-2 bg-gray-50 group-hover:bg-white">
                              <svg className="w-5 h-5 text-gray-600 group-hover:text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                              </svg>
                            </div>
                            <span className="font-semibold">Logout</span>
                          </button>
                        </div>
                      </div>
                    </PopoverPanel>
                  </div>
                )}
              </Popover>
            </div>
          
          </div>
        </div>
      </nav>

      {/* Breadcrumb Navigation */}
      {breadcrumbs.length > 1 && (
        <div className="bg-white border-b border-gray-200">
          <div className="mx-auto px-4 sm:px-6 lg:px-8">
            <nav className="flex py-3" aria-label="Breadcrumb">
              <ol className="flex items-center space-x-2">
                {breadcrumbs.map((crumb, index) => (
                  <li key={crumb.name} className="flex items-center">
                    {index > 0 && (
                      <svg className="flex-shrink-0 w-5 h-5 text-gray-400 mx-2" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clipRule="evenodd" />
                      </svg>
                    )}
                    {index < breadcrumbs.length - 1 ? (
                      <Link
                        to={crumb.href}
                        className="text-sm text-gray-500 hover:text-gray-700"
                      >
                        {crumb.name}
                      </Link>
                    ) : (
                      <span className="text-sm font-medium text-gray-900">{crumb.name}</span>
                    )}
                  </li>
                ))}
              </ol>
            </nav>
          </div>
        </div>
      )}

      {/* Main Content */}
      <main className="py-6">
        <div className="mx-auto px-4 sm:px-6 lg:px-8">
          {children}
        </div>
      </main>

      {/* Profile Edit Modal */}
      {showProfileModal && (
        <div className="fixed inset-0 z-50 overflow-y-auto">
          {/* Backdrop */}
          <div
            className="fixed inset-0 bg-black/50 transition-opacity"
            onClick={() => setShowProfileModal(false)}
          />

          {/* Modal Content */}
          <div className="flex min-h-full items-center justify-center p-4">
            <div className="relative bg-white rounded-lg shadow-xl w-full max-w-2xl">
              <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
                <h3 className="text-lg font-medium text-gray-900">Edit Profile</h3>
                <button
                  onClick={() => setShowProfileModal(false)}
                  className="text-gray-400 hover:text-gray-600"
                >
                  <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              <div className="px-6 py-4">
                <Suspense
                  fallback={
                    <div className="flex items-center justify-center p-12">
                      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
                    </div>
                  }
                >
                  <UserEditor
                    mode="profile"
                    user={profileUserData}
                    onSave={handleSaveProfile}
                    onCancel={() => setShowProfileModal(false)}
                    isModal={true}
                  />
                </Suspense>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Admin Tools Drawer with ESB Config and Deployment tabs */}
      <AdminToolsDrawer
        isOpen={showAdminTools}
        onClose={() => setShowAdminTools(false)}
        onOpen={() => setShowAdminTools(true)}
      />
    </div>
  );
};

export default Layout;
