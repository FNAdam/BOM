import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { bomsApi } from '../api/boms';
import { itemsApi } from '../api/items';
import {
  BOM_STATUSES,
  type BomExplosionNodeDto,
  type BomListItemDto,
  type BomStatus,
  type ItemDto,
} from '../api/types';
import { Modal } from '../components/Modal';

const PRODUCT_TYPES = new Set(['SemiFinished', 'FinishedGood']);

function StatusBadge({ status }: { status: BomStatus }) {
  return <span className={`badge badge-${status.toLowerCase()}`}>{status}</span>;
}

function fmt(n: number): string {
  return n.toLocaleString('id-ID', { maximumFractionDigits: 6 });
}

/** Baris pohon explode dengan indentasi berdasarkan level. */
function ExplosionRow({ node }: { node: BomExplosionNodeDto }) {
  return (
    <>
      <tr>
        <td>
          <span style={{ paddingLeft: `${node.level * 20}px` }}>
            {node.level > 0 && <span className="tree-branch">└ </span>}
            {node.itemCode} - {node.itemName}
            {node.hasBom && <span className="badge badge-active tree-tag">BOM</span>}
          </span>
        </td>
        <td>{node.itemType}</td>
        <td className="num">{fmt(node.quantity)}</td>
        <td className="num">{fmt(node.scrapPercent * 100)}%</td>
        <td className="num">
          <strong>{fmt(node.effectiveQuantity)}</strong> {node.uomCode}
        </td>
      </tr>
      {node.children.map((child) => (
        <ExplosionRow key={`${child.level}-${child.itemId}-${child.uomCode}`} node={child} />
      ))}
    </>
  );
}

