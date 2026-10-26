import { useEffect, useState } from 'react';
import { itemSupplierPricesApi } from '../api/itemSupplierPrices';
import { itemsApi } from '../api/items';
import { suppliersApi } from '../api/suppliers';
import { uomsApi } from '../api/uoms';
import type {
  ItemDto,
  ItemSupplierPriceDto,
  SupplierDto,
  UomDto,
  UpsertItemSupplierPriceDto,
} from '../api/types';
import { Modal } from '../components/Modal';

const emptyForm: UpsertItemSupplierPriceDto = {
  itemId: 0,
  supplierId: 0,
  uomId: 0,
  price: 0,
  currency: 'IDR',
  minOrderQty: 0,
  effectiveFrom: null,
  effectiveTo: null,
  isPreferred: false,
};

export function ItemPricesPage() {
  const [prices, setPrices] = useState<ItemSupplierPriceDto[]>([]);
  const [items, setItems] = useState<ItemDto[]>([]);
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [uoms, setUoms] = useState<UomDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<UpsertItemSupplierPriceDto>(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [pricesData, itemsData, suppliersData, uomsData] = await Promise.all([
        itemSupplierPricesApi.list(),
        itemsApi.list(),
        suppliersApi.list(),
        uomsApi.list(),
      ]);
      setPrices(pricesData);
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
  }, []);

  function openCreate() {
    setForm({
      ...emptyForm,
      itemId: items[0]?.id ?? 0,
      supplierId: suppliers[0]?.id ?? 0,
      uomId: uoms[0]?.id ?? 0,
    });
    setFormError(null);
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setFormError(null);
    try {
      await itemSupplierPricesApi.create(form);
      setShowForm(false);
      await load();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Gagal menyimpan.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(price: ItemSupplierPriceDto) {
    if (!window.confirm(`Hapus harga ${price.itemName} dari ${price.supplierName}?`)) return;
    try {
      await itemSupplierPricesApi.remove(price.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menghapus.');
    }
  }

  const canCreate = items.length > 0 && suppliers.length > 0 && uoms.length > 0;

  return (
    <div>
      <div className="page-header">
        <h1>Harga Item - Supplier</h1>
        <button type="button" className="btn btn-primary" onClick={openCreate} disabled={!canCreate}>
          + Tambah Harga
        </button>
      </div>

      {!canCreate && !loading && (
        <div className="alert alert-info">Butuh minimal 1 item, 1 supplier, dan 1 UOM untuk menambah harga.</div>
      )}
      {error && <div className="alert alert-error">{error}</div>}

      {loading ? (
        <p className="muted">Memuat...</p>
      ) : prices.length === 0 ? (
        <p className="muted">Belum ada data harga.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Item</th>
                <th>Supplier</th>
                <th>UOM</th>
                <th className="num">Price</th>
                <th>Currency</th>
                <th className="num">Min Qty</th>
                <th>Berlaku</th>
                <th>Preferred</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {prices.map((price) => (
                <tr key={price.id}>
                  <td>{price.itemName}</td>
                  <td>{price.supplierName}</td>
                  <td>{price.uomCode}</td>
                  <td className="num">{price.price.toLocaleString('id-ID')}</td>
                  <td>{price.currency}</td>
                  <td className="num">{price.minOrderQty.toLocaleString('id-ID')}</td>
                  <td>
                    {price.effectiveFrom ?? '...'} s/d {price.effectiveTo ?? '...'}
                  </td>
                  <td>{price.isPreferred ? 'Ya' : 'Tidak'}</td>
                  <td className="actions">
                    <button type="button" className="btn btn-sm btn-danger" onClick={() => handleDelete(price)}>
                      Hapus
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showForm && (
        <Modal title="Tambah Harga" onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit}>
            {formError && <div className="alert alert-error">{formError}</div>}

            <label>
              Item
              <select
                value={form.itemId}
                required
                onChange={(e) => setForm({ ...form, itemId: Number(e.target.value) })}
              >
                <option value={0} disabled>
                  -- pilih item --
                </option>
                {items.map((i) => (
                  <option key={i.id} value={i.id}>
                    {i.code} - {i.name}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Supplier
              <select
                value={form.supplierId}
                required
                onChange={(e) => setForm({ ...form, supplierId: Number(e.target.value) })}
              >
                <option value={0} disabled>
                  -- pilih supplier --
                </option>
                {suppliers.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.code} - {s.name}
                  </option>
                ))}
              </select>
            </label>

            <label>
              UOM
              <select
                value={form.uomId}
                required
                onChange={(e) => setForm({ ...form, uomId: Number(e.target.value) })}
              >
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
              Price
              <input
                type="number"
                step="0.0001"
                min="0"
                value={form.price}
                onChange={(e) => setForm({ ...form, price: Number(e.target.value) })}
              />
            </label>

            <label>
              Currency
              <input
                value={form.currency}
                maxLength={3}
                required
                onChange={(e) => setForm({ ...form, currency: e.target.value.toUpperCase() })}
              />
            </label>

            <label>
              Min Order Qty
              <input
                type="number"
                step="0.0001"
                min="0"
                value={form.minOrderQty}
                onChange={(e) => setForm({ ...form, minOrderQty: Number(e.target.value) })}
              />
            </label>

            <label>
              Effective From (opsional)
              <input
                type="date"
                value={form.effectiveFrom ?? ''}
                onChange={(e) => setForm({ ...form, effectiveFrom: e.target.value || null })}
              />
            </label>

            <label>
              Effective To (opsional)
              <input
                type="date"
                value={form.effectiveTo ?? ''}
                onChange={(e) => setForm({ ...form, effectiveTo: e.target.value || null })}
              />
            </label>

            <label className="checkbox">
              <input
                type="checkbox"
                checked={form.isPreferred}
                onChange={(e) => setForm({ ...form, isPreferred: e.target.checked })}
              />
              Preferred
            </label>

            <div className="form-actions">
              <button type="button" className="btn" onClick={() => setShowForm(false)}>
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
