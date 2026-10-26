import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Base path otomatis jadi "/<nama-repo>/" saat dibuild di GitHub Actions (GITHUB_REPOSITORY
// = "owner/repo" tersedia otomatis), supaya aset ketemu saat di-deploy ke GitHub Pages
// (https://<owner>.github.io/<repo>/). Build lokal (dev/preview) tetap pakai "/" seperti biasa.
const repoName = process.env.GITHUB_REPOSITORY?.split('/')[1]

// https://vite.dev/config/
export default defineConfig({
  base: repoName ? `/${repoName}/` : '/',
  plugins: [react()],
})
