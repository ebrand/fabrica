import { useState, useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { StytchProvider } from '@stytch/react';
import { StytchUIClient } from '@stytch/vanilla-js';
import { AuthProvider } from './context/AuthContext';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Users from './pages/Users';
import Products from './pages/Products';
import Categories from './pages/Categories';
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

  return (
    <StytchProvider stytch={stytch}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route path="/authenticate" element={<Login />} />
            <Route
              path="/*"
              element={
                <ProtectedRoute>
                  <Routes>
                    <Route path="/" element={<Dashboard />} />
                    <Route path="/users" element={<Users />} />
                    <Route path="/products" element={<Products />} />
                    <Route path="/categories" element={<Categories />} />
                    <Route path="/roles" element={<Dashboard />} />
                    <Route path="/permissions" element={<Dashboard />} />
                    <Route path="/api-docs" element={<ApiDocs />} />
                    <Route path="/profile" element={<Profile />} />
                    <Route path="*" element={<Navigate to="/" replace />} />
                  </Routes>
                </ProtectedRoute>
              }
            />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </StytchProvider>
  );
}

export default App;
