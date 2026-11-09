import { useEffect, useState } from 'react';
import { dashboardApi } from '../api/dashboard';
import type { DashboardMarginDto } from '../api/types';
import { PageHeader } from '../components/PageHeader';
import { itemTypeLabel } from '../labels';

function rupiah(n: number): string {
  return n.toLocaleString('id-ID', {
    style: 'currency',
    currency: 'IDR',
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  });
}

function percent(n: number): string {
  return `${(n * 100).toLocaleString('id-ID', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  })}%`;
}

export function DashboardPage() {
  const [data, setData] = useState<DashboardMarginDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setData(await dashboardApi.margin());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal memuat dashboard.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  return (
    <div>
      <PageHeader
        title="Dashboard Margin"
        description="Ringkasan untung-rugi semua produk yang sudah punya harga jual atau resep aktif."
        actions={
          <button type="button" className="btn" disabled={loading} onClick={() => void load()}>
            {loading ? 'Memuat...' : '🔄 Muat ulang'}
          </button>
        }
      />

      {error && <div className="alert alert-error">{error}</div>}
      {loading && !data && <p className="muted">Memuat...</p>}

      {data && (
        <>
          <div className="cost-summary dashboard-summary">
            <div className="cost-card">
              <span className="cost-label">Jumlah produk dihitung</span>
              <span className="cost-value">{data.summary.productCount}</span>
            </div>
            <div className="cost-card">
              <span className="cost-label">Rata-rata margin</span>
              <span className="cost-value">{percent(data.summary.avgMarginPct)}</span>
            </div>
            <div className={`cost-card${data.summary.belowMinCount > 0 ? ' cost-warn' : ''}`}>
              <span className="cost-label">Margin di bawah minimum ({percent(data.minMarginPercent)})</span>
              <span className="cost-value">{data.summary.belowMinCount}</span>
            </div>
          </div>

          {data.items.length === 0 ? (
            <div className="getting-started">
              <strong>Belum ada yang bisa dihitung.</strong> Ikuti urutan ini supaya produk muncul di sini:
              <ol>
                <li>
                  Buat bahan baku &amp; produk di menu <a href="#/items">📦 Bahan &amp; Produk</a>.
                </li>
                <li>
                  Susun resepnya di menu <a href="#/boms">📋 Resep Produk (BOM)</a>, lalu klik <b>Aktifkan</b>.
                </li>
                <li>
                  Tetapkan harga jualnya di menu <a href="#/pricing">🏷️ Tetapkan Harga Jual</a>, lalu simpan &amp;
                  aktifkan.
                </li>
              </ol>
              Setelah itu, margin untung-rugi produk tersebut akan tampil otomatis di halaman ini.
            </div>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Produk</th>
                    <th>Tipe</th>
                    <th className="num">Biaya Produksi</th>
                    <th className="num">Harga Jual (ex-factory)</th>
                    <th className="num">Untung per Unit</th>
                    <th className="num">Margin</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((it) => (
                    <tr key={it.productItemId}>
                      <td>
                        {it.productCode} - {it.productName}
                        {it.hasActivePriceList && (
                          <span className="badge badge-active tree-tag">Harga aktif</span>
                        )}
                      </td>
                      <td>{itemTypeLabel(it.itemType)}</td>
                      <td className="num">{rupiah(it.standardCost)}</td>
                      <td className="num">{rupiah(it.exFactoryPrice)}</td>
                      <td className="num">{rupiah(it.contributionMargin)}</td>
                      <td className={`num ${it.belowMinMargin ? 'margin-bad' : 'margin-good'}`}>
                        <strong>{percent(it.marginPct)}</strong>
                      </td>
                      <td>
                        {it.belowMinMargin ? (
                          <span className="badge badge-obsolete">di bawah minimum</span>
                        ) : (
                          <span className="badge badge-active">sehat</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}
    </div>
  );
}
