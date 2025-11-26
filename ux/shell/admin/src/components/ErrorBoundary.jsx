import { Component } from 'react';

class ErrorBoundary extends Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error) {
    console.error('🔴 ErrorBoundary getDerivedStateFromError:', error?.message);
    return { hasError: true, error };
  }

  componentDidCatch(error, errorInfo) {
    console.error('🔴 ErrorBoundary componentDidCatch:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      console.log('🔴 ErrorBoundary rendering fallback for:', this.props.name || 'unnamed');
      // Render fallback UI
      return this.props.fallback || (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-sm text-red-600">
          <p className="font-medium">Failed to load component</p>
          {this.props.showError && (
            <p className="text-xs mt-1 text-red-500">{this.state.error?.message}</p>
          )}
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
