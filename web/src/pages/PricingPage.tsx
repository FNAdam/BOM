import { useEffect, useMemo, useState } from 'react';
import { itemsApi } from '../api/items';
import { salesChannelsApi } from '../api/salesChannels';
import { pricingSettingsApi } from '../api/pricingSettings';
import { packLevelsApi } from '../api/packLevels';
import { pricingApi } from '../api/pricing';
import { priceListsApi } from '../api/priceLists';
import { Modal } from '../components/Modal';
import {
  COST_BASES,
  PRICING_METHODS,
  ROUNDING_RULES,
  type CostBasis,
  type ItemDto,
  type PackLevelDto,
  type PricingMethod,
  type PricingResultDto,
  type PriceListListItemDto,
  type RoundingRule,
  type SalesChannelDto,
  type SimulatePricingRequest,
  type UpsertPackLevelDto,
} from '../api/types';
import { PageHeader } from '../components/PageHeader';
import { bomStatusLabel, costBasisLabel, pricingMethodLabel, roundingRuleLabel } from '../labels';

const PRODUCT_TYPES = new Set(['SemiFinished', 'FinishedGood']);

function rupiah(n: number): string {
  return n.toLocaleString('id-ID', { maximumFractionDigits: 0 });
}

function pct(n: number): string {
  return `${(n * 100).toLocaleString('id-ID', { maximumFractionDigits: 2 })}%`;
}

/** Persen string ("45") -> desimal (0.45) atau undefined bila kosong. */
function pctToDec(value: string): number | undefined {
  if (value.trim() === '') return undefined;
  const n = Number(value);
  return Number.isFinite(n) ? n / 100 : undefined;
}

function numOrNull(value: string): number | null {
  if (value.trim() === '') return null;
  const n = Number(value);
  return Number.isFinite(n) ? n : null;
}

