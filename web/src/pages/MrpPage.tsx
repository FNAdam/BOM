import { useEffect, useMemo, useState } from 'react';
import { itemsApi } from '../api/items';
import { bomsApi } from '../api/boms';
import { mrpApi } from '../api/mrp';
import type { ItemDto, MrpLineDto, MrpResultDto } from '../api/types';

const PRODUCT_TYPES = new Set(['SemiFinished', 'FinishedGood']);

interface DemandRow {
  productItemId: number | '';
  quantity: string;
}

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

export function MrpPage() {
  const [items, setItems] = useState<ItemDto[]>([]);
  const [productIds, setProductIds] = useState<Set<number>>(new Set());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [demands, setDemands] = useState<DemandRow[]>([{ productItemId: '', quantity: '' }]);
  const [result, setResult] = useState<MrpResultDto | null>(null);
  const [busy, setBusy] = useState(false);

  // Produk yang layak jadi demand: SemiFinished/FinishedGood dengan BOM Active.
  const products = useMemo(
    () => items.filter((i) => PRODUCT_TYPES.has(i.type) && productIds.has(i.id)),
    [items, productIds],
  );

  async function loadMasters() {
    setLoading(true);
    setError(null);
    try {
      const [itemsData, activeBoms] = await Promise.all([
        itemsApi.list(),
        bomsApi.list({ status: 'Active' }),
      ]);
      setItems(itemsData);
      setProductIds(new Set(activeBoms.map((b) => b.productItemId)));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal memuat data.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadMasters();
  }, []);

  function updateRow(index: number, patch: Partial<DemandRow>) {
    setDemands((prev) => prev.map((row, i) => (i === index ? { ...row, ...patch } : row)));
  }

  function addRow() {
    setDemands((prev) => [...prev, { productItemId: '', quantity: '' }]);
  }

  function removeRow(index: number) {
    setDemands((prev) => (prev.length === 1 ? prev : prev.filter((_, i) => i !== index)));
  }

  async function calculate() {
    const payload = demands
      .filter((r) => r.productItemId !== '' && Number(r.quantity) > 0)
      .map((r) => ({ productItemId: Number(r.productItemId), quantity: Number(r.quantity) }));

    if (payload.length === 0) {
      setError('Tambahkan minimal satu baris demand dengan produk dan qty > 0.');
      return;
    }

    setBusy(true);
    setError(null);
    setResult(null);
    try {
      setResult(await mrpApi.explode({ demands: payload }));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menghitung MRP.');
    } finally {
      setBusy(false);
    }
  }

  const buyLines = result?.lines.filter((l) => l.source === 'Buy') ?? [];
  const makeLines = result?.lines.filter((l) => l.source === 'Make') ?? [];

  return (
    <div>
      <div className="page-header">
        <h1>MRP / Procurement</h1>
        <button type="button" className="btn btn-primary" disabled={busy || loading} onClick={calculate}>
          {busy ? 'Menghitung...' : 'Hitung MRP'}
        </button>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {loading && <p className="muted">Memuat...</p>}

      {!loading && (
        <>
          <div className="section-header">
            <h2>Demand</h2>
            <button type="button" className="btn btn-sm" onClick={addRow}>
              + Tambah baris
            </button>
          </div>

          {products.length === 0 ? (
            <div className="alert alert-info">
              Belum ada produk ber-BOM Active. Buat dan aktifkan BOM lebih dulu di menu BOM.
            </div>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Produk (SemiFinished / FinishedGood)</th>
                    <th className="num">Qty</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {demands.map((row, i) => {
                    const selected = items.find((it) => it.id === row.productItemId);
                    return (
                      <tr key={i}>
                        <td>
                          <select
                            value={row.productItemId}
                            onChange={(e) =>
                              updateRow(i, { productItemId: e.target.value ? Number(e.target.value) : '' })
                            }
                          >
                            <option value="">-- pilih produk --</option>
                            {products.map((p) => (
                              <option key={p.id} value={p.id}>
                                {p.code} - {p.name}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td className="num">
                          <input
                            type="number"
                            step="0.000001"
                            min="0"
                            placeholder={selected ? selected.baseUomCode : 'qty'}
                            value={row.quantity}
                            onChange={(e) => updateRow(i, { quantity: e.target.value })}
                          />
                        </td>
                        <td className="actions">
                          <button
                            type="button"
                            className="btn btn-sm btn-danger"
                            disabled={demands.length === 1}
                            onClick={() => removeRow(i)}
                          >
                            Hapus
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}

      {result && (
        <>
          <div className="cost-summary" style={{ marginTop: 20 }}>
            <div className="cost-card">
              <span className="cost-label">Baris Make</span>
              <span className="cost-value">{makeLines.length}</span>
            </div>
            <div className="cost-card">
              <span className="cost-label">Baris Buy</span>
              <span className="cost-value">{buyLines.length}</span>
            </div>
            <div className="cost-card cost-total">
              <span className="cost-label">Total biaya pembelian</span>
              <span className="cost-value">{rupiah(result.totalPurchaseCost)}</span>
            </div>
          </div>

          <div className="section-header">
            <h2>Saran Pembelian (Buy)</h2>
          </div>
          {buyLines.length === 0 ? (
            <p className="muted">Tidak ada kebutuhan pembelian.</p>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Item</th>
                    <th>Supplier</th>
                    <th className="num">Gross</th>
                    <th className="num">On-Hand</th>
                    <th className="num">Net</th>
                    <th className="num">MOQ</th>
                    <th className="num">Saran Order</th>
                    <th className="num">Harga / satuan</th>
                    <th className="num">Lead (hari)</th>
                    <th className="num">Extended</th>
                  </tr>
                </thead>
                <tbody>
                  {buyLines.map((line) => (
                    <BuyRow key={line.itemId} line={line} />
                  ))}
                </tbody>
              </table>
            </div>
          )}

          <div className="section-header">
            <h2>Rencana Produksi (Make)</h2>
          </div>
          {makeLines.length === 0 ? (
            <p className="muted">Tidak ada rencana produksi.</p>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Item</th>
                    <th>Tipe</th>
                    <th className="num">Gross</th>
                    <th className="num">On-Hand</th>
                    <th className="num">Net (produksi)</th>
                    <th>Satuan</th>
                  </tr>
                </thead>
                <tbody>
                  {makeLines.map((line) => (
                    <tr key={line.itemId}>
                      <td>
                        {line.code} - {line.name}
                        <span className="badge badge-active tree-tag">BOM</span>
                      </td>
                      <td>{line.itemType}</td>
                      <td className="num">{qty(line.grossQty)}</td>
                      <td className="num">{qty(line.onHandQty)}</td>
                      <td className="num">
                        <strong>{qty(line.netQty)}</strong>
                      </td>
                      <td>{line.uomCode}</td>
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

function BuyRow({ line }: { line: MrpLineDto }) {
  return (
    <tr>
      <td>
        {line.code} - {line.name}
      </td>
      <td>{line.supplierName ?? <span className="muted">std cost</span>}</td>
      <td className="num">{qty(line.grossQty)}</td>
      <td className="num">{qty(line.onHandQty)}</td>
      <td className="num">{qty(line.netQty)}</td>
      <td className="num">{line.minOrderQty != null ? qty(line.minOrderQty) : '-'}</td>
      <td className="num">
        <strong>
          {line.suggestedOrderQty != null ? qty(line.suggestedOrderQty) : '-'} {line.uomCode}
        </strong>
      </td>
      <td className="num">{line.unitCost != null ? rupiah(line.unitCost) : '-'}</td>
      <td className="num">{line.leadTimeDays != null ? line.leadTimeDays : '-'}</td>
      <td className="num">
        <strong>{line.extendedCost != null ? rupiah(line.extendedCost) : '-'}</strong>
      </td>
    </tr>
  );
}
