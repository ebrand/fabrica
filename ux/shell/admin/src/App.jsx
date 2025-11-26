import { useState, useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { StytchProvider } from '@stytch/react';
import { StytchUIClient } from '@stytch/vanilla-js';
import { AuthProvider } from './context/AuthContext';
import ErrorBoundary from './components/ErrorBoundary';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Users from './pages/Users';
import Products from './pages/Products';
import Categories from './pages/Categories';
import BlockManagement from './pages/BlockManagement';
import ContentManagement from './pages/ContentManagement';
import ApiDocs from './pages/ApiDocs';
import Profile from './pages/Profile';
import ProtectedRoute from './components/ProtectedRoute';
import configService from './services/config';
import './index.css';

function App() {
  const [stytch, setStytch] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Initialize Stytch with config from Vault
    async function initializeStytch() {
      try {
        const publicToken = await configService.getStytchPublicToken();
        const stytchClient = new StytchUIClient(publicToken);
        setStytch(stytchClient);
      } catch (error) {
        console.error('Failed to initialize Stytch:', error);
      } finally {
        setLoading(false);
      }
    }

    initializeStytch();
  }, []);

  if (loading || !stytch) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
          <p className="mt-4 text-gray-600">Loading configuration from Vault...</p>
        </div>
      </div>
    );
  }

  const errorFallback = (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="text-center">
        <div className="text-red-500 text-6xl mb-4">!</div>
        <h1 className="text-xl font-bold text-gray-900">Something went wrong</h1>
        <p className="text-gray-600 mt-2">Please refresh the page or try again later</p>
        <button
          onClick={() => window.location.reload()}
          className="mt-4 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          Refresh Page
        </button>
      </div>
    </div>
  );

  return (
    <StytchProvider stytch={stytch}>
      <AuthProvider>
        <ErrorBoundary name="AppRoot" fallback={errorFallback}>
          <BrowserRouter>
            <Routes>
              <Route path="/login" element={<Login />} />
              <Route path="/authenticate" element={<Login />} />
              <Route path="/" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
              <Route path="/users" element={<ProtectedRoute><Users /></ProtectedRoute>} />
              <Route path="/products" element={<ProtectedRoute><Products /></ProtectedRoute>} />
              <Route path="/categories" element={<ProtectedRoute><Categories /></ProtectedRoute>} />
              <Route path="/content/blocks" element={<ProtectedRoute><BlockManagement /></ProtectedRoute>} />
              <Route path="/content/manage" element={<ProtectedRoute><ContentManagement /></ProtectedRoute>} />
              <Route path="/roles" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
              <Route path="/permissions" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
              <Route path="/api-docs" element={<ProtectedRoute><ApiDocs /></ProtectedRoute>} />
              <Route path="/profile" element={<ProtectedRoute><Profile /></ProtectedRoute>} />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </BrowserRouter>
        </ErrorBoundary>
      </AuthProvider>
    </StytchProvider>
  );
}

export default App;
