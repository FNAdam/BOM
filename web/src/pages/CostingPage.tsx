import { useEffect, useMemo, useState } from 'react';
import { costingApi } from '../api/costing';
import { itemsApi } from '../api/items';
import { bomsApi } from '../api/boms';
import type {
  CostBreakdownDto,
  CostLineDto,
  CostSnapshotDto,
  ItemDto,
} from '../api/types';

const PRODUCT_TYPES = new Set(['SemiFinished', 'FinishedGood']);

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

/** Baris pohon biaya, menjorok berdasarkan kedalaman. */
function CostRow({ line, depth }: { line: CostLineDto; depth: number }) {
  return (
    <>
      <tr>
        <td>
          <span style={{ paddingLeft: `${depth * 20}px` }}>
            {depth > 0 && <span className="tree-branch">└ </span>}
            {line.code} - {line.name}
            {line.hasBom && <span className="badge badge-active tree-tag">BOM</span>}
          </span>
        </td>
        <td className="num">
          {qty(line.quantityPerOutput)} {line.uomCode}
        </td>
        <td className="num">{qty(line.scrapPercent * 100)}%</td>
        <td className="num">{rupiah(line.unitCost)}</td>
        <td className="num">
          <strong>{rupiah(line.extendedCost)}</strong>
        </td>
      </tr>
      {line.children?.map((child) => (
        <CostRow key={`${depth}-${child.itemId}-${child.uomCode}`} line={child} depth={depth + 1} />
      ))}
    </>
  );
}

export function CostingPage() {
  const [items, setItems] = useState<ItemDto[]>([]);
  const [bomProductIds, setBomProductIds] = useState<Set<number>>(new Set());
  const [selectedId, setSelectedId] = useState<number | ''>('');

  const [breakdown, setBreakdown] = useState<CostBreakdownDto | null>(null);
  const [snapshots, setSnapshots] = useState<CostSnapshotDto[]>([]);

  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Item ber-BOM Active didahulukan; sisanya (termasuk item beli) menyusul.
  const { withBom, others } = useMemo(() => {
    const wb: ItemDto[] = [];
    const ot: ItemDto[] = [];
    for (const item of items) {
      if (bomProductIds.has(item.id) && PRODUCT_TYPES.has(item.type)) wb.push(item);
      else ot.push(item);
    }
    return { withBom: wb, others: ot };
  }, [items, bomProductIds]);

  async function loadMasters() {
    setLoading(true);
    setError(null);
    try {
      const [itemsData, activeBoms] = await Promise.all([
        itemsApi.list(),
        bomsApi.list({ status: 'Active' }),
      ]);
      setItems(itemsData);
      setBomProductIds(new Set(activeBoms.map((b) => b.productItemId)));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal memuat data.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadMasters();
  }, []);

  async function selectItem(id: number | '') {
    setSelectedId(id);
    setBreakdown(null);
    setSnapshots([]);
    setError(null);
    if (id === '') return;
    setBusy(true);
    try {
      const [bd, snaps] = await Promise.all([costingApi.breakdown(id), costingApi.snapshots(id)]);
      setBreakdown(bd);
      setSnapshots(snaps);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menghitung biaya.');
    } finally {
      setBusy(false);
    }
  }

  async function saveSnapshot() {
    if (selectedId === '') return;
    setBusy(true);
    setError(null);
    try {
      const bd = await costingApi.rollup(selectedId);
      setBreakdown(bd);
      setSnapshots(await costingApi.snapshots(selectedId));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menyimpan snapshot.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1>Costing Standar</h1>
        {breakdown && (
          <button type="button" className="btn btn-primary" disabled={busy} onClick={saveSnapshot}>
            {busy ? 'Memproses...' : 'Simpan snapshot'}
          </button>
        )}
      </div>

      <div className="filters">
        <label className="filter">
          Item
          <select
            value={selectedId}
            onChange={(e) => void selectItem(e.target.value ? Number(e.target.value) : '')}
          >
            <option value="">-- pilih item --</option>
            {withBom.length > 0 && (
              <optgroup label="Produk (BOM Active)">
                {withBom.map((i) => (
                  <option key={i.id} value={i.id}>
                    {i.code} - {i.name}
                  </option>
                ))}
              </optgroup>
            )}
            <optgroup label="Item lain">
              {others.map((i) => (
                <option key={i.id} value={i.id}>
                  {i.code} - {i.name}
                </option>
              ))}
            </optgroup>
          </select>
        </label>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {loading && <p className="muted">Memuat...</p>}

      {breakdown && (
        <>
          <div className="cost-summary">
            <div className="cost-card">
              <span className="cost-label">Material</span>
              <span className="cost-value">{rupiah(breakdown.materialCost)}</span>
            </div>
            <div className="cost-card">
              <span className="cost-label">Labor</span>
              <span className="cost-value">{rupiah(breakdown.laborCost)}</span>
            </div>
            <div className="cost-card">
              <span className="cost-label">Overhead</span>
              <span className="cost-value">{rupiah(breakdown.overheadCost)}</span>
            </div>
            <div className="cost-card cost-total">
              <span className="cost-label">Total / {breakdown.outputUomCode}</span>
              <span className="cost-value">{rupiah(breakdown.totalCost)}</span>
            </div>
          </div>

          {breakdown.isPurchased ? (
            <div className="alert alert-info">
              Item dibeli (tanpa BOM Active): biaya diambil dari harga supplier preferred atau
              Standard Material Cost, per {breakdown.outputUomCode}.
            </div>
          ) : breakdown.components.length === 0 ? (
            <p className="muted">BOM tidak memiliki komponen.</p>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Komponen</th>
                    <th className="num">Qty / output</th>
                    <th className="num">Scrap</th>
                    <th className="num">Biaya / satuan</th>
                    <th className="num">Biaya / output</th>
                  </tr>
                </thead>
                <tbody>
                  {breakdown.components.map((line) => (
                    <CostRow key={`${line.itemId}-${line.uomCode}`} line={line} depth={0} />
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}

      {selectedId !== '' && (
        <>
          <div className="section-header">
            <h2>Riwayat Snapshot</h2>
          </div>
          {snapshots.length === 0 ? (
            <p className="muted">Belum ada snapshot untuk item ini.</p>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Tanggal</th>
                    <th className="num">Material</th>
                    <th className="num">Labor</th>
                    <th className="num">Overhead</th>
                    <th className="num">Total</th>
                    <th>Satuan</th>
                    <th>BOM</th>
                  </tr>
                </thead>
                <tbody>
                  {snapshots.map((s) => (
                    <tr key={s.id}>
                      <td>{s.asOfDate}</td>
                      <td className="num">{rupiah(s.materialCost)}</td>
                      <td className="num">{rupiah(s.laborCost)}</td>
                      <td className="num">{rupiah(s.overheadCost)}</td>
                      <td className="num">
                        <strong>{rupiah(s.totalCost)}</strong>
                      </td>
                      <td>{s.outputUomCode}</td>
                      <td>{s.bomVersion != null ? `v${s.bomVersion}` : '-'}</td>
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
