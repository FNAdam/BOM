// HashRouter (bukan BrowserRouter): GitHub Pages hosting statis murni, tidak bisa rewrite
// server-side ke index.html untuk path dalam-SPA — refresh di selain "/" akan 404 kalau
// pakai BrowserRouter. HashRouter (URL berbentuk .../#/items) jalan tanpa konfigurasi server.
import { useState } from 'react';
import { HashRouter, NavLink, Navigate, Route, Routes } from 'react-router-dom';
import { clearSession, getUsername, isAuthenticated } from './api/auth';
import logo from './assets/logo.png';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';
import { ItemsPage } from './pages/ItemsPage';
import { UomsPage } from './pages/UomsPage';
import { SuppliersPage } from './pages/SuppliersPage';
import { ItemPricesPage } from './pages/ItemPricesPage';
import { BomsPage } from './pages/BomsPage';
import { BomEditorPage } from './pages/BomEditorPage';
import { CostingPage } from './pages/CostingPage';
import { MrpPage } from './pages/MrpPage';
import { InventoryPage } from './pages/InventoryPage';
import { ProductionPage } from './pages/ProductionPage';
import { PricingPage } from './pages/PricingPage';
import { SalesChannelsPage } from './pages/SalesChannelsPage';
import { PricingSettingsPage } from './pages/PricingSettingsPage';
import { PurchaseOrdersPage } from './pages/PurchaseOrdersPage';
import { SalesOrdersPage } from './pages/SalesOrdersPage';
import { ReceiptPage } from './pages/ReceiptPage';

// Dikelompokkan mengikuti alur kerja asli (data dasar -> resep -> biaya -> produksi -> harga
// jual), bukan abjad - supaya menu kelihatan seperti langkah, bukan daftar fitur acak.
// Istilah teknis (BOM, MRP, COGS) tetap ditulis dalam kurung supaya yang sudah familiar tidak
// kebingungan, sementara nama depannya pakai bahasa sehari-hari.
const navGroups = [
  {
    group: 'Ringkasan',
    items: [{ to: '/', label: 'Dashboard', icon: '🏠' }],
  },
  {
    group: '1. Data Dasar',
    items: [
      { to: '/items', label: 'Bahan & Produk', icon: '📦' },
      { to: '/uoms', label: 'Satuan Ukur', icon: '📏' },
      { to: '/suppliers', label: 'Supplier', icon: '🚚' },
      { to: '/prices', label: 'Harga dari Supplier', icon: '🧾' },
    ],
  },
  {
    group: '2. Resep (BOM)',
    items: [{ to: '/boms', label: 'Resep Produk (BOM)', icon: '📋' }],
  },
  {
    group: '3. Hitung Biaya',
    items: [{ to: '/costing', label: 'Biaya Produksi (Costing)', icon: '🧮' }],
  },
  {
    group: '4. Produksi & Stok',
    items: [
      { to: '/mrp', label: 'Rencana Belanja (MRP)', icon: '🛒' },
      { to: '/purchase-orders', label: 'Pesanan Pembelian', icon: '📥' },
      { to: '/production', label: 'Produksi (COGS)', icon: '🏭' },
      { to: '/inventory', label: 'Stok Barang', icon: '📊' },
    ],
  },
  {
    group: '5. Harga Jual',
    items: [
      { to: '/sales-orders', label: 'Pesanan Penjualan', icon: '🧾' },
      { to: '/pricing', label: 'Tetapkan Harga Jual', icon: '🏷️' },
      { to: '/channels', label: 'Kanal Penjualan', icon: '🧭' },
      { to: '/pricing-settings', label: 'Setelan Harga', icon: '⚙️' },
    ],
  },
];

function App() {
  const [authed, setAuthed] = useState(isAuthenticated());
  const [navOpen, setNavOpen] = useState(false);

  if (!authed) {
    return <LoginPage onLoggedIn={() => setAuthed(true)} />;
  }

  function handleLogout() {
    clearSession();
    setAuthed(false);
  }

  return (
    <HashRouter>
      <div className="app-shell">
        <div className="mobile-topbar">
          <button className="hamburger-btn" onClick={() => setNavOpen(true)} aria-label="Buka menu">
            ☰
          </button>
          <span className="logo-badge">
            <img src={logo} alt="Ramuan BunSa" />
          </span>
        </div>

        <div className={navOpen ? 'nav-overlay open' : 'nav-overlay'} onClick={() => setNavOpen(false)} />

        <aside className={navOpen ? 'sidebar open' : 'sidebar'}>
          <div className="brand">
            <span className="logo-badge">
              <img src={logo} alt="Ramuan BunSa" />
            </span>
            <span className="brand-sub">Sistem Resep &amp; Biaya (BOM)</span>
          </div>
          <nav>
            {navGroups.map((group) => (
              <div className="nav-group" key={group.group}>
                <div className="nav-group-title">{group.group}</div>
                {group.items.map((item) => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    end={item.to === '/'}
                    onClick={() => setNavOpen(false)}
                    className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}
                  >
                    <span className="nav-icon" aria-hidden="true">{item.icon}</span>
                    {item.label}
                  </NavLink>
                ))}
              </div>
            ))}
          </nav>
          <div className="sidebar-footer">
            <button className="btn" onClick={handleLogout}>
              Keluar{getUsername() ? ` (${getUsername()})` : ''}
            </button>
          </div>
        </aside>

        <main className="content">
          <Routes>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/items" element={<ItemsPage />} />
            <Route path="/uoms" element={<UomsPage />} />
            <Route path="/suppliers" element={<SuppliersPage />} />
            <Route path="/prices" element={<ItemPricesPage />} />
            <Route path="/boms" element={<BomsPage />} />
            <Route path="/boms/new" element={<BomEditorPage />} />
            <Route path="/boms/:id/edit" element={<BomEditorPage />} />
            <Route path="/costing" element={<CostingPage />} />
            <Route path="/mrp" element={<MrpPage />} />
            <Route path="/purchase-orders" element={<PurchaseOrdersPage />} />
            <Route path="/sales-orders" element={<SalesOrdersPage />} />
            <Route path="/sales-orders/:id/receipt" element={<ReceiptPage />} />
            <Route path="/production" element={<ProductionPage />} />
            <Route path="/inventory" element={<InventoryPage />} />
            <Route path="/pricing" element={<PricingPage />} />
            <Route path="/channels" element={<SalesChannelsPage />} />
            <Route path="/pricing-settings" element={<PricingSettingsPage />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </main>
      </div>
    </HashRouter>
  );
}

export default App;
