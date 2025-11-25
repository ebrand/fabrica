import { useState } from 'react';
import UserManagement from './pages/UserManagement';
import ApiDocumentation from './pages/ApiDocumentation';
import './index.css';

function App() {
  const [currentPage, setCurrentPage] = useState('users');

  const pages = [
    { id: 'users', label: 'User Management', component: UserManagement },
    { id: 'api-docs', label: 'API Documentation', component: ApiDocumentation }
  ];

  const CurrentPageComponent = pages.find(p => p.id === currentPage)?.component;

  return (
    <div>
      {/* Navigation Tabs */}
      <div className="bg-white border-b border-gray-200">
        <div className="px-4 sm:px-6 lg:px-8">
          <nav className="flex space-x-8" aria-label="Tabs">
            {pages.map((page) => (
              <button
                key={page.id}
                onClick={() => setCurrentPage(page.id)}
                className={`py-4 px-1 border-b-2 font-medium text-sm transition-colors ${
                  currentPage === page.id
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                {page.label}
              </button>
            ))}
          </nav>
        </div>
      </div>

      {/* Page Content */}
      <div className="px-4 sm:px-6 lg:px-8 py-6">
        {CurrentPageComponent && <CurrentPageComponent />}
      </div>
    </div>
  );
}

export default App;
