import Combobox from './components/Combobox'
import Select from './components/Select'
import RadioCards from './components/RadioCards'
import Toast from './components/Toast'

// Demo app for testing common components standalone
function App() {
  return (
    <div className="p-8 space-y-8">
      <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Common Components</h1>

      <section>
        <h2 className="text-lg font-medium mb-4 text-gray-900 dark:text-white">Combobox</h2>
        <Combobox
          label="Assigned to"
          options={[
            { id: 1, name: 'Leslie Alexander' },
            { id: 2, name: 'Michael Foster' },
            { id: 3, name: 'Dries Vincent' },
          ]}
        />
      </section>

      <section>
        <h2 className="text-lg font-medium mb-4 text-gray-900 dark:text-white">Select</h2>
        <Select
          label="Assigned to"
          options={[
            { id: 1, name: 'Wade Cooper' },
            { id: 2, name: 'Arlene Mccoy' },
            { id: 3, name: 'Devon Webb' },
          ]}
        />
      </section>

      <section>
        <h2 className="text-lg font-medium mb-4 text-gray-900 dark:text-white">Radio Cards</h2>
        <RadioCards
          label="RAM"
          helpLink={{ text: 'See performance specs', href: '#' }}
          options={[
            { id: '4gb', name: '4 GB', inStock: true },
            { id: '8gb', name: '8 GB', inStock: true },
            { id: '16gb', name: '16 GB', inStock: true },
            { id: '32gb', name: '32 GB', inStock: false },
          ]}
          defaultValue="8gb"
        />
      </section>

      <section>
        <h2 className="text-lg font-medium mb-4 text-gray-900 dark:text-white">Toast</h2>
        <Toast
          title="Successfully saved!"
          message="Anyone with a link can now view this file."
        />
      </section>
    </div>
  )
}

export default App
