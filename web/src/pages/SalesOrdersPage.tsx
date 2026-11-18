import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { salesOrdersApi } from '../api/salesOrders';
import { itemsApi } from '../api/items';
import { salesChannelsApi } from '../api/salesChannels';
import { packLevelsApi } from '../api/packLevels';
import { pricingSettingsApi } from '../api/pricingSettings';
import type {
  ItemDto,
  PackLevelDto,
  SalesChannelDto,
  SalesOrderListDto,
  SalesOrderStatus,
  UpsertSalesOrderDto,
  UpsertSalesOrderLineDto,
} from '../api/types';
import { Modal } from '../components/Modal';
import { PageHeader } from '../components/PageHeader';
import { salesOrderStatusLabel } from '../labels';
import { decToPctStr, pctStrToDec } from '../utils/percent';

function rupiah(n: number): string {
  return n.toLocaleString('id-ID', { maximumFractionDigits: 0 });
}

const PRODUCT_TYPES = new Set(['SemiFinished', 'FinishedGood']);

const STATUS_BADGE: Record<SalesOrderStatus, string> = {
  Draft: 'badge-draft',
  Completed: 'badge-active',
  Cancelled: 'badge-obsolete',
};

function StatusBadge({ status }: { status: SalesOrderStatus }) {
  return <span className={`badge ${STATUS_BADGE[status]}`}>{salesOrderStatusLabel(status)}</span>;
}

const emptyLine = (): UpsertSalesOrderLineDto => ({ productItemId: 0, packLevelId: null, qty: 1, unitPrice: 0 });

