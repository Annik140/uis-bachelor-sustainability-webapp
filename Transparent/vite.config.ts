import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // proxy API requests to the ASP.NET backend during development
      '/admin': {
        target: 'http://localhost:5120',
        changeOrigin: true,
        secure: false,
        // Only bypass real frontend routes. Admin API GETs (e.g. /admin/clothingbrands)
        // must still be proxied to the backend.
        bypass(req) {
          if (req.method !== 'GET') {
            return;
          }

          const url = req.url ?? '';
          const isFrontendAdminRoute =
            url === '/admin' ||
            url === '/admin/' ||
            url === '/admin/login' ||
            url === '/admin/dashboard' ||
            url === '/admin/brands/new' ||
            /^\/admin\/brands\/\d+\/edit$/.test(url);

          if (isFrontendAdminRoute) {
            return url;
          }
        }
      },
      '/brands': {
        target: 'http://localhost:5120',
        changeOrigin: true,
        secure: false,
      }
    }
  }
})

