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
        // bypass GET requests (those are React routes like /admin/login, /admin/dashboard)
        bypass(req, _res, _opt) {
          if (req.method === 'GET') {
            return req.url;
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

