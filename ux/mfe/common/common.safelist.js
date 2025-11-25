/**
 * Common UI Components Safelist
 *
 * This file contains all Tailwind CSS classes used by the mfe-common components.
 * Import this file into your shell's tailwind.config.js to ensure all required
 * classes are generated even when using CSS purging.
 *
 * Usage in tailwind.config.js:
 *   import commonSafelist from '../../mfe/common/common.safelist.js'
 *
 *   export default {
 *     safelist: [
 *       ...commonSafelist,
 *       // your other safelist classes...
 *     ]
 *   }
 */

const commonSafelist = [
  // ===================
  // Layout & Positioning
  // ===================
  'fixed', 'relative', 'absolute', 'inset-0', 'inset-y-0',
  'z-10', 'z-50',
  'left-0', 'right-0', 'top-0', 'bottom-0',

  // ===================
  // Sizing
  // ===================
  'h-5', 'w-5', 'h-6', 'w-6',
  'w-full',
  'max-w-sm', 'max-h-60',

  // ===================
  // Flexbox & Grid
  // ===================
  'flex', 'inline-flex', 'block',
  'flex-col',
  'items-start', 'items-center', 'items-end',
  'justify-center', 'justify-between',
  'gap-3',
  'space-y-4',

  // ===================
  // Spacing
  // ===================
  'p-3', 'p-4', 'p-6',
  'px-2', 'px-3', 'px-4', 'py-1', 'py-2', 'py-6',
  'pl-3', 'pl-10', 'pr-2', 'pr-4', 'pr-10',
  'mb-1', 'ml-3', 'ml-4', 'mt-1', 'mt-2',
  'sm:p-6',

  // ===================
  // Typography
  // ===================
  'text-sm', 'text-base', 'text-lg', 'text-xl', 'text-2xl',
  'font-medium', 'font-semibold', 'font-bold', 'font-normal',
  'text-left',
  'truncate',
  'sm:text-sm',

  // ===================
  // Colors - Text
  // ===================
  'text-white',
  'text-gray-400', 'text-gray-500', 'text-gray-700', 'text-gray-900',
  'text-blue-600',
  'text-green-400', 'text-red-400', 'text-yellow-400',

  // ===================
  // Colors - Background
  // ===================
  'bg-white',
  'bg-blue-600',
  'bg-gray-50', 'bg-gray-100', 'bg-gray-200', 'bg-gray-800',

  // ===================
  // Borders
  // ===================
  'border', 'border-gray-300', 'border-gray-400', 'border-transparent',
  'border-blue-500',
  'rounded-md', 'rounded-lg', 'rounded-r-md',

  // ===================
  // Ring (for focus states)
  // ===================
  'ring-1', 'ring-black', 'ring-opacity-5', 'ring-blue-500',

  // ===================
  // Effects & Shadows
  // ===================
  'shadow-sm', 'shadow-lg',
  'opacity-25',

  // ===================
  // Transitions & Animations
  // ===================
  'transition', 'transform',
  'duration-100', 'duration-200', 'duration-300',
  'ease-in', 'ease-out',

  // ===================
  // Interactivity
  // ===================
  'cursor-default', 'cursor-pointer', 'cursor-not-allowed',
  'select-none',
  'overflow-auto', 'overflow-hidden',
  'pointer-events-none', 'pointer-events-auto',

  // ===================
  // Focus States
  // ===================
  'focus:outline-none',
  'focus:border-blue-500',
  'focus:ring-1', 'focus:ring-blue-500',

  // ===================
  // Hover States
  // ===================
  'hover:border-gray-400',
  'hover:bg-gray-50', 'hover:bg-gray-100',
  'hover:bg-blue-600', 'hover:text-white',
  'hover:text-gray-500',

  // ===================
  // Group Hover States
  // ===================
  'group', 'group-hover:text-white',

  // ===================
  // Screen Reader
  // ===================
  'sr-only',

  // ===================
  // Toast Component Classes
  // ===================
  'bg-green-50', 'bg-red-50', 'bg-yellow-50', 'bg-blue-50',
  'text-green-400', 'text-green-800',
  'text-red-400', 'text-red-800',
  'text-yellow-400', 'text-yellow-800',
  'text-blue-400', 'text-blue-800',
  'border-green-200', 'border-red-200', 'border-yellow-200', 'border-blue-200',
]

export default commonSafelist
