import type { Config } from 'tailwindcss';

export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        obsidian: { 950: '#05080F', 900: '#0A0F18', 800: '#101724', 700: '#182231' },
        bronze: { 300: '#F0D48A', 400: '#DDB755', 500: '#C9A227', 600: '#9A7B1E', 700: '#6E5615' },
        jade: { 300: '#7FE7CB', 400: '#4CCBA8', 500: '#1FA588', 600: '#137A66', 700: '#0E5B4D' },
        son: { 400: '#E4604E', 500: '#C43D2E' },
        ivory: '#EAE4D3',
      },
      fontFamily: {
        display: ['"Cormorant"', 'serif'],
        body: ['"Be Vietnam Pro"', 'sans-serif'],
        tech: ['"IBM Plex Mono"', 'monospace'],
      },
    },
  },
  plugins: [],
} satisfies Config;
