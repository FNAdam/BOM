import { useEffect, useState } from 'react';
import { dashboardApi } from '../api/dashboard';
import type { DashboardMarginDto } from '../api/types';

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
      <div className="page-header">
        <h1>Dashboard Margin</h1>
        <button type="button" className="btn" disabled={loading} onClick={() => void load()}>
          {loading ? 'Memuat...' : 'Muat ulang'}
        </button>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {loading && !data && <p className="muted">Memuat...</p>}

      {data && (
        <>
          <div className="cost-summary dashboard-summary">
            <div className="cost-card">
              <span className="cost-label">Jumlah produk</span>
              <span className="cost-value">{data.summary.productCount}</span>
            </div>
            <div className="cost-card">
              <span className="cost-label">Rata-rata margin</span>
              <span className="cost-value">{percent(data.summary.avgMarginPct)}</span>
            </div>
            <div className={`cost-card${data.summary.belowMinCount > 0 ? ' cost-warn' : ''}`}>
              <span className="cost-label">Di bawah minimum ({percent(data.minMarginPercent)})</span>
              <span className="cost-value">{data.summary.belowMinCount}</span>
            </div>
          </div>

          {data.items.length === 0 ? (
            <p className="muted">
              Belum ada produk ber-harga (price list Active) atau ber-BOM Active untuk dihitung.
            </p>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Produk</th>
                    <th>Tipe</th>
                    <th className="num">Std Cost</th>
                    <th className="num">Harga ex-factory</th>
                    <th className="num">Kontribusi</th>
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
                          <span className="badge badge-active tree-tag">Price list</span>
                        )}
                      </td>
                      <td>{it.itemType}</td>
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
