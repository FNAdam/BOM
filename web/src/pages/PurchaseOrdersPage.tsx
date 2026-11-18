import { useEffect, useState } from 'react';
import { purchaseOrdersApi } from '../api/purchaseOrders';
import { itemsApi } from '../api/items';
import { suppliersApi } from '../api/suppliers';
import { uomsApi } from '../api/uoms';
import type {
  ItemDto,
  PurchaseOrderDto,
  PurchaseOrderListDto,
  PurchaseOrderStatus,
  SupplierDto,
  UomDto,
  UpsertPurchaseOrderDto,
  UpsertPurchaseOrderLineDto,
} from '../api/types';
import { Modal } from '../components/Modal';
import { PageHeader } from '../components/PageHeader';
import { purchaseOrderStatusLabel } from '../labels';

function rupiah(n: number): string {
  return n.toLocaleString('id-ID', { maximumFractionDigits: 0 });
}

function qty(n: number): string {
  return n.toLocaleString('id-ID', { maximumFractionDigits: 6 });
}

const STATUS_BADGE: Record<PurchaseOrderStatus, string> = {
  Draft: 'badge-draft',
  Sent: 'badge-pending',
  PartiallyReceived: 'badge-pending',
  Received: 'badge-active',
  Cancelled: 'badge-obsolete',
};

function StatusBadge({ status }: { status: PurchaseOrderStatus }) {
  return <span className={`badge ${STATUS_BADGE[status]}`}>{purchaseOrderStatusLabel(status)}</span>;
}

const emptyLine = (): UpsertPurchaseOrderLineDto => ({ itemId: 0, uomId: 0, qtyOrdered: 1, unitPrice: 0 });

