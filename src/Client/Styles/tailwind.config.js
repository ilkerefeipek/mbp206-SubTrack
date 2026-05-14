/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Pages/**/*.razor',
    './Layout/**/*.razor',
    './**/*.razor',
    './wwwroot/index.html'
  ],
  theme: {
    extend: {
      colors: {
        'primary-dark': '#0D3B36',
        'primary':      '#0F766E',
        'primary-mid':  '#14B8A6',
        'primary-light':'#CCFBF1',
        'accent':       '#F59E0B',
        'success':      '#10B981',
        'warning':      '#F59E0B',
        'danger':       '#EF4444',
        'sidebar-bg':   '#0D3B36'
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'Segoe UI', 'sans-serif']
      }
    }
  },
  plugins: []
};
