import { lazy, Suspense } from 'react';
import { useAuth } from '../context/AuthContext';
import Layout from '../components/Layout';

// Dynamically import the UserProfile component from the Admin MFE
const UserProfile = lazy(() => import('adminMfe/UserProfile'));

function Profile() {
  const { user, syncedUser, refreshUser } = useAuth();

  return (
    <Layout>
      <Suspense
        fallback={
          <div className="flex items-center justify-center p-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
          </div>
        }
      >
        <UserProfile user={user} syncedUser={syncedUser} onProfileUpdate={refreshUser} />
      </Suspense>
    </Layout>
  );
}

export default Profile;