export function PricingPage() {
  const [items, setItems] = useState<ItemDto[]>([]);
  const [channels, setChannels] = useState<SalesChannelDto[]>([]);
  const [packLevels, setPackLevels] = useState<PackLevelDto[]>([]);
  const [priceLists, setPriceLists] = useState<PriceListListItemDto[]>([]);

  const [selectedId, setSelectedId] = useState<number | ''>('');

  // Setelan yang dapat diubah (persen disimpan sebagai string persen, mis. "45").
  const [method, setMethod] = useState<PricingMethod>('Margin');
  const [costBasis, setCostBasis] = useState<CostBasis>('Standard');
  const [targetPct, setTargetPct] = useState('45');
  const [taxPct, setTaxPct] = useState('11');
  const [roundingRule, setRoundingRule] = useState<RoundingRule>('None');
  const [fixedCost, setFixedCost] = useState('');
  const [manualBasePrice, setManualBasePrice] = useState('');
  const [floorCost, setFloorCost] = useState('');
  const [channelMarginPct, setChannelMarginPct] = useState<Record<number, string>>({});
  const [packDiscountPct, setPackDiscountPct] = useState<Record<number, string>>({});

  const [result, setResult] = useState<PricingResultDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [showPacks, setShowPacks] = useState(false);

  const products = useMemo(() => items.filter((i) => PRODUCT_TYPES.has(i.type)), [items]);

  async function loadMasters() {
    setLoading(true);
    setError(null);
    try {
      const [itemsData, channelsData, settings] = await Promise.all([
        itemsApi.list(),
        salesChannelsApi.list(),
        pricingSettingsApi.get(),
      ]);
      setItems(itemsData);
      setChannels(channelsData);
      setTargetPct(String(settings.defaultTargetMarginPercent * 100));
      setTaxPct(String(settings.defaultTaxPercent * 100));
      setRoundingRule(settings.roundingRule);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal memuat data.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadMasters();
  }, []);

  async function loadPacksAndLists(productId: number) {
    const [packs, lists] = await Promise.all([
      packLevelsApi.list(productId),
      priceListsApi.list({ productItemId: productId }),
    ]);
    setPackLevels(packs);
    setPriceLists(lists);
  }

  async function selectProduct(id: number | '') {
    setSelectedId(id);
    setResult(null);
    setError(null);
    setNotice(null);
    setPackLevels([]);
    setPriceLists([]);
    setChannelMarginPct({});
    setPackDiscountPct({});
    if (id === '') return;
    try {
      await loadPacksAndLists(id);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal memuat kemasan / price list.');
    }
  }

  // Susun request simulasi dari state saat ini.
  const request = useMemo<SimulatePricingRequest | null>(() => {
    if (selectedId === '') return null;
    const channelMargins = channels
      .map((c) => ({ salesChannelId: c.id, dec: pctToDec(channelMarginPct[c.id] ?? '') }))
      .filter((c): c is { salesChannelId: number; dec: number } => c.dec !== undefined)
      .map((c) => ({ salesChannelId: c.salesChannelId, marginPercent: c.dec }));
    const packDiscounts = packLevels
      .map((p) => ({ packLevelId: p.id, dec: pctToDec(packDiscountPct[p.id] ?? '') }))
      .filter((p): p is { packLevelId: number; dec: number } => p.dec !== undefined && p.dec !== 0)
      .map((p) => ({ packLevelId: p.packLevelId, discountPercent: p.dec }));

    return {
      productItemId: selectedId,
      pricingMethod: method,
      costBasis,
      taxMode: 'Exclusive',
      targetPercent: pctToDec(targetPct) ?? null,
      taxPercent: pctToDec(taxPct) ?? null,
      roundingRule,
      manualBasePrice: method === 'Manual' ? numOrNull(manualBasePrice) : null,
      floorCost: numOrNull(floorCost),
      fixedCostPerPeriod: numOrNull(fixedCost),
      channelMargins: channelMargins.length ? channelMargins : null,
      packDiscounts: packDiscounts.length ? packDiscounts : null,
    };
  }, [
    selectedId, method, costBasis, targetPct, taxPct, roundingRule, manualBasePrice, floorCost,
    fixedCost, channels, channelMarginPct, packLevels, packDiscountPct,
  ]);

  // Simulasi live (debounce) tiap kali request berubah.
  const requestKey = request ? JSON.stringify(request) : '';
  useEffect(() => {
    if (!request) return;
    let cancelled = false;
    const timer = setTimeout(async () => {
      setBusy(true);
      setError(null);
      try {
        const res = await pricingApi.simulate(request);
        if (!cancelled) setResult(res);
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Gagal menghitung harga.');
      } finally {
        if (!cancelled) setBusy(false);
      }
    }, 300);
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [requestKey]);

  function cellValue(tier: string, packLevelId: number): number | null {
    const cell = result?.cells.find((c) => c.tierName === tier && c.packLevelId === packLevelId);
    if (!cell) return null;
    return tier === 'HET' ? cell.grossPrice : cell.netPrice;
  }

  async function savePriceList(activate: boolean) {
    if (!request) return;
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      const created = await priceListsApi.create({ simulation: request });
      if (activate) await priceListsApi.activate(created.id);
      if (selectedId !== '') await loadPacksAndLists(selectedId);
      setNotice(activate ? 'Price list disimpan & diaktifkan.' : 'Price list disimpan sebagai Draft.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menyimpan price list.');
    } finally {
      setBusy(false);
    }
  }

  async function activateList(id: number) {
    setError(null);
    try {
      await priceListsApi.activate(id);
      if (selectedId !== '') await loadPacksAndLists(selectedId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal mengaktifkan.');
    }
  }

  async function deleteList(id: number) {
    if (!window.confirm('Hapus price list ini?')) return;
    setError(null);
    try {
      await priceListsApi.remove(id);
      if (selectedId !== '') await loadPacksAndLists(selectedId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menghapus.');
    }
  }

  return (
    <div>
      <PageHeader
        title="Tetapkan Harga Jual"
        description="Hitung harga jual dari biaya produksi + margin yang diinginkan, termasuk PPN dan harga per kemasan (botol/pack/karton)."
        actions={
          result &&
          selectedId !== '' && (
            <div className="form-actions" style={{ margin: 0 }}>
              <button type="button" className="btn" disabled={busy} onClick={() => void savePriceList(false)}>
                Simpan sebagai Draf
              </button>
              <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void savePriceList(true)}>
                Simpan &amp; Aktifkan
              </button>
            </div>
          )
        }
      />

      {error && <div className="alert alert-error">{error}</div>}
      {notice && <div className="alert alert-info">{notice}</div>}
      {loading && <p className="muted">Memuat...</p>}

      <div className="filters">
        <label className="filter">
          Produk
          <select value={selectedId} onChange={(e) => void selectProduct(e.target.value ? Number(e.target.value) : '')}>
            <option value="">-- pilih produk --</option>
            {products.map((p) => (
              <option key={p.id} value={p.id}>
                {p.code} - {p.name}
              </option>
            ))}
          </select>
        </label>
        <label className="filter">
          Dasar biaya
          <select value={costBasis} onChange={(e) => setCostBasis(e.target.value as CostBasis)}>
            {COST_BASES.map((b) => (
              <option key={b} value={b}>
                {costBasisLabel(b)}
              </option>
            ))}
          </select>
        </label>
        {selectedId !== '' && (
          <label className="filter" style={{ justifyContent: 'flex-end' }}>
            &nbsp;
            <button type="button" className="btn btn-sm" onClick={() => setShowPacks(true)}>
              Kelola Kemasan ({packLevels.length})
            </button>
          </label>
        )}
      </div>

      {selectedId === '' ? (
        <p className="muted">Pilih produk untuk mulai menetapkan harga.</p>
      ) : (
        <>
          <div className="pricing-grid">
            <fieldset className="editor-fieldset">
              <div className="section-header">
                <h2>Setelan</h2>
              </div>
              <div className="form-grid">
                <label>
                  Metode Hitung Harga
                  <select value={method} onChange={(e) => setMethod(e.target.value as PricingMethod)}>
                    {PRICING_METHODS.map((m) => (
                      <option key={m} value={m}>
                        {pricingMethodLabel(m)}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Target Margin (%)
                  <input
                    type="number"
                    value={targetPct}
                    disabled={method === 'Manual'}
                    onChange={(e) => setTargetPct(e.target.value)}
                  />
                  <span className="field-hint">Berapa persen untung yang diinginkan dari harga jual.</span>
                </label>
                {method === 'Manual' && (
                  <label>
                    Harga Dasar (ditentukan sendiri)
                    <input
                      type="number"
                      value={manualBasePrice}
                      onChange={(e) => setManualBasePrice(e.target.value)}
                    />
                  </label>
                )}
                <label>
                  PPN (%)
                  <input type="number" value={taxPct} onChange={(e) => setTaxPct(e.target.value)} />
                </label>
                <label>
                  Pembulatan Harga
                  <select value={roundingRule} onChange={(e) => setRoundingRule(e.target.value as RoundingRule)}>
                    {ROUNDING_RULES.map((r) => (
                      <option key={r} value={r}>
                        {roundingRuleLabel(r)}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Harga Minimum / Floor (opsional)
                  <input type="number" value={floorCost} onChange={(e) => setFloorCost(e.target.value)} />
                  <span className="field-hint">Harga jual tidak akan pernah disarankan di bawah angka ini.</span>
                </label>
                <label>
                  Biaya Tetap / Bulan (untuk break-even)
                  <input type="number" value={fixedCost} onChange={(e) => setFixedCost(e.target.value)} />
                  <span className="field-hint">
                    Mis. sewa tempat, gaji tetap - dipakai untuk hitung target jual bulanan (break-even).
                  </span>
                </label>
              </div>

              <div className="section-header">
                <h2>Margin Tambahan per Kanal Penjualan</h2>
              </div>
              <p className="muted" style={{ marginTop: -6 }}>
                Kosongkan untuk pakai margin default kanal tersebut (lihat menu Kanal Penjualan).
              </p>
              <div className="form-grid">
                {channels.map((c) => (
                  <label key={c.id}>
                    {c.name} (%)
                    <input
                      type="number"
                      placeholder={String(c.defaultMarginPercent * 100)}
                      value={channelMarginPct[c.id] ?? ''}
                      onChange={(e) => setChannelMarginPct({ ...channelMarginPct, [c.id]: e.target.value })}
                    />
                  </label>
                ))}
              </div>

              {packLevels.length > 0 && (
                <>
                  <div className="section-header">
                    <h2>Diskon per Kemasan</h2>
                  </div>
                  <div className="form-grid">
                    {packLevels.map((p) => (
                      <label key={p.id}>
                        {p.name} (%)
                        <input
                          type="number"
                          placeholder="0"
                          value={packDiscountPct[p.id] ?? ''}
                          onChange={(e) => setPackDiscountPct({ ...packDiscountPct, [p.id]: e.target.value })}
                        />
                      </label>
                    ))}
                  </div>
                </>
              )}
            </fieldset>

            <div className="editor-fieldset">
              {result && (
                <>
                  <div className="cost-summary">
                    <div className="cost-card">
                      <span className="cost-label">Biaya Produksi / satuan dasar</span>
                      <span className="cost-value">{rupiah(result.cost)}</span>
                    </div>
                    <div className="cost-card cost-total">
                      <span className="cost-label">Harga Dasar (ex-factory)</span>
                      <span className="cost-value">{rupiah(result.basePrice)}</span>
                    </div>
                    <div className="cost-card">
                      <span className="cost-label">Margin</span>
                      <span className="cost-value">{pct(result.marginPct)}</span>
                    </div>
                    <div className="cost-card">
                      <span className="cost-label">Untung / satuan dasar</span>
                      <span className="cost-value">{rupiah(result.contributionMargin)}</span>
                    </div>
                    <div className="cost-card">
                      <span className="cost-label">Target Jual Balik Modal (unit/bulan)</span>
                      <span className="cost-value">{result.breakEvenQty != null ? rupiah(result.breakEvenQty) : '-'}</span>
                    </div>
                  </div>

                  {result.floorViolation && (
                    <div className="alert alert-error">
                      Peringatan: harga dasar di bawah harga minimum (floor), atau margin di bawah batas minimum
                      yang ditentukan di Setelan Harga.
                    </div>
                  )}

                  <div className="table-wrap">
                    <table>
                      <thead>
                        <tr>
                          <th>Kemasan</th>
                          <th className="num">Isi / satuan dasar</th>
                          {result.tiers.map((t) => (
                            <th key={t} className="num">
                              {t}
                              {t === 'HET' ? ' *' : ''}
                            </th>
                          ))}
                        </tr>
                      </thead>
                      <tbody>
                        {result.packLevels.map((p) => (
                          <tr key={p.id}>
                            <td>{p.name}</td>
                            <td className="num">{p.qtyPerBase.toLocaleString('id-ID', { maximumFractionDigits: 6 })}</td>
                            {result.tiers.map((t) => {
                              const v = cellValue(t, p.id);
                              return (
                                <td key={t} className="num">
                                  {v != null ? rupiah(v) : '-'}
                                </td>
                              );
                            })}
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                  <p className="variance-hint muted">* HET = harga eceran tertinggi, termasuk PPN. Kolom lain tanpa PPN (net).</p>
                </>
              )}
              {busy && !result && <p className="muted">Menghitung...</p>}
            </div>
          </div>

          <div className="section-header">
            <h2>Riwayat Harga Tersimpan</h2>
          </div>
          {priceLists.length === 0 ? (
            <p className="muted">Belum ada harga tersimpan untuk produk ini.</p>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>#</th>
                    <th>Metode</th>
                    <th>Dasar biaya</th>
                    <th className="num">Target</th>
                    <th>Status</th>
                    <th className="num">Baris</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {priceLists.map((pl) => (
                    <tr key={pl.id}>
                      <td>{pl.id}</td>
                      <td>{pricingMethodLabel(pl.pricingMethod)}</td>
                      <td>{costBasisLabel(pl.costBasis)}</td>
                      <td className="num">{pct(pl.targetPercent)}</td>
                      <td>
                        <span className={`badge badge-${pl.status.toLowerCase()}`}>{bomStatusLabel(pl.status)}</span>
                      </td>
                      <td className="num">{pl.lineCount}</td>
                      <td className="actions">
                        {pl.status !== 'Active' && (
                          <button type="button" className="btn btn-sm" onClick={() => void activateList(pl.id)}>
                            Aktifkan
                          </button>
                        )}
                        {pl.status !== 'Active' && (
                          <button type="button" className="btn btn-sm btn-danger" onClick={() => void deleteList(pl.id)}>
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
        </>
      )}

      {showPacks && selectedId !== '' && (
        <PackLevelsManager
          itemId={selectedId}
          levels={packLevels}
          onClose={() => setShowPacks(false)}
          onChanged={async () => {
            await loadPacksAndLists(selectedId);
          }}
        />
      )}
    </div>
  );
}

// --------------------------------------------------------- Pack level manager

const emptyPack: UpsertPackLevelDto = {
  name: '',
  qtyPerBase: 1,
  barcode: null,
  sequence: 0,
  isDefaultSales: false,
};

function PackLevelsManager({
  itemId,
  levels,
  onClose,
  onChanged,
}: {
  itemId: number;
  levels: PackLevelDto[];
  onClose: () => void;
  onChanged: () => Promise<void>;
}) {
  const [form, setForm] = useState<UpsertPackLevelDto>(emptyPack);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  function reset() {
    setForm(emptyPack);
    setEditingId(null);
    setErr(null);
  }

  function edit(p: PackLevelDto) {
    setEditingId(p.id);
    setErr(null);
    setForm({
      name: p.name,
      qtyPerBase: p.qtyPerBase,
      barcode: p.barcode,
      sequence: p.sequence,
      isDefaultSales: p.isDefaultSales,
    });
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setErr(null);
    try {
      if (editingId === null) await packLevelsApi.create(itemId, form);
      else await packLevelsApi.update(itemId, editingId, form);
      await onChanged();
      reset();
    } catch (error) {
      setErr(error instanceof Error ? error.message : 'Gagal menyimpan kemasan.');
    } finally {
      setSaving(false);
    }
  }

  async function remove(p: PackLevelDto) {
    if (!window.confirm(`Hapus kemasan "${p.name}"?`)) return;
    setErr(null);
    try {
      await packLevelsApi.remove(itemId, p.id);
      await onChanged();
      if (editingId === p.id) reset();
    } catch (error) {
      setErr(error instanceof Error ? error.message : 'Gagal menghapus.');
    }
  }

  return (
    <Modal title="Kelola Jenjang Kemasan" onClose={onClose}>
      {err && <div className="alert alert-error">{err}</div>}

      {levels.length > 0 && (
        <div className="table-wrap" style={{ marginBottom: 16 }}>
          <table>
            <thead>
              <tr>
                <th>Nama</th>
                <th className="num">Isi / satuan dasar</th>
                <th>Default</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {levels.map((p) => (
                <tr key={p.id}>
                  <td>{p.name}</td>
                  <td className="num">{p.qtyPerBase}</td>
                  <td>{p.isDefaultSales ? 'Ya' : '-'}</td>
                  <td className="actions">
                    <button type="button" className="btn btn-sm" onClick={() => edit(p)}>
                      Edit
                    </button>
                    <button type="button" className="btn btn-sm btn-danger" onClick={() => void remove(p)}>
                      Hapus
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <form onSubmit={submit}>
        <label>
          Nama (mis. Botol / Pack / Karton)
          <input value={form.name} maxLength={40} required onChange={(e) => setForm({ ...form, name: e.target.value })} />
        </label>
        <label>
          Isi per Satuan Dasar
          <input
            type="number"
            step="any"
            min="0"
            value={form.qtyPerBase}
            required
            onChange={(e) => setForm({ ...form, qtyPerBase: Number(e.target.value) })}
          />
          <span className="field-hint">Mis. kalau 1 Pack isi 12 botol, isi 12.</span>
        </label>
        <label>
          Barcode (opsional)
          <input
            value={form.barcode ?? ''}
            maxLength={40}
            onChange={(e) => setForm({ ...form, barcode: e.target.value ? e.target.value : null })}
          />
        </label>
        <label>
          Urutan
          <input
            type="number"
            value={form.sequence}
            onChange={(e) => setForm({ ...form, sequence: Number(e.target.value) })}
          />
        </label>
        <label className="checkbox">
          <input
            type="checkbox"
            checked={form.isDefaultSales}
            onChange={(e) => setForm({ ...form, isDefaultSales: e.target.checked })}
          />
          Kemasan default penjualan
        </label>
        <div className="form-actions">
          {editingId !== null && (
            <button type="button" className="btn" onClick={reset}>
              Batal edit
            </button>
          )}
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Menyimpan...' : editingId === null ? 'Tambah' : 'Simpan'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
