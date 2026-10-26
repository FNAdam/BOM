import { useEffect, useState } from 'react';
import { inventoryApi } from '../api/inventory';
import { stockMovementsApi } from '../api/stockMovements';
import type { InventoryBalanceDto, StockMovementDto, StockMovementType } from '../api/types';
import { Modal } from '../components/Modal';

function num(n: number): string {
  return n.toLocaleString('id-ID', { maximumFractionDigits: 6 });
}

function rupiah(n: number): string {
  return n.toLocaleString('id-ID', {
    style: 'currency',
    currency: 'IDR',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

const IN_TYPES = new Set<StockMovementType>(['Receipt', 'ProductionReceipt']);

/** Tanda arah: hijau untuk masuk, merah untuk keluar. */
function MovementType({ type }: { type: StockMovementType }) {
  const dir = IN_TYPES.has(type) ? 'in' : type === 'Adjustment' ? 'adj' : 'out';
  const sign = dir === 'in' ? '+' : dir === 'out' ? '-' : '~';
  return (
    <span className={`move-type move-${dir}`}>
      {sign} {type}
    </span>
  );
}

export function InventoryPage() {
  const [rows, setRows] = useState<InventoryBalanceDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editing, setEditing] = useState<InventoryBalanceDto | null>(null);
  const [qtyOnHand, setQtyOnHand] = useState('0');
  const [averageCost, setAverageCost] = useState('');
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [movements, setMovements] = useState<StockMovementDto[]>([]);
  const [moveFilter, setMoveFilter] = useState<number | ''>('');
  const [movesLoading, setMovesLoading] = useState(true);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setRows(await inventoryApi.list());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal memuat data.');
    } finally {
      setLoading(false);
    }
  }

  async function loadMovements() {
    setMovesLoading(true);
    try {
      setMovements(await stockMovementsApi.list(moveFilter === '' ? undefined : moveFilter));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal memuat riwayat stok.');
    } finally {
      setMovesLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  useEffect(() => {
    void loadMovements();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [moveFilter]);

  function openEdit(row: InventoryBalanceDto) {
    setEditing(row);
    setQtyOnHand(String(row.qtyOnHand));
    setAverageCost(row.averageCost ? String(row.averageCost) : '');
    setFormError(null);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!editing) return;
    setSaving(true);
    setFormError(null);
    try {
      const updated = await inventoryApi.upsert(editing.itemId, {
        qtyOnHand: Number(qtyOnHand),
        averageCost: averageCost === '' ? null : Number(averageCost),
      });
      setRows((prev) => prev.map((r) => (r.itemId === updated.itemId ? updated : r)));
      setEditing(null);
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Gagal menyimpan.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1>Stok (On-Hand)</h1>
      </div>

      <div className="alert alert-info">
        On-hand &amp; biaya rata-rata bergerak kini hidup dari buku besar (StockMovement): produksi
        menutup order akan memposting pengeluaran komponen dan penerimaan produk jadi otomatis.
        Set on-hand manual di sini hanya untuk saldo awal / koreksi.
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {loading ? (
        <p className="muted">Memuat...</p>
      ) : rows.length === 0 ? (
        <p className="muted">Belum ada item.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Code</th>
                <th>Name</th>
                <th>Base UOM</th>
                <th className="num">Qty On-Hand</th>
                <th className="num">Avg Cost</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.itemId}>
                  <td>{row.itemCode}</td>
                  <td>{row.itemName}</td>
                  <td>{row.uomCode}</td>
                  <td className="num">{num(row.qtyOnHand)}</td>
                  <td className="num">{row.averageCost ? rupiah(row.averageCost) : '-'}</td>
                  <td className="actions">
                    <button type="button" className="btn btn-sm" onClick={() => openEdit(row)}>
                      Set saldo awal
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="section-header">
        <h2>Riwayat pergerakan stok</h2>
        <label className="filter">
          Item
          <select
            value={moveFilter}
            onChange={(e) => setMoveFilter(e.target.value ? Number(e.target.value) : '')}
          >
            <option value="">Semua item</option>
            {rows.map((r) => (
              <option key={r.itemId} value={r.itemId}>
                {r.itemCode} - {r.itemName}
              </option>
            ))}
          </select>
        </label>
      </div>

      {movesLoading ? (
        <p className="muted">Memuat...</p>
      ) : movements.length === 0 ? (
        <p className="muted">Belum ada pergerakan stok.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tanggal</th>
                <th>Item</th>
                <th>Tipe</th>
                <th className="num">Qty</th>
                <th className="num">Unit Cost</th>
                <th>Ref</th>
                <th>Catatan</th>
              </tr>
            </thead>
            <tbody>
              {movements.map((m) => (
                <tr key={m.id}>
                  <td>{m.movementDate}</td>
                  <td>
                    {m.itemCode} - {m.itemName}
                  </td>
                  <td>
                    <MovementType type={m.type} />
                  </td>
                  <td className="num">{num(m.qty)}</td>
                  <td className="num">{rupiah(m.unitCost)}</td>
                  <td>{m.refType ? `${m.refType} #${m.refId ?? '-'}` : '-'}</td>
                  <td>{m.notes ?? '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {editing && (
        <Modal title={`Set saldo awal: ${editing.itemCode}`} onClose={() => setEditing(null)}>
          <form onSubmit={handleSubmit}>
            {formError && <div className="alert alert-error">{formError}</div>}

            <label>
              Qty On-Hand ({editing.uomCode})
              <input
                type="number"
                step="0.000001"
                min="0"
                required
                value={qtyOnHand}
                onChange={(e) => setQtyOnHand(e.target.value)}
              />
            </label>

            <label>
              Average Cost (opsional)
              <input
                type="number"
                step="0.0001"
                min="0"
                value={averageCost}
                onChange={(e) => setAverageCost(e.target.value)}
              />
            </label>

            <div className="form-actions">
              <button type="button" className="btn" onClick={() => setEditing(null)}>
                Batal
              </button>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Menyimpan...' : 'Simpan'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
