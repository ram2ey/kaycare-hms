import { Component } from 'react';
import type { ReactNode, ErrorInfo } from 'react';

interface Props {
  children: ReactNode;
  /** Optional custom fallback. If omitted, shows a styled error card. */
  fallback?: ReactNode;
}

interface State {
  error: Error | null;
}

/**
 * Catches render-time errors in the subtree and shows a readable error card
 * instead of a blank page. Wrap each major section of the app with this.
 */
export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // Log to console so developers can see the stack
    console.error('[ErrorBoundary] Uncaught render error:', error, info.componentStack);
  }

  handleReset = () => {
    this.setState({ error: null });
    // Navigate back to a safe page
    window.location.href = '/patients';
  };

  render() {
    if (this.state.error) {
      if (this.props.fallback) return this.props.fallback;

      return (
        <div className="flex h-full items-center justify-center p-10 bg-gray-50">
          <div className="bg-white rounded-2xl shadow-lg border border-red-100 p-8 max-w-xl w-full">
            <div className="flex items-center gap-3 mb-4">
              <span className="text-3xl">⚠️</span>
              <h2 className="text-xl font-semibold text-gray-800">Something went wrong</h2>
            </div>
            <p className="text-sm text-gray-500 mb-4">
              This page ran into an unexpected error. The message below can help diagnose the issue.
            </p>
            <pre className="bg-red-50 border border-red-200 rounded-lg p-4 text-xs text-red-800 overflow-auto max-h-48 whitespace-pre-wrap break-all mb-6">
              {this.state.error.message}
              {'\n\n'}
              {this.state.error.stack}
            </pre>
            <button
              onClick={this.handleReset}
              className="w-full bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium py-2.5 rounded-lg transition-colors"
            >
              Go back to Patients
            </button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
