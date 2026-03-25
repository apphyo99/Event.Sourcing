import { useMsal } from '@azure/msal-react';
import { loginRequest } from '../config/authConfig';
import { Package } from 'lucide-react';

const LoginPage = () => {
  const { instance } = useMsal();

  const handleLogin = () => {
    instance.loginRedirect(loginRequest).catch((error) => {
      console.error('Login error:', error);
    });
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div className="text-center">
          <Package className="mx-auto h-12 w-12 text-blue-600" />
          <h2 className="mt-6 text-3xl font-extrabold text-gray-900">
            Event Sourcing Demo
          </h2>
          <p className="mt-2 text-sm text-gray-600">
            Sign in to access the order management system
          </p>
        </div>
        <div className="mt-8 space-y-6">
          <button
            onClick={handleLogin}
            className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Sign in with Microsoft
          </button>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
