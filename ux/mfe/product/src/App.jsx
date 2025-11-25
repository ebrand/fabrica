import { useState } from 'react'
import ProductManagement from './pages/ProductManagement'
import CategoryManagement from './pages/CategoryManagement'

function App() {
  const [currentPage, setCurrentPage] = useState('products')

  return (
    <div className="min-h-screen bg-gray-100">
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex">
              <div className="flex space-x-8">
                <button
                  onClick={() => setCurrentPage('products')}
                  className={`inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium ${
                    currentPage === 'products'
                      ? 'border-blue-500 text-gray-900'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  }`}
                >
                  Products
                </button>
                <button
                  onClick={() => setCurrentPage('categories')}
                  className={`inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium ${
                    currentPage === 'categories'
                      ? 'border-blue-500 text-gray-900'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  }`}
                >
                  Categories
                </button>
              </div>
            </div>
          </div>
        </div>
      </nav>

      <main>
        {currentPage === 'products' && <ProductManagement />}
        {currentPage === 'categories' && <CategoryManagement />}
      </main>
    </div>
  )
}

export default App
