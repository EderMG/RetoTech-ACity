/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  theme: {
    extend: {
      colors: {
        ink: "#1c1917",
        clay: "#b45309",
        surface: "#faf7f2"
      }
    }
  },
  plugins: []
};
