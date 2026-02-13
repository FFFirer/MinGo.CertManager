/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./Pages/**/*.{razor,cshtml}",
    "./Shared/**/*.{razor,cshtml}",
    "./Components/**/*.{razor,cshtml}"
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        primary: 'rgb(0, 120, 212)',
        'primary-hover': 'rgb(0, 100, 180)',
        background: 'rgb(255, 255, 255)',
        foreground: 'rgb(23, 23, 23)',
        card: 'rgb(255, 255, 255)',
        'card-foreground': 'rgb(23, 23, 23)',
        muted: 'rgb(243, 244, 246)',
        'muted-foreground': 'rgb(115, 115, 115)',
        accent: 'rgb(243, 249, 255)',
        'accent-foreground': 'rgb(23, 23, 23)',
        border: 'rgb(229, 229, 229)',
        input: 'rgb(229, 229, 229)',
        destructive: 'rgb(220, 38, 38)',
      },
      dark: {
        background: 'rgb(32, 32, 32)',
        foreground: 'rgb(255, 255, 255)',
        card: 'rgb(44, 44, 44)',
        'card-foreground': 'rgb(255, 255, 255)',
        muted: 'rgb(60, 60, 60)',
        'muted-foreground': 'rgb(163, 163, 163)',
        accent: 'rgb(51, 51, 51)',
        'accent-foreground': 'rgb(255, 255, 255)',
        border: 'rgb(60, 60, 60)',
        input: 'rgb(60, 60, 60)',
      },
      fontFamily: {
        sans: ['"Segoe UI"', '"Segoe UI Web (West European)"', '-apple-system', 'BlinkMacSystemFont', '"Roboto"', '"Helvetica Neue"', 'sans-serif'],
      },
      boxShadow: {
        'sm': '0 2px 4px rgba(0, 0, 0, 0.08)',
        'md': '0 4px 8px rgba(0, 0, 0, 0.12)',
        'lg': '0 8px 16px rgba(0, 0, 0, 0.16)',
        'xl': '0 12px 24px rgba(0, 0, 0, 0.20)',
      },
      transitionDuration: {
        '200': '200ms',
      },
    },
  },
  plugins: [],
}
