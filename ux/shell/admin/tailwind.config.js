/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  // Safelist classes used by MFE components that inherit shell's Tailwind
  safelist: [
    // ===================
    // Common UI Components (Select, Combobox, RadioCards, Toast)
    // ===================
    // Layout & Positioning
    'fixed', 'relative', 'absolute', 'inset-0', 'inset-y-0',
    'z-10', 'z-50',
    'left-0', 'right-0', 'top-0', 'bottom-0',
    // Sizing
    'h-5', 'w-5', 'h-6', 'w-6', 'w-full', 'max-w-sm', 'max-h-60',
    // Flexbox
    'flex', 'inline-flex', 'block', 'flex-col',
    'items-start', 'items-center', 'items-end',
    'justify-center', 'justify-between', 'gap-3', 'space-y-4',
    // Spacing
    'p-3', 'p-4', 'p-6', 'px-2', 'px-3', 'px-4', 'py-1', 'py-2', 'py-6',
    'pl-3', 'pl-10', 'pr-2', 'pr-4', 'pr-10',
    'mb-1', 'ml-3', 'ml-4', 'mt-1', 'mt-2', 'sm:p-6',
    // Typography
    'text-sm', 'text-base', 'text-lg', 'text-xl', 'text-2xl',
    'font-medium', 'font-semibold', 'font-bold', 'font-normal',
    'text-left', 'truncate', 'sm:text-sm',
    // Colors - Text
    'text-white', 'text-gray-400', 'text-gray-500', 'text-gray-700', 'text-gray-900',
    'text-blue-600', 'text-blue-900',
    'text-green-400', 'text-green-800', 'text-red-400', 'text-red-800',
    'text-yellow-400', 'text-yellow-800', 'text-blue-400', 'text-blue-800',
    // Colors - Background
    'bg-white', 'bg-blue-600', 'bg-blue-50',
    'bg-gray-50', 'bg-gray-100', 'bg-gray-200', 'bg-gray-800',
    'bg-green-50', 'bg-red-50', 'bg-yellow-50',
    // Borders
    'border', 'border-gray-300', 'border-gray-400', 'border-transparent', 'border-blue-500',
    'border-green-200', 'border-red-200', 'border-yellow-200', 'border-blue-200',
    'rounded-md', 'rounded-lg', 'rounded-r-md',
    // Ring & Shadow
    'ring-1', 'ring-black', 'ring-black/5', 'ring-opacity-5', 'ring-blue-500',
    'shadow-sm', 'shadow-lg', 'opacity-25',
    // Transitions
    'transition', 'transform', 'duration-100', 'duration-200', 'duration-300', 'ease-in', 'ease-out',
    // Interactivity
    'cursor-default', 'cursor-pointer', 'cursor-not-allowed', 'select-none',
    'overflow-auto', 'overflow-hidden', 'pointer-events-none', 'pointer-events-auto',
    // Focus States
    'focus:outline-none', 'focus:border-blue-500', 'focus:ring-1', 'focus:ring-blue-500',
    // Hover States - CRITICAL for Select/Combobox dropdowns
    'hover:border-gray-400', 'hover:bg-gray-50', 'hover:bg-gray-100',
    'hover:bg-blue-600', 'hover:text-white', 'hover:text-gray-500',
    // Group hover for checkmark icon
    'group', 'group-hover:text-white',
    // Screen Reader
    'sr-only',
    // ===================
    // Admin shell specific classes
    // ===================
    // Layout & positioning
    'fixed', 'relative', 'inset-0', 'z-50',
    'min-h-full', 'min-w-full', 'h-4', 'h-6', 'h-12', 'w-4', 'w-6', 'w-8', 'w-12', 'w-full',
    'max-w-2xl', 'max-w-3xl',
    // Flexbox & Grid
    'flex', 'grid', 'inline-flex', 'items-center', 'justify-center', 'justify-end', 'justify-between',
    'gap-2', 'gap-3', 'gap-4', 'gap-6',
    'grid-cols-1', 'grid-cols-12', 'sm:grid-cols-2',
    'col-span-2', 'col-span-3', 'col-span-5',
    // Spacing
    'p-4', 'p-6', 'p-12', 'px-2', 'px-3', 'px-4', 'px-6', 'py-0', 'py-2', 'py-3', 'py-4', 'py-6',
    'mx-auto', 'mt-1', 'mb-4', 'ml-2', 'sm:px-6', 'lg:px-8',
    'space-x-8', 'space-y-2', 'space-y-6',
    // Borders & Rounded
    'border', 'border-t', 'border-r', 'border-b', 'border-b-2', 'border-transparent',
    'border-gray-200', 'border-gray-300', 'border-green-200', 'border-red-200', 'border-yellow-200', 'border-blue-600',
    'rounded', 'rounded-md', 'rounded-lg', 'rounded-full',
    'divide-y', 'divide-gray-200',
    // Background colors
    'bg-white', 'bg-gray-50', 'bg-gray-100', 'bg-blue-600',
    'bg-green-50', 'bg-red-50', 'bg-yellow-50', 'bg-purple-100',
    'bg-black/50',
    // Text colors
    'text-white', 'text-gray-400', 'text-gray-500', 'text-gray-600', 'text-gray-700', 'text-gray-900',
    'text-blue-600', 'text-green-700', 'text-red-600', 'text-red-700', 'text-yellow-700', 'text-purple-600', 'text-purple-800',
    // Text sizing & alignment
    'text-xs', 'text-sm', 'text-lg', 'text-xl', 'text-left', 'text-center', 'text-right',
    'font-medium', 'font-semibold', 'leading-5', 'tracking-wider', 'uppercase', 'whitespace-nowrap',
    // Effects
    'shadow', 'shadow-sm', 'shadow-xl', 'animate-spin', 'rotate-90',
    'transition-colors', 'transition-opacity',
    'overflow-hidden', 'overflow-x-auto', 'overflow-y-auto',
    // Interactive states
    'cursor-pointer', 'hover:bg-gray-50', 'hover:bg-blue-700', 'hover:bg-white',
    'hover:text-gray-600', 'hover:text-blue-900', 'hover:text-red-900',
    'focus:outline-none', 'focus:ring-2', 'focus:ring-blue-500', 'focus:ring-offset-2', 'focus:border-blue-500',
    'disabled:opacity-50', 'disabled:cursor-not-allowed',
    // Table
    'align-top', 'sm:text-sm',
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
          800: '#1e40af',
          900: '#1e3a8a',
        },
      },
    },
  },
  plugins: [],
}
