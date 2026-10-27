// HashRouter (bukan BrowserRouter): GitHub Pages hosting statis murni, tidak bisa rewrite
// server-side ke index.html untuk path dalam-SPA — refresh di selain "/" akan 404 kalau
// pakai BrowserRouter. HashRouter (URL berbentuk .../#/items) jalan tanpa konfigurasi server.
import { useState } from 'react';
import { HashRouter, NavLink, Navigate, Route, Routes } from 'react-router-dom';
import { clearSession, getUsername, isAuthenticated } from './api/auth';
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

const navGroups = [
  {
    group: 'Ringkasan',
    items: [{ to: '/', label: 'Dashboard' }],
  },
  {
    group: 'Master Data',
    items: [
      { to: '/items', label: 'Items' },
      { to: '/uoms', label: 'Satuan Ukur' },
      { to: '/suppliers', label: 'Suppliers' },
      { to: '/prices', label: 'Harga Supplier' },
      { to: '/boms', label: 'BOM' },
    ],
  },
  {
    group: 'Transaksi',
    items: [
      { to: '/mrp', label: 'MRP' },
      { to: '/production', label: 'Produksi' },
      { to: '/inventory', label: 'Stok' },
    ],
  },
  {
    group: 'Costing & Harga',
    items: [
      { to: '/costing', label: 'Costing' },
      { to: '/pricing', label: 'Harga Jual' },
    ],
  },
  {
    group: 'Pengaturan',
    items: [
      { to: '/channels', label: 'Kanal Penjualan' },
      { to: '/pricing-settings', label: 'Setelan Harga' },
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
          <span className="brand-mark">BOM</span>
        </div>

        <div className={navOpen ? 'nav-overlay open' : 'nav-overlay'} onClick={() => setNavOpen(false)} />

        <aside className={navOpen ? 'sidebar open' : 'sidebar'}>
          <div className="brand">
            <span className="brand-mark">BOM</span>
            <span className="brand-sub">Bill of Materials</span>
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
