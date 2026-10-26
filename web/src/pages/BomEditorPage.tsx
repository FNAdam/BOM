import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { bomsApi } from '../api/boms';
import { itemsApi } from '../api/items';
import { uomsApi } from '../api/uoms';
import type {
  BomStatus,
  ItemDto,
  UomDto,
  UpsertBomComponentDto,
  UpsertBomDto,
  UpsertBomOperationDto,
} from '../api/types';

const PRODUCT_TYPES = new Set(['SemiFinished', 'FinishedGood']);

const emptyComponent = (sequence: number): UpsertBomComponentDto => ({
  componentItemId: 0,
  quantity: 1,
  uomId: 0,
  scrapPercent: 0,
  sequence,
  isOptional: false,
  notes: null,
});

const emptyOperation = (sequence: number): UpsertBomOperationDto => ({
  sequence,
  name: '',
  laborCostPerOutputUnit: 0,
  overheadRate: 0,
});

export function BomEditorPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const editingId = id ? Number(id) : null;

  const [items, setItems] = useState<ItemDto[]>([]);
  const [uoms, setUoms] = useState<UomDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [status, setStatus] = useState<BomStatus>('Draft');

  const [productItemId, setProductItemId] = useState<number>(0);
  const [outputQuantity, setOutputQuantity] = useState<number>(1);
  const [outputUomId, setOutputUomId] = useState<number>(0);
  const [yieldPercent, setYieldPercent] = useState<number>(1);
  const [effectiveFrom, setEffectiveFrom] = useState<string | null>(null);
  const [effectiveTo, setEffectiveTo] = useState<string | null>(null);
  const [notes, setNotes] = useState<string>('');
  const [components, setComponents] = useState<UpsertBomComponentDto[]>([]);
  const [operations, setOperations] = useState<UpsertBomOperationDto[]>([]);

  const readOnly = editingId !== null && status !== 'Draft';
  const products = items.filter((i) => PRODUCT_TYPES.has(i.type));

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError(null);
      try {
        const [itemsData, uomsData] = await Promise.all([itemsApi.list(), uomsApi.list()]);
        setItems(itemsData);
        setUoms(uomsData);

        if (editingId !== null) {
          const bom = await bomsApi.get(editingId);
          setStatus(bom.status);
          setProductItemId(bom.productItemId);
          setOutputQuantity(bom.outputQuantity);
          setOutputUomId(bom.outputUomId);
          setYieldPercent(bom.yieldPercent);
          setEffectiveFrom(bom.effectiveFrom);
          setEffectiveTo(bom.effectiveTo);
          setNotes(bom.notes ?? '');
          setComponents(
            bom.components.map((c) => ({
              componentItemId: c.componentItemId,
              quantity: c.quantity,
              uomId: c.uomId,
              scrapPercent: c.scrapPercent,
              sequence: c.sequence,
              isOptional: c.isOptional,
              notes: null,
            })),
          );
          setOperations(
            bom.operations.map((o) => ({
              sequence: o.sequence,
              name: o.name,
              laborCostPerOutputUnit: o.laborCostPerOutputUnit,
              overheadRate: o.overheadRate,
            })),
          );
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Gagal memuat data.');
      } finally {
        setLoading(false);
      }
    }
    void load();
  }, [editingId]);

  function updateComponent(index: number, patch: Partial<UpsertBomComponentDto>) {
    setComponents((prev) => prev.map((c, i) => (i === index ? { ...c, ...patch } : c)));
  }

  function updateOperation(index: number, patch: Partial<UpsertBomOperationDto>) {
    setOperations((prev) => prev.map((o, i) => (i === index ? { ...o, ...patch } : o)));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError(null);

    const dto: UpsertBomDto = {
      productItemId,
      outputQuantity,
      outputUomId,
      yieldPercent,
      effectiveFrom,
      effectiveTo,
      notes: notes.trim() ? notes.trim() : null,
      components,
      operations,
    };

    try {
      if (editingId === null) {
        await bomsApi.create(dto);
      } else {
        await bomsApi.update(editingId, dto);
      }
      navigate('/boms');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menyimpan.');
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <p className="muted">Memuat...</p>;

  const title = editingId === null ? 'BOM Baru' : readOnly ? `BOM (${status}) - hanya baca` : 'Edit BOM';

  return (
    <div>
      <div className="page-header">
        <h1>{title}</h1>
        <button type="button" className="btn" onClick={() => navigate('/boms')}>
          Kembali
        </button>
      </div>

      {readOnly && (
        <div className="alert alert-info">
          BOM berstatus {status} tidak dapat diubah. Gunakan &quot;New Version&quot; untuk merevisi.
        </div>
      )}
      {error && <div className="alert alert-error">{error}</div>}

      <form onSubmit={handleSubmit}>
        <fieldset disabled={readOnly} className="editor-fieldset">
          <div className="form-grid">
            <label>
              Produk (semi/finished)
              <select
                value={productItemId}
                required
                onChange={(e) => setProductItemId(Number(e.target.value))}
              >
                <option value={0} disabled>
                  -- pilih produk --
                </option>
                {products.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.code} - {p.name}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Output Qty
              <input
                type="number"
                step="0.000001"
                min="0"
                value={outputQuantity}
                onChange={(e) => setOutputQuantity(Number(e.target.value))}
              />
            </label>

            <label>
              Output UOM
              <select value={outputUomId} required onChange={(e) => setOutputUomId(Number(e.target.value))}>
                <option value={0} disabled>
                  -- pilih UOM --
                </option>
                {uoms.map((u) => (
                  <option key={u.id} value={u.id}>
                    {u.code} - {u.name}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Yield (0-1)
              <input
                type="number"
                step="0.0001"
                min="0"
                max="1"
                value={yieldPercent}
                onChange={(e) => setYieldPercent(Number(e.target.value))}
              />
            </label>

            <label>
              Efektif Dari
              <input
                type="date"
                value={effectiveFrom ?? ''}
                onChange={(e) => setEffectiveFrom(e.target.value || null)}
              />
            </label>

            <label>
              Efektif Sampai
              <input
                type="date"
                value={effectiveTo ?? ''}
                onChange={(e) => setEffectiveTo(e.target.value || null)}
              />
            </label>
          </div>

          <label>
            Catatan
            <input value={notes} maxLength={500} onChange={(e) => setNotes(e.target.value)} />
          </label>

          {/* --- Components --- */}
          <div className="section-header">
            <h2>Komponen</h2>
            {!readOnly && (
              <button
                type="button"
                className="btn btn-sm"
                onClick={() => setComponents((prev) => [...prev, emptyComponent(prev.length + 1)])}
              >
                + Baris
              </button>
            )}
          </div>

          {components.length === 0 ? (
            <p className="muted">Belum ada komponen.</p>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th style={{ width: 60 }}>Seq</th>
                    <th>Item</th>
                    <th className="num">Qty</th>
                    <th>UOM</th>
                    <th className="num">Scrap</th>
                    <th>Opsional</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {components.map((c, index) => (
                    <tr key={index}>
                      <td>
                        <input
                          type="number"
                          min="0"
                          value={c.sequence}
                          onChange={(e) => updateComponent(index, { sequence: Number(e.target.value) })}
                        />
                      </td>
                      <td>
                        <select
                          value={c.componentItemId}
                          required
                          onChange={(e) => updateComponent(index, { componentItemId: Number(e.target.value) })}
                        >
                          <option value={0} disabled>
                            -- item --
                          </option>
                          {items.map((i) => (
                            <option key={i.id} value={i.id}>
                              {i.code} - {i.name}
                            </option>
                          ))}
                        </select>
                      </td>
                      <td>
                        <input
                          type="number"
                          step="0.000001"
                          min="0"
                          value={c.quantity}
                          onChange={(e) => updateComponent(index, { quantity: Number(e.target.value) })}
                        />
                      </td>
                      <td>
                        <select
                          value={c.uomId}
                          required
                          onChange={(e) => updateComponent(index, { uomId: Number(e.target.value) })}
                        >
                          <option value={0} disabled>
                            -- UOM --
                          </option>
                          {uoms.map((u) => (
                            <option key={u.id} value={u.id}>
                              {u.code}
                            </option>
                          ))}
                        </select>
                      </td>
                      <td>
                        <input
                          type="number"
                          step="0.0001"
                          min="0"
                          value={c.scrapPercent}
                          onChange={(e) => updateComponent(index, { scrapPercent: Number(e.target.value) })}
                        />
                      </td>
                      <td>
                        <input
                          type="checkbox"
                          checked={c.isOptional}
                          onChange={(e) => updateComponent(index, { isOptional: e.target.checked })}
                        />
                      </td>
                      <td className="actions">
                        {!readOnly && (
                          <button
                            type="button"
                            className="btn btn-sm btn-danger"
                            onClick={() => setComponents((prev) => prev.filter((_, i) => i !== index))}
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

          {/* --- Operations --- */}
          <div className="section-header">
            <h2>Operasi (opsional)</h2>
            {!readOnly && (
              <button
                type="button"
                className="btn btn-sm"
                onClick={() => setOperations((prev) => [...prev, emptyOperation(prev.length + 1)])}
              >
                + Baris
              </button>
            )}
          </div>

          {operations.length === 0 ? (
            <p className="muted">Belum ada operasi.</p>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th style={{ width: 60 }}>Seq</th>
                    <th>Nama</th>
                    <th className="num">Labor / unit</th>
                    <th className="num">Overhead</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {operations.map((o, index) => (
                    <tr key={index}>
                      <td>
                        <input
                          type="number"
                          min="0"
                          value={o.sequence}
                          onChange={(e) => updateOperation(index, { sequence: Number(e.target.value) })}
                        />
                      </td>
                      <td>
                        <input
                          value={o.name}
                          maxLength={100}
                          required
                          onChange={(e) => updateOperation(index, { name: e.target.value })}
                        />
                      </td>
                      <td>
                        <input
                          type="number"
                          step="0.0001"
                          min="0"
                          value={o.laborCostPerOutputUnit}
                          onChange={(e) =>
                            updateOperation(index, { laborCostPerOutputUnit: Number(e.target.value) })
                          }
                        />
                      </td>
                      <td>
                        <input
                          type="number"
                          step="0.0001"
                          min="0"
                          value={o.overheadRate}
                          onChange={(e) => updateOperation(index, { overheadRate: Number(e.target.value) })}
                        />
                      </td>
                      <td className="actions">
                        {!readOnly && (
                          <button
                            type="button"
                            className="btn btn-sm btn-danger"
                            onClick={() => setOperations((prev) => prev.filter((_, i) => i !== index))}
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
        </fieldset>

        {!readOnly && (
          <div className="form-actions editor-actions">
            <button type="button" className="btn" onClick={() => navigate('/boms')}>
              Batal
            </button>
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Menyimpan...' : 'Simpan sebagai Draft'}
            </button>
          </div>
        )}
      </form>
    </div>
  );
}
