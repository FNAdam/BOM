import { useEffect, useState } from 'react';
import { productionApi } from '../api/production';
import { bomsApi } from '../api/boms';
import type {
  BomListItemDto,
  CogsReportDto,
  ProductionOrderDto,
  ProductionOrderListDto,
  ProductionStatus,
} from '../api/types';
import { Modal } from '../components/Modal';

function rupiah(n: number): string {
  return n.toLocaleString('id-ID', {
    style: 'currency',
    currency: 'IDR',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

function qty(n: number): string {
  return n.toLocaleString('id-ID', { maximumFractionDigits: 6 });
}

const STATUS_BADGE: Record<ProductionStatus, string> = {
  Open: 'badge-draft',
  Closed: 'badge-active',
  Cancelled: 'badge-obsolete',
};

function StatusBadge({ status }: { status: ProductionStatus }) {
  return <span className={`badge ${STATUS_BADGE[status]}`}>{status}</span>;
}

/** Kelas warna variance: positif = tidak menguntungkan (merah), negatif = menguntungkan (hijau). */
function varianceClass(n: number): string {
  if (n > 0) return 'variance-pos';
  if (n < 0) return 'variance-neg';
  return '';
}

export function ProductionPage() {
  const [orders, setOrders] = useState<ProductionOrderListDto[]>([]);
  const [statusFilter, setStatusFilter] = useState<ProductionStatus | ''>('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [creating, setCreating] = useState(false);
  const [detailId, setDetailId] = useState<number | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setOrders(await productionApi.list(statusFilter ? { status: statusFilter } : undefined));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal memuat data.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [statusFilter]);

  return (
    <div>
      <div className="page-header">
        <h1>Produksi &amp; COGS</h1>
        <button type="button" className="btn btn-primary" onClick={() => setCreating(true)}>
          + Buat order
        </button>
      </div>

      <div className="filters">
        <label className="filter">
          Status
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as ProductionStatus | '')}
          >
            <option value="">Semua</option>
            <option value="Open">Open</option>
            <option value="Closed">Closed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </label>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      {loading ? (
        <p className="muted">Memuat...</p>
      ) : orders.length === 0 ? (
        <p className="muted">Belum ada perintah produksi.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Batch</th>
                <th>Produk</th>
                <th>Status</th>
                <th className="num">Planned</th>
                <th className="num">Actual</th>
                <th>Satuan</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {orders.map((o) => (
                <tr key={o.id}>
                  <td>{o.batchNo}</td>
                  <td>
                    {o.productCode} - {o.productName}
                  </td>
                  <td>
                    <StatusBadge status={o.status} />
                  </td>
                  <td className="num">{qty(o.plannedQty)}</td>
                  <td className="num">{o.actualQty != null ? qty(o.actualQty) : '-'}</td>
                  <td>{o.outputUomCode}</td>
                  <td className="actions">
                    <button type="button" className="btn btn-sm" onClick={() => setDetailId(o.id)}>
                      Detail
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {creating && (
        <CreateOrderModal
          onClose={() => setCreating(false)}
          onCreated={(id) => {
            setCreating(false);
            void load();
            setDetailId(id);
          }}
        />
      )}

      {detailId != null && (
        <OrderDetailModal
          orderId={detailId}
          onClose={() => setDetailId(null)}
          onChanged={() => void load()}
        />
      )}
    </div>
  );
}

// --------------------------------------------------------------- Create modal

function CreateOrderModal({
  onClose,
  onCreated,
}: {
  onClose: () => void;
  onCreated: (id: number) => void;
}) {
  const [boms, setBoms] = useState<BomListItemDto[]>([]);
  const [bomId, setBomId] = useState<number | ''>('');
  const [plannedQty, setPlannedQty] = useState('');
  const [batchNo, setBatchNo] = useState('');
  const [startDate, setStartDate] = useState('');
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      try {
        setBoms(await bomsApi.list({ status: 'Active' }));
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Gagal memuat BOM Active.');
      }
    })();
  }, []);

  const selectedBom = boms.find((b) => b.id === bomId);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (bomId === '') {
      setError('Pilih BOM Active terlebih dahulu.');
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const created = await productionApi.create({
        bomId: Number(bomId),
        plannedQty: Number(plannedQty),
        batchNo: batchNo.trim(),
        startDate: startDate === '' ? null : startDate,
        endDate: null,
        notes: notes.trim() === '' ? null : notes.trim(),
      });
      onCreated(created.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal membuat order.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal title="Buat perintah produksi" onClose={onClose}>
      <form onSubmit={handleSubmit}>
        {error && <div className="alert alert-error">{error}</div>}

        <label>
          BOM (Active)
          <select value={bomId} onChange={(e) => setBomId(e.target.value ? Number(e.target.value) : '')} required>
            <option value="">-- pilih BOM --</option>
            {boms.map((b) => (
              <option key={b.id} value={b.id}>
                {b.productCode} - {b.productName} (v{b.version}, out {qty(b.outputQuantity)} {b.outputUomCode})
              </option>
            ))}
          </select>
        </label>

        <label>
          Planned Qty {selectedBom ? `(${selectedBom.outputUomCode})` : ''}
          <input
            type="number"
            step="0.000001"
            min="0.000001"
            required
            value={plannedQty}
            onChange={(e) => setPlannedQty(e.target.value)}
          />
        </label>

        <label>
          Batch No
          <input
            type="text"
            maxLength={40}
            required
            value={batchNo}
            onChange={(e) => setBatchNo(e.target.value)}
          />
        </label>

        <label>
          Tanggal mulai (opsional)
          <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
        </label>

        <label>
          Catatan (opsional)
          <input type="text" maxLength={500} value={notes} onChange={(e) => setNotes(e.target.value)} />
        </label>

        <div className="form-actions">
          <button type="button" className="btn" onClick={onClose}>
            Batal
          </button>
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Menyimpan...' : 'Buat order'}
          </button>
        </div>
      </form>
    </Modal>
  );
}

// --------------------------------------------------------------- Detail modal

function OrderDetailModal({
  orderId,
  onClose,
  onChanged,
}: {
  orderId: number;
  onClose: () => void;
  onChanged: () => void;
}) {
  const [order, setOrder] = useState<ProductionOrderDto | null>(null);
  const [actuals, setActuals] = useState<Record<number, string>>({});
  const [closeQty, setCloseQty] = useState('');
  const [cogs, setCogs] = useState<CogsReportDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setError(null);
    try {
      const o = await productionApi.get(orderId);
      setOrder(o);
      setActuals(Object.fromEntries(o.consumptions.map((c) => [c.componentItemId, String(c.actualQty)])));
      setCloseQty((prev) => (prev === '' ? String(o.actualQty ?? o.plannedQty) : prev));
      if (o.status === 'Closed') {
        setCogs(await productionApi.cogs(o.id));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal memuat order.');
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orderId]);

  async function saveConsumption() {
    if (!order) return;
    setBusy(true);
    setError(null);
    try {
      const lines = order.consumptions.map((c) => ({
        componentItemId: c.componentItemId,
        actualQty: Number(actuals[c.componentItemId] ?? c.actualQty),
      }));
      await productionApi.recordConsumption(order.id, { lines });
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menyimpan konsumsi.');
    } finally {
      setBusy(false);
    }
  }

  async function closeOrder() {
    if (!order) return;
    setBusy(true);
    setError(null);
    try {
      const report = await productionApi.close(order.id, { actualQty: Number(closeQty) });
      setCogs(report);
      await load();
      onChanged();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menutup order.');
    } finally {
      setBusy(false);
    }
  }

  const isOpen = order?.status === 'Open';

  return (
    <Modal
      title={order ? `Order ${order.batchNo} - ${order.productCode}` : 'Detail order'}
      onClose={onClose}
    >
      {error && <div className="alert alert-error">{error}</div>}
      {!order ? (
        <p className="muted">Memuat...</p>
      ) : (
        <>
          <div className="detail-meta">
            <span>
              Produk: <strong>{order.productName}</strong>
            </span>
            <span>
              Status: <StatusBadge status={order.status} />
            </span>
            <span>
              Planned: <strong>{qty(order.plannedQty)}</strong> {order.outputUomCode}
            </span>
            {order.actualQty != null && (
              <span>
                Actual: <strong>{qty(order.actualQty)}</strong> {order.outputUomCode}
              </span>
            )}
          </div>

          <div className="section-header">
            <h2>Konsumsi komponen</h2>
            {isOpen && (
              <button type="button" className="btn btn-sm" disabled={busy} onClick={saveConsumption}>
                Simpan konsumsi
              </button>
            )}
          </div>

          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Komponen</th>
                  <th className="num">Planned</th>
                  <th className="num">Actual</th>
                  <th>Satuan</th>
                  {!isOpen && <th className="num">Unit Cost</th>}
                  {!isOpen && <th className="num">Actual Cost</th>}
                </tr>
              </thead>
              <tbody>
                {order.consumptions.map((c) => (
                  <tr key={c.componentItemId}>
                    <td>
                      {c.componentCode} - {c.componentName}
                    </td>
                    <td className="num">{qty(c.plannedQty)}</td>
                    <td className="num">
                      {isOpen ? (
                        <input
                          type="number"
                          step="0.000001"
                          min="0"
                          value={actuals[c.componentItemId] ?? ''}
                          onChange={(e) =>
                            setActuals((prev) => ({ ...prev, [c.componentItemId]: e.target.value }))
                          }
                        />
                      ) : (
                        qty(c.actualQty)
                      )}
                    </td>
                    <td>{c.uomCode}</td>
                    {!isOpen && <td className="num">{rupiah(c.actualUnitCost)}</td>}
                    {!isOpen && <td className="num">{rupiah(c.actualCost)}</td>}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {isOpen && (
            <div className="close-box">
              <div className="section-header">
                <h2>Tutup order</h2>
              </div>
              <div className="explode-controls">
                <label>
                  Actual output ({order.outputUomCode})
                  <input
                    type="number"
                    step="0.000001"
                    min="0.000001"
                    value={closeQty}
                    onChange={(e) => setCloseQty(e.target.value)}
                  />
                </label>
                <button type="button" className="btn btn-primary" disabled={busy} onClick={closeOrder}>
                  {busy ? 'Memproses...' : 'Tutup order & hitung COGS'}
                </button>
              </div>
            </div>
          )}

          {cogs && <CogsReport report={cogs} />}
        </>
      )}
    </Modal>
  );
}

// -------------------------------------------------------------- COGS report

function CogsReport({ report }: { report: CogsReportDto }) {
  return (
    <>
      <div className="section-header">
        <h2>Laporan COGS</h2>
      </div>

      <div className="cost-summary">
        <div className="cost-card">
          <span className="cost-label">Material</span>
          <span className="cost-value">{rupiah(report.materialCost)}</span>
        </div>
        <div className="cost-card">
          <span className="cost-label">Konversi</span>
          <span className="cost-value">{rupiah(report.conversionCost)}</span>
        </div>
        <div className="cost-card">
          <span className="cost-label">COGS total</span>
          <span className="cost-value">{rupiah(report.cogsTotal)}</span>
        </div>
        <div className="cost-card cost-total">
          <span className="cost-label">COGS / unit</span>
          <span className="cost-value">{rupiah(report.cogsUnit)}</span>
        </div>
      </div>

      <div className="cost-summary">
        <div className="cost-card">
          <span className="cost-label">Standar total</span>
          <span className="cost-value">{rupiah(report.standardTotal)}</span>
        </div>
        <div className="cost-card">
          <span className="cost-label">Material variance</span>
          <span className={`cost-value ${varianceClass(report.materialVariance)}`}>
            {rupiah(report.materialVariance)}
          </span>
        </div>
        <div className="cost-card">
          <span className="cost-label">Yield variance</span>
          <span className={`cost-value ${varianceClass(report.yieldVariance)}`}>
            {rupiah(report.yieldVariance)}
          </span>
        </div>
        <div className="cost-card">
          <span className="cost-label">Total variance</span>
          <span className={`cost-value ${varianceClass(report.totalVariance)}`}>
            {rupiah(report.totalVariance)}
          </span>
        </div>
      </div>

      <p className="muted variance-hint">
        Variance positif (merah) = tidak menguntungkan (biaya aktual di atas standar);
        negatif (hijau) = menguntungkan.
      </p>

      <div className="section-header">
        <h2>Variance per komponen</h2>
      </div>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Komponen</th>
              <th className="num">Actual Qty</th>
              <th className="num">Std Qty</th>
              <th className="num">Actual/unit</th>
              <th className="num">Std/unit</th>
              <th className="num">Price var.</th>
              <th className="num">Usage var.</th>
            </tr>
          </thead>
          <tbody>
            {report.lines.map((l) => (
              <tr key={l.componentId}>
                <td>
                  {l.code} - {l.name}
                </td>
                <td className="num">
                  {qty(l.actualQty)} {l.uomCode}
                </td>
                <td className="num">{qty(l.stdQtyAllowed)}</td>
                <td className="num">{rupiah(l.actualUnitCost)}</td>
                <td className="num">{rupiah(l.stdUnitCost)}</td>
                <td className={`num ${varianceClass(l.priceVariance)}`}>{rupiah(l.priceVariance)}</td>
                <td className={`num ${varianceClass(l.usageVariance)}`}>{rupiah(l.usageVariance)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
