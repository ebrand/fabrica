import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useStytch } from '@stytch/react';
import { useAuth } from '../context/AuthContext';

const Login = () => {
  const stytch = useStytch();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();

  useEffect(() => {
    // Handle OAuth callback
    const urlParams = new URLSearchParams(window.location.search);
    const token = urlParams.get('token');
    const stytchTokenType = urlParams.get('stytch_token_type');

    if (token && stytchTokenType === 'oauth') {
      // Authenticate with the OAuth token
      stytch.oauth.authenticate(token, {
        session_duration_minutes: 60
      }).then(() => {
        navigate('/');
      }).catch((error) => {
        console.error('OAuth authentication error:', error);
      });
    } else if (isAuthenticated) {
      navigate('/');
    }
  }, [isAuthenticated, navigate, stytch]);

  const handleGoogleLogin = async () => {
    const redirectURL = `${window.location.origin}/authenticate`;

    try {
      await stytch.oauth.google.start({
        oauth_request_identifier: crypto.randomUUID(),
        login_redirect_url: redirectURL,
        signup_redirect_url: redirectURL,
      });
    } catch (error) {
      console.error('OAuth error:', error);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100">
      <div className="max-w-md w-full mx-4">
        <div className="bg-white rounded-2xl shadow-xl p-8">
          {/* Logo */}
          <div className="flex justify-center mb-8">
            <div className="w-16 h-16 bg-gradient-to-br from-blue-500 to-blue-700 rounded-2xl flex items-center justify-center shadow-lg">
              <svg className="w-10 h-10 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
            </div>
          </div>

          {/* Title */}
          <h1 className="text-3xl font-bold text-center text-gray-900 mb-2">
            Fabrica Admin
          </h1>
          <p className="text-center text-gray-600 mb-8">
            Sign in to access your admin dashboard
          </p>

          {/* Google OAuth Button */}
          <button
            onClick={handleGoogleLogin}
            className="w-full flex items-center justify-center gap-3 bg-white border-2 border-gray-300 rounded-lg px-6 py-3 text-gray-700 font-medium hover:bg-gray-50 hover:border-gray-400 transition-all duration-200 shadow-sm hover:shadow-md"
          >
            <svg className="w-5 h-5" viewBox="0 0 24 24">
              <path
                fill="#4285F4"
                d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
              />
              <path
                fill="#34A853"
                d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
              />
              <path
                fill="#FBBC05"
                d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
              />
              <path
                fill="#EA4335"
                d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
              />
            </svg>
            Sign in with Google
          </button>

          {/* Divider */}
          <div className="relative my-6">
            <div className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-gray-300"></div>
            </div>
            <div className="relative flex justify-center text-sm">
              <span className="px-2 bg-white text-gray-500">Secure OAuth Authentication</span>
            </div>
          </div>

          {/* Info */}
          <p className="text-center text-sm text-gray-500">
            Authentication powered by{' '}
            <a href="https://stytch.com" target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:text-blue-700 font-medium">
              Stytch
            </a>
          </p>
        </div>

        {/* Footer */}
        <p className="text-center text-sm text-gray-600 mt-6">
          © 2025 Fabrica Commerce Cloud. All rights reserved.
        </p>
      </div>
    </div>
  );
};

export default Login;
