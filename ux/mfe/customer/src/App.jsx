import { useState } from 'react'
import CustomerManagement from './pages/CustomerManagement'
import SegmentManagement from './pages/SegmentManagement'

function App() {
  const [currentPage, setCurrentPage] = useState('customers')

  return (
    <div className="min-h-screen bg-gray-100">
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex">
              <div className="flex space-x-8">
                <button
                  onClick={() => setCurrentPage('customers')}
                  className={`inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium ${
                    currentPage === 'customers'
                      ? 'border-blue-500 text-gray-900'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  }`}
                >
                  Customers
                </button>
                <button
                  onClick={() => setCurrentPage('segments')}
                  className={`inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium ${
                    currentPage === 'segments'
                      ? 'border-blue-500 text-gray-900'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  }`}
                >
                  Segments
                </button>
              </div>
            </div>
          </div>
        </div>
      </nav>

      <main>
        {currentPage === 'customers' && <CustomerManagement />}
        {currentPage === 'segments' && <SegmentManagement />}
      </main>
    </div>
  )
}

export default App