export function BomsPage() {
  const navigate = useNavigate();

  const [boms, setBoms] = useState<BomListItemDto[]>([]);
  const [products, setProducts] = useState<ItemDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [filterProduct, setFilterProduct] = useState<number | ''>('');
  const [filterStatus, setFilterStatus] = useState<BomStatus | ''>('');

  const [explosion, setExplosion] = useState<BomExplosionNodeDto | null>(null);
  const [explodeFor, setExplodeFor] = useState<BomListItemDto | null>(null);
  const [explodeQty, setExplodeQty] = useState(1);
  const [explodeError, setExplodeError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const params = {
        productItemId: filterProduct === '' ? undefined : filterProduct,
        status: filterStatus === '' ? undefined : filterStatus,
      };
      const [bomsData, itemsData] = await Promise.all([bomsApi.list(params), itemsApi.list()]);
      setBoms(bomsData);
      setProducts(itemsData.filter((i) => PRODUCT_TYPES.has(i.type)));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal memuat data.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filterProduct, filterStatus]);

  async function handleActivate(bom: BomListItemDto) {
    if (!window.confirm(`Aktifkan BOM ${bom.productCode} v${bom.version}?`)) return;
    setBusy(true);
    setError(null);
    try {
      await bomsApi.activate(bom.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal mengaktifkan.');
    } finally {
      setBusy(false);
    }
  }

  async function handleNewVersion(bom: BomListItemDto) {
    setBusy(true);
    setError(null);
    try {
      const created = await bomsApi.newVersion(bom.id);
      navigate(`/boms/${created.id}/edit`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal membuat versi baru.');
    } finally {
      setBusy(false);
    }
  }

  async function handleDelete(bom: BomListItemDto) {
    if (!window.confirm(`Hapus BOM ${bom.productCode} v${bom.version}?`)) return;
    setBusy(true);
    setError(null);
    try {
      await bomsApi.remove(bom.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menghapus.');
    } finally {
      setBusy(false);
    }
  }

  async function openExplode(bom: BomListItemDto) {
    setExplodeFor(bom);
    setExplodeQty(bom.outputQuantity);
    setExplosion(null);
    setExplodeError(null);
    try {
      const tree = await bomsApi.explode(bom.id, bom.outputQuantity);
      setExplosion(tree);
    } catch (err) {
      setExplodeError(err instanceof Error ? err.message : 'Gagal explode.');
    }
  }

  async function runExplode() {
    if (!explodeFor) return;
    setExplodeError(null);
    try {
      const tree = await bomsApi.explode(explodeFor.id, explodeQty);
      setExplosion(tree);
    } catch (err) {
      setExplodeError(err instanceof Error ? err.message : 'Gagal explode.');
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1>BOM</h1>
        <button type="button" className="btn btn-primary" onClick={() => navigate('/boms/new')}>
          + BOM Baru
        </button>
      </div>

      <div className="filters">
        <label className="filter">
          Produk
          <select
            value={filterProduct}
            onChange={(e) => setFilterProduct(e.target.value ? Number(e.target.value) : '')}
          >
            <option value="">Semua produk</option>
            {products.map((p) => (
              <option key={p.id} value={p.id}>
                {p.code} - {p.name}
              </option>
            ))}
          </select>
        </label>
        <label className="filter">
          Status
          <select
            value={filterStatus}
            onChange={(e) => setFilterStatus((e.target.value || '') as BomStatus | '')}
          >
            <option value="">Semua status</option>
            {BOM_STATUSES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </label>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      {loading ? (
        <p className="muted">Memuat...</p>
      ) : boms.length === 0 ? (
        <p className="muted">Belum ada BOM.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Produk</th>
                <th className="num">Versi</th>
                <th>Status</th>
                <th className="num">Output</th>
                <th className="num">Komponen</th>
                <th>Efektif</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {boms.map((bom) => (
                <tr key={bom.id}>
                  <td>
                    {bom.productCode} - {bom.productName}
                  </td>
                  <td className="num">{bom.version}</td>
                  <td>
                    <StatusBadge status={bom.status} />
                  </td>
                  <td className="num">
                    {fmt(bom.outputQuantity)} {bom.outputUomCode}
                  </td>
                  <td className="num">{bom.componentCount}</td>
                  <td>
                    {bom.effectiveFrom ?? '...'} s/d {bom.effectiveTo ?? '...'}
                  </td>
                  <td className="actions">
                    <button type="button" className="btn btn-sm" onClick={() => openExplode(bom)}>
                      Explode
                    </button>
                    <button
                      type="button"
                      className="btn btn-sm"
                      onClick={() => navigate(`/boms/${bom.id}/edit`)}
                    >
                      {bom.status === 'Draft' ? 'Edit' : 'Lihat'}
                    </button>
                    {bom.status === 'Draft' && (
                      <button
                        type="button"
                        className="btn btn-sm btn-primary"
                        disabled={busy}
                        onClick={() => handleActivate(bom)}
                      >
                        Activate
                      </button>
                    )}
                    <button
                      type="button"
                      className="btn btn-sm"
                      disabled={busy}
                      onClick={() => handleNewVersion(bom)}
                    >
                      New Version
                    </button>
                    {bom.status !== 'Active' && (
                      <button
                        type="button"
                        className="btn btn-sm btn-danger"
                        disabled={busy}
                        onClick={() => handleDelete(bom)}
                      >
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

      {explodeFor && (
        <Modal
          title={`Explode: ${explodeFor.productCode} v${explodeFor.version}`}
          onClose={() => {
            setExplodeFor(null);
            setExplosion(null);
          }}
        >
          <div className="explode-controls">
            <label>
              Target Qty ({explodeFor.outputUomCode})
              <input
                type="number"
                step="0.000001"
                min="0"
                value={explodeQty}
                onChange={(e) => setExplodeQty(Number(e.target.value))}
              />
            </label>
            <button type="button" className="btn btn-primary" onClick={runExplode}>
              Hitung
            </button>
          </div>

          {explodeError && <div className="alert alert-error">{explodeError}</div>}

          {explosion && (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Item</th>
                    <th>Type</th>
                    <th className="num">Qty</th>
                    <th className="num">Scrap</th>
                    <th className="num">Efektif</th>
                  </tr>
                </thead>
                <tbody>
                  <ExplosionRow node={explosion} />
                </tbody>
              </table>
            </div>
          )}
        </Modal>
      )}
    </div>
  );
}
