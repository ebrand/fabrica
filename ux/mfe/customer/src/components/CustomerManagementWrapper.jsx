// Note: No CSS import here - the shell provides Tailwind
import CustomerManagement from '../pages/CustomerManagement';

export default function CustomerManagementWrapper(props) {
  return <CustomerManagement {...props} />;
}