function todayStr(): string {
  const d = new Date();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${mm}-${dd}`;
}

export function SalesOrdersPage() {
  const navigate = useNavigate();

  const [orders, setOrders] = useState<SalesOrderListDto[]>([]);
  const [products, setProducts] = useState<ItemDto[]>([]);
  const [channels, setChannels] = useState<SalesChannelDto[]>([]);
  const [packLevelsByProduct, setPackLevelsByProduct] = useState<Record<number, PackLevelDto[]>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [statusFilter, setStatusFilter] = useState<SalesOrderStatus | ''>('');

  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editingStatus, setEditingStatus] = useState<SalesOrderStatus>('Draft');
  const [orderNo, setOrderNo] = useState('');
  const [salesChannelId, setSalesChannelId] = useState<number | ''>('');
  const [customerName, setCustomerName] = useState('');
  const [orderDate, setOrderDate] = useState(todayStr());
  const [defaultTaxPct, setDefaultTaxPct] = useState('0');
  const [taxPct, setTaxPct] = useState('0');
  const [notes, setNotes] = useState('');
  const [lines, setLines] = useState<UpsertSalesOrderLineDto[]>([emptyLine()]);
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [ordersData, itemsData, channelsData, settings] = await Promise.all([
        salesOrdersApi.list(statusFilter ? { status: statusFilter } : undefined),
        itemsApi.list(),
        salesChannelsApi.list(),
        pricingSettingsApi.get(),
      ]);
      setOrders(ordersData);
      setProducts(itemsData.filter((i) => PRODUCT_TYPES.has(i.type)));
      setChannels(channelsData);
      setDefaultTaxPct(decToPctStr(settings.defaultTaxPercent));
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

  async function ensurePackLevels(productId: number) {
    if (productId === 0 || packLevelsByProduct[productId]) return;
    try {
      const levels = await packLevelsApi.list(productId);
      setPackLevelsByProduct((prev) => ({ ...prev, [productId]: levels }));
    } catch {
      // diamkan - produk mungkin belum punya kemasan, baris tetap bisa dipakai tanpa kemasan
    }
  }

  function resetForm() {
    setEditingId(null);
    setEditingStatus('Draft');
    setOrderNo('');
    setSalesChannelId('');
    setCustomerName('');
    setOrderDate(todayStr());
    setTaxPct(defaultTaxPct);
    setNotes('');
    setLines([emptyLine()]);
    setFormError(null);
  }

  function openCreate() {
    resetForm();
    setShowForm(true);
  }

  async function openEdit(row: SalesOrderListDto) {
    const so = await salesOrdersApi.get(row.id);
    setEditingId(so.id);
    setEditingStatus(so.status);
    setOrderNo(so.orderNo);
    setSalesChannelId(so.salesChannelId ?? '');
    setCustomerName(so.customerName ?? '');
    setOrderDate(so.orderDate);
    setTaxPct(decToPctStr(so.taxPercent));
    setNotes(so.notes ?? '');
    setLines(so.lines.map((l) => ({ productItemId: l.productItemId, packLevelId: l.packLevelId, qty: l.qty, unitPrice: l.unitPrice })));
    for (const l of so.lines) void ensurePackLevels(l.productItemId);
    setFormError(null);
    setShowForm(true);
  }

  function updateLine(index: number, patch: Partial<UpsertSalesOrderLineDto>) {
    setLines((prev) => prev.map((l, i) => (i === index ? { ...l, ...patch } : l)));
    if (patch.productItemId) void ensurePackLevels(patch.productItemId);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setFormError(null);
    const dto: UpsertSalesOrderDto = {
      orderNo: orderNo.trim(),
      salesChannelId: salesChannelId === '' ? null : salesChannelId,
      customerName: customerName.trim() ? customerName.trim() : null,
      orderDate,
      taxPercent: pctStrToDec(taxPct),
      notes: notes.trim() ? notes.trim() : null,
      lines,
    };
    try {
      if (editingId === null) await salesOrdersApi.create(dto);
      else await salesOrdersApi.update(editingId, dto);
      setShowForm(false);
      await load();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Gagal menyimpan.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(row: SalesOrderListDto) {
    if (!window.confirm(`Hapus pesanan "${row.orderNo}"?`)) return;
    setBusy(true);
    setError(null);
    try {
      await salesOrdersApi.remove(row.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menghapus.');
    } finally {
      setBusy(false);
    }
  }

  async function handleComplete(row: SalesOrderListDto) {
    if (!window.confirm(`Selesaikan pesanan "${row.orderNo}"? Stok akan berkurang dan baris tidak bisa diubah lagi setelah ini.`)) return;
    setBusy(true);
    setError(null);
    try {
      await salesOrdersApi.complete(row.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menyelesaikan pesanan.');
    } finally {
      setBusy(false);
    }
  }

  async function handleCancel(row: SalesOrderListDto) {
    if (!window.confirm(`Batalkan pesanan "${row.orderNo}"?`)) return;
    setBusy(true);
    setError(null);
    try {
      await salesOrdersApi.cancel(row.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal membatalkan.');
    } finally {
      setBusy(false);
    }
  }

  const canCreate = products.length > 0;
  const readOnly = editingId !== null && editingStatus !== 'Draft';

  return (
    <div>
      <PageHeader
        title="Pesanan Penjualan"
        description="Catat penjualan produk ke customer. Begitu diselesaikan, stok otomatis berkurang dan struk bisa dicetak."
        actions={
          <button type="button" className="btn btn-primary" onClick={openCreate} disabled={!canCreate}>
            + Pesanan Baru
          </button>
        }
      />

      {!canCreate && !loading && (
        <div className="alert alert-info">Butuh minimal 1 produk (Setengah Jadi/Produk Jadi) untuk membuat pesanan.</div>
      )}

      <div className="filters">
        <label className="filter">
          Status
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value as SalesOrderStatus | '')}>
            <option value="">Semua status</option>
            <option value="Draft">{salesOrderStatusLabel('Draft')}</option>
            <option value="Completed">{salesOrderStatusLabel('Completed')}</option>
            <option value="Cancelled">{salesOrderStatusLabel('Cancelled')}</option>
          </select>
        </label>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      {loading ? (
        <p className="muted">Memuat...</p>
      ) : orders.length === 0 ? (
        <p className="muted">Belum ada pesanan penjualan.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>No. Pesanan</th>
                <th>Customer</th>
                <th>Kanal</th>
                <th>Status</th>
                <th>Tanggal</th>
                <th className="num">Total</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {orders.map((o) => (
                <tr key={o.id}>
                  <td>{o.orderNo}</td>
                  <td>{o.customerName ?? '-'}</td>
                  <td>{o.salesChannelName ?? 'Ex-factory'}</td>
                  <td><StatusBadge status={o.status} /></td>
                  <td>{o.orderDate}</td>
                  <td className="num">{rupiah(o.grandTotal)}</td>
                  <td className="actions">
                    <button type="button" className="btn btn-sm" onClick={() => void openEdit(o)}>
                      {o.status === 'Draft' ? 'Edit' : 'Lihat'}
                    </button>
                    {o.status === 'Draft' && (
                      <button type="button" className="btn btn-sm btn-primary" disabled={busy} onClick={() => handleComplete(o)}>
                        Selesaikan
                      </button>
                    )}
                    {o.status === 'Completed' && (
                      <button type="button" className="btn btn-sm btn-primary" onClick={() => navigate(`/sales-orders/${o.id}/receipt`)}>
                        Cetak Struk
                      </button>
                    )}
                    {o.status === 'Draft' && (
                      <button type="button" className="btn btn-sm btn-danger" disabled={busy} onClick={() => handleCancel(o)}>
                        Batalkan
                      </button>
                    )}
                    {o.status === 'Draft' && (
                      <button type="button" className="btn btn-sm btn-danger" disabled={busy} onClick={() => handleDelete(o)}>
                        Hapus
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showForm && (
        <Modal
          title={editingId === null ? 'Pesanan Baru' : `Pesanan ${orderNo} (${salesOrderStatusLabel(editingStatus)})`}
          onClose={() => setShowForm(false)}
        >
          <form onSubmit={handleSubmit}>
            {formError && <div className="alert alert-error">{formError}</div>}
            {readOnly && (
              <div className="alert alert-info">
                Pesanan ini sudah {salesOrderStatusLabel(editingStatus).toLowerCase()}, tidak bisa diubah lagi.
              </div>
            )}

            <fieldset disabled={readOnly} className="editor-fieldset">
              <div className="form-grid">
                <label>
                  No. Pesanan / Struk
                  <input value={orderNo} maxLength={40} required placeholder="mis. INV-2026-11-001"
                    onChange={(e) => setOrderNo(e.target.value)} />
                </label>

                <label>
                  Kanal Penjualan (opsional)
                  <select value={salesChannelId} onChange={(e) => setSalesChannelId(e.target.value ? Number(e.target.value) : '')}>
                    <option value="">-- Ex-factory / langsung --</option>
                    {channels.map((c) => (
                      <option key={c.id} value={c.id}>{c.name}</option>
                    ))}
                  </select>
                </label>

                <label>
                  Nama Customer (opsional)
                  <input value={customerName} maxLength={150} placeholder="mis. Toko Berkah"
                    onChange={(e) => setCustomerName(e.target.value)} />
                </label>

                <label>
                  Tanggal
                  <input type="date" value={orderDate} required onChange={(e) => setOrderDate(e.target.value)} />
                </label>

                <label>
                  PPN (%) - ditambahkan ke subtotal
                  <input type="number" step="0.01" min="0" value={taxPct} onChange={(e) => setTaxPct(e.target.value)} />
                  <span className="field-hint">
                    Isi 0 kalau harga di bawah sudah termasuk PPN, atau transaksi ini tidak kena PPN - jangan sampai
                    dobel pajak.
                  </span>
                </label>
              </div>

              <label>
                Catatan (opsional)
                <input value={notes} maxLength={500} onChange={(e) => setNotes(e.target.value)} />
              </label>

              <div className="section-header">
                <h2>Produk yang Dijual</h2>
                {!readOnly && (
                  <button type="button" className="btn btn-sm" onClick={() => setLines((prev) => [...prev, emptyLine()])}>
                    + Baris
                  </button>
                )}
              </div>

              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Produk</th>
                      <th>Kemasan</th>
                      <th className="num">Qty</th>
                      <th className="num">Harga/satuan (sebelum PPN)</th>
                      <th className="num">Subtotal</th>
                      {!readOnly && <th></th>}
                    </tr>
                  </thead>
                  <tbody>
                    {lines.map((l, index) => {
                      const availablePacks = packLevelsByProduct[l.productItemId] ?? [];
                      return (
                        <tr key={index}>
                          <td>
                            <select value={l.productItemId} required
                              onChange={(e) => updateLine(index, { productItemId: Number(e.target.value), packLevelId: null })}>
                              <option value={0} disabled>-- produk --</option>
                              {products.map((p) => (
                                <option key={p.id} value={p.id}>{p.code} - {p.name}</option>
                              ))}
                            </select>
                          </td>
                          <td>
                            <select value={l.packLevelId ?? ''}
                              onChange={(e) => updateLine(index, { packLevelId: e.target.value ? Number(e.target.value) : null })}>
                              <option value="">-- satuan dasar --</option>
                              {availablePacks.map((pl) => (
                                <option key={pl.id} value={pl.id}>{pl.name}</option>
                              ))}
                            </select>
                          </td>
                          <td>
                            <input type="number" step="0.000001" min="0.000001" value={l.qty}
                              onChange={(e) => updateLine(index, { qty: Number(e.target.value) })} />
                          </td>
                          <td>
                            <input type="number" step="0.0001" min="0" value={l.unitPrice}
                              onChange={(e) => updateLine(index, { unitPrice: Number(e.target.value) })} />
                          </td>
                          <td className="num">{rupiah(l.qty * l.unitPrice)}</td>
                          {!readOnly && (
                            <td className="actions">
                              <button type="button" className="btn btn-sm btn-danger"
                                disabled={lines.length === 1}
                                onClick={() => setLines((prev) => prev.filter((_, i) => i !== index))}>
                                Hapus
                              </button>
                            </td>
                          )}
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </fieldset>

            <div className="form-actions">
              <button type="button" className="btn" onClick={() => setShowForm(false)}>
                {readOnly ? 'Tutup' : 'Batal'}
              </button>
              {!readOnly && (
                <button type="submit" className="btn btn-primary" disabled={saving}>
                  {saving ? 'Menyimpan...' : 'Simpan sebagai Draf'}
                </button>
              )}
              {editingStatus === 'Completed' && (
                <button type="button" className="btn btn-primary" onClick={() => navigate(`/sales-orders/${editingId}/receipt`)}>
                  Cetak Struk
                </button>
              )}
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
