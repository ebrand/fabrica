import { useEffect, useState } from 'react';
import SwaggerUI from 'swagger-ui-react';
import 'swagger-ui-react/swagger-ui.css';

function SwaggerViewer({ url, title }) {
  const [error, setError] = useState(null);

  useEffect(() => {
    setError(null);
  }, [url]);

  const onError = (err) => {
    console.error('Swagger UI Error:', err);
    setError('Failed to load API documentation');
  };

  return (
    <div className="swagger-container">
      <div className="mb-4">
        <h3 className="text-xl font-semibold text-gray-900">{title}</h3>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      <div className="bg-white rounded-lg shadow overflow-hidden">
        <SwaggerUI
          url={url}
          docExpansion="list"
          defaultModelsExpandDepth={1}
          onComplete={(system) => {
            if (system.errActions) {
              system.errActions.clear({ source: 'fetch' });
            }
          }}
        />
      </div>
    </div>
  );
}

export default SwaggerViewer;
