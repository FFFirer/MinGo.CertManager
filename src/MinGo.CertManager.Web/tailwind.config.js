/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./Pages/**/*.{razor,cshtml}",
    "./Shared/**/*.{razor,cshtml}",
    "./Components/**/*.{razor,cshtml}"
  ],
  theme: {
    extend: {},
  },
  plugins: [],
  darkMode: 'class'
}