// toISOString() pakai UTC - sebelum jam 07:00 WIB ini masih "kemarin" di UTC. Pakai komponen
// tanggal lokal supaya default tanggal PO benar sepanjang hari.
function todayStr(): string {
  const d = new Date();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${mm}-${dd}`;
}

export function PurchaseOrdersPage() {
  const [orders, setOrders] = useState<PurchaseOrderListDto[]>([]);
  const [items, setItems] = useState<ItemDto[]>([]);
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [uoms, setUoms] = useState<UomDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [statusFilter, setStatusFilter] = useState<PurchaseOrderStatus | ''>('');

  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editingStatus, setEditingStatus] = useState<PurchaseOrderStatus>('Draft');
  const [supplierId, setSupplierId] = useState<number>(0);
  const [orderNo, setOrderNo] = useState('');
  const [orderDate, setOrderDate] = useState(todayStr());
  const [notes, setNotes] = useState('');
  const [lines, setLines] = useState<UpsertPurchaseOrderLineDto[]>([emptyLine()]);
  const [editingQtyReceived, setEditingQtyReceived] = useState<number[]>([]);
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const [receiveFor, setReceiveFor] = useState<PurchaseOrderDto | null>(null);
  const [receiveQty, setReceiveQty] = useState<Record<number, string>>({});
  const [receiveError, setReceiveError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [ordersData, itemsData, suppliersData, uomsData] = await Promise.all([
        purchaseOrdersApi.list(statusFilter ? { status: statusFilter } : undefined),
        itemsApi.list(),
        suppliersApi.list(),
        uomsApi.list(),
      ]);
      setOrders(ordersData);
      setItems(itemsData);
      setSuppliers(suppliersData);
      setUoms(uomsData);
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

  function resetForm() {
    setEditingId(null);
    setEditingStatus('Draft');
    setSupplierId(suppliers[0]?.id ?? 0);
    setOrderNo('');
    setOrderDate(todayStr());
    setNotes('');
    setLines([emptyLine()]);
    setEditingQtyReceived([]);
    setFormError(null);
  }

  function openCreate() {
    resetForm();
    setShowForm(true);
  }

  async function openEdit(row: PurchaseOrderListDto) {
    const po = await purchaseOrdersApi.get(row.id);
    setEditingId(po.id);
    setEditingStatus(po.status);
    setSupplierId(po.supplierId);
    setOrderNo(po.orderNo);
    setOrderDate(po.orderDate);
    setNotes(po.notes ?? '');
    setLines(po.lines.map((l) => ({ itemId: l.itemId, uomId: l.uomId, qtyOrdered: l.qtyOrdered, unitPrice: l.unitPrice })));
    setEditingQtyReceived(po.lines.map((l) => l.qtyReceived));
    setFormError(null);
    setShowForm(true);
  }

  function updateLine(index: number, patch: Partial<UpsertPurchaseOrderLineDto>) {
    setLines((prev) => prev.map((l, i) => {
      if (i !== index) return l;
      const next = { ...l, ...patch };
      // Set default satuan ke satuan dasar item begitu item dipilih/diganti, supaya tidak
      // kejadian salah satuan (mis. pesan "gram" tapi satuannya ketinggalan "pcs").
      if (patch.itemId !== undefined && patch.itemId !== l.itemId) {
        const item = items.find((it) => it.id === patch.itemId);
        if (item) next.uomId = item.baseUomId;
      }
      return next;
    }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setFormError(null);
    const dto: UpsertPurchaseOrderDto = {
      supplierId,
      orderNo: orderNo.trim(),
      orderDate,
      notes: notes.trim() ? notes.trim() : null,
      lines,
    };
    try {
      if (editingId === null) await purchaseOrdersApi.create(dto);
      else await purchaseOrdersApi.update(editingId, dto);
      setShowForm(false);
      await load();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Gagal menyimpan.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(row: PurchaseOrderListDto) {
    if (!window.confirm(`Hapus PO "${row.orderNo}"?`)) return;
    setBusy(true);
    setError(null);
    try {
      await purchaseOrdersApi.remove(row.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menghapus.');
    } finally {
      setBusy(false);
    }
  }

  async function handleSend(row: PurchaseOrderListDto) {
    setBusy(true);
    setError(null);
    try {
      await purchaseOrdersApi.send(row.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal mengirim PO.');
    } finally {
      setBusy(false);
    }
  }

  async function handleCancel(row: PurchaseOrderListDto) {
    if (!window.confirm(`Batalkan PO "${row.orderNo}"?`)) return;
    setBusy(true);
    setError(null);
    try {
      await purchaseOrdersApi.cancel(row.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal membatalkan.');
    } finally {
      setBusy(false);
    }
  }

  async function openReceive(row: PurchaseOrderListDto) {
    const po = await purchaseOrdersApi.get(row.id);
    setReceiveFor(po);
    setReceiveQty({});
    setReceiveError(null);
  }

  async function submitReceive() {
    if (!receiveFor) return;
    const receiveLines = Object.entries(receiveQty)
      .map(([lineId, v]) => ({ lineId: Number(lineId), qtyReceiving: Number(v) }))
      .filter((l) => l.qtyReceiving > 0);

    if (receiveLines.length === 0) {
      setReceiveError('Isi minimal satu qty yang diterima.');
      return;
    }

    setBusy(true);
    setReceiveError(null);
    try {
      await purchaseOrdersApi.receive(receiveFor.id, { lines: receiveLines });
      setReceiveFor(null);
      await load();
    } catch (err) {
      setReceiveError(err instanceof Error ? err.message : 'Gagal mencatat penerimaan.');
    } finally {
      setBusy(false);
    }
  }

  const canCreate = items.length > 0 && suppliers.length > 0 && uoms.length > 0;
  const readOnly = editingId !== null && editingStatus !== 'Draft';

  return (
    <div>
      <PageHeader
        title="Pesanan Pembelian"
        description="Catat pesanan bahan baku/kemasan ke supplier. Begitu barang diterima, stok & biaya rata-rata otomatis ter-update."
        actions={
          <button type="button" className="btn btn-primary" onClick={openCreate} disabled={!canCreate}>
            + PO Baru
          </button>
        }
      />

      {!canCreate && !loading && (
        <div className="alert alert-info">Butuh minimal 1 bahan, 1 supplier, dan 1 satuan ukur untuk membuat PO.</div>
      )}

      <div className="filters">
        <label className="filter">
          Status
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value as PurchaseOrderStatus | '')}>
            <option value="">Semua status</option>
            <option value="Draft">{purchaseOrderStatusLabel('Draft')}</option>
            <option value="Sent">{purchaseOrderStatusLabel('Sent')}</option>
            <option value="PartiallyReceived">{purchaseOrderStatusLabel('PartiallyReceived')}</option>
            <option value="Received">{purchaseOrderStatusLabel('Received')}</option>
            <option value="Cancelled">{purchaseOrderStatusLabel('Cancelled')}</option>
          </select>
        </label>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      {loading ? (
        <p className="muted">Memuat...</p>
      ) : orders.length === 0 ? (
        <p className="muted">Belum ada pesanan pembelian.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>No. PO</th>
                <th>Supplier</th>
                <th>Status</th>
                <th>Tanggal</th>
                <th className="num">Jml Baris</th>
                <th className="num">Total Nilai</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {orders.map((o) => (
                <tr key={o.id}>
                  <td>{o.orderNo}</td>
                  <td>{o.supplierCode} - {o.supplierName}</td>
                  <td><StatusBadge status={o.status} /></td>
                  <td>{o.orderDate}</td>
                  <td className="num">{o.lineCount}</td>
                  <td className="num">{rupiah(o.totalValue)}</td>
                  <td className="actions">
                    {o.status === 'Draft' && (
                      <button type="button" className="btn btn-sm" onClick={() => void openEdit(o)}>
                        Edit
                      </button>
                    )}
                    {o.status !== 'Draft' && (
                      <button type="button" className="btn btn-sm" onClick={() => void openEdit(o)}>
                        Lihat
                      </button>
                    )}
                    {o.status === 'Draft' && (
                      <button type="button" className="btn btn-sm btn-primary" disabled={busy} onClick={() => handleSend(o)}>
                        Kirim
                      </button>
                    )}
                    {(o.status === 'Sent' || o.status === 'PartiallyReceived') && (
                      <button type="button" className="btn btn-sm btn-primary" onClick={() => void openReceive(o)}>
                        Terima Barang
                      </button>
                    )}
                    {(o.status === 'Draft' || o.status === 'Sent' || o.status === 'PartiallyReceived') && (
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
          title={editingId === null ? 'PO Baru' : `PO ${orderNo} (${purchaseOrderStatusLabel(editingStatus)})`}
          onClose={() => setShowForm(false)}
        >
          <form onSubmit={handleSubmit}>
            {formError && <div className="alert alert-error">{formError}</div>}
            {readOnly && (
              <div className="alert alert-info">
                PO ini sudah {purchaseOrderStatusLabel(editingStatus).toLowerCase()}, tidak bisa diubah lagi.
              </div>
            )}

            <fieldset disabled={readOnly} className="editor-fieldset">
              <div className="form-grid">
                <label>
                  Supplier
                  <select value={supplierId} required onChange={(e) => setSupplierId(Number(e.target.value))}>
                    <option value={0} disabled>-- pilih supplier --</option>
                    {suppliers.map((s) => (
                      <option key={s.id} value={s.id}>{s.code} - {s.name}</option>
                    ))}
                  </select>
                </label>

                <label>
                  No. PO
                  <input value={orderNo} maxLength={40} required placeholder="mis. PO-2026-11-001"
                    onChange={(e) => setOrderNo(e.target.value)} />
                </label>

                <label>
                  Tanggal
                  <input type="date" value={orderDate} required onChange={(e) => setOrderDate(e.target.value)} />
                </label>
              </div>

              <label>
                Catatan (opsional)
                <input value={notes} maxLength={500} onChange={(e) => setNotes(e.target.value)} />
              </label>

              <div className="section-header">
                <h2>Baris Pesanan</h2>
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
                      <th>Bahan</th>
                      <th>Satuan</th>
                      <th className="num">Qty Pesan</th>
                      <th className="num">Harga/satuan</th>
                      {readOnly && <th className="num">Sudah Diterima</th>}
                      {!readOnly && <th></th>}
                    </tr>
                  </thead>
                  <tbody>
                    {lines.map((l, index) => (
                      <tr key={index}>
                        <td>
                          <select value={l.itemId} required onChange={(e) => updateLine(index, { itemId: Number(e.target.value) })}>
                            <option value={0} disabled>-- bahan --</option>
                            {items.map((i) => (
                              <option key={i.id} value={i.id}>{i.code} - {i.name}</option>
                            ))}
                          </select>
                        </td>
                        <td>
                          <select value={l.uomId} required onChange={(e) => updateLine(index, { uomId: Number(e.target.value) })}>
                            <option value={0} disabled>-- satuan --</option>
                            {uoms.map((u) => (
                              <option key={u.id} value={u.id}>{u.code}</option>
                            ))}
                          </select>
                        </td>
                        <td>
                          <input type="number" step="0.000001" min="0.000001" value={l.qtyOrdered}
                            onChange={(e) => updateLine(index, { qtyOrdered: Number(e.target.value) })} />
                        </td>
                        <td>
                          <input type="number" step="0.0001" min="0" value={l.unitPrice}
                            onChange={(e) => updateLine(index, { unitPrice: Number(e.target.value) })} />
                        </td>
                        {readOnly && <td className="num">{qty(editingQtyReceived[index] ?? 0)}</td>}
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
                    ))}
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
            </div>
          </form>
        </Modal>
      )}

      {receiveFor && (
        <Modal title={`Terima Barang: ${receiveFor.orderNo}`} onClose={() => setReceiveFor(null)}>
          {receiveError && <div className="alert alert-error">{receiveError}</div>}
          <p className="muted" style={{ marginTop: 0 }}>
            Isi jumlah yang benar-benar datang sekarang per baris (boleh sebagian / tidak semua baris sekaligus).
          </p>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Bahan</th>
                  <th className="num">Dipesan</th>
                  <th className="num">Sudah Diterima</th>
                  <th className="num">Sisa</th>
                  <th className="num">Diterima Sekarang</th>
                </tr>
              </thead>
              <tbody>
                {receiveFor.lines.map((l) => {
                  const remaining = l.qtyOrdered - l.qtyReceived;
                  return (
                    <tr key={l.id}>
                      <td>{l.itemCode} - {l.itemName}</td>
                      <td className="num">{qty(l.qtyOrdered)} {l.uomCode}</td>
                      <td className="num">{qty(l.qtyReceived)}</td>
                      <td className="num">{qty(remaining)}</td>
                      <td className="num">
                        {remaining > 0 ? (
                          <input
                            type="number"
                            step="0.000001"
                            min="0"
                            max={remaining}
                            placeholder="0"
                            value={receiveQty[l.id] ?? ''}
                            onChange={(e) => setReceiveQty((prev) => ({ ...prev, [l.id]: e.target.value }))}
                          />
                        ) : (
                          <span className="muted">lunas</span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          <div className="form-actions">
            <button type="button" className="btn" onClick={() => setReceiveFor(null)}>Batal</button>
            <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void submitReceive()}>
              {busy ? 'Memproses...' : 'Catat Penerimaan'}
            </button>
          </div>
        </Modal>
      )}
    </div>
  );
}
