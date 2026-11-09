import { useEffect, useState } from 'react';
import { itemsApi } from '../api/items';
import { uomsApi } from '../api/uoms';
import { itemCategoriesApi } from '../api/itemCategories';
import {
  COSTING_METHODS,
  ITEM_TYPES,
  type ItemDto,
  type ItemCategoryDto,
  type UomDto,
  type UpsertItemDto,
} from '../api/types';
import { Modal } from '../components/Modal';
import { PageHeader } from '../components/PageHeader';
import { costingMethodLabel, itemTypeLabel } from '../labels';

const emptyForm: UpsertItemDto = {
  code: '',
  name: '',
  type: 'RawMaterial',
  baseUomId: 0,
  categoryId: null,
  standardMaterialCost: 0,
  costingMethod: 'Standard',
  density: null,
  isActive: true,
};

export function ItemsPage() {
  const [items, setItems] = useState<ItemDto[]>([]);
  const [uoms, setUoms] = useState<UomDto[]>([]);
  const [categories, setCategories] = useState<ItemCategoryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<UpsertItemDto>(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [generatingCode, setGeneratingCode] = useState(false);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [itemsData, uomsData, categoriesData] = await Promise.all([
        itemsApi.list(),
        uomsApi.list(),
        itemCategoriesApi.list(),
      ]);
      setItems(itemsData);
      setUoms(uomsData);
      setCategories(categoriesData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal memuat data.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  // Ambil saran kode berikutnya dari server untuk tipe tertentu (format "PREFIX-0000").
  // Kegagalan diabaikan: auto-generate hanya kemudahan, tidak boleh memblokir pengisian form.
  async function suggestCode(type: UpsertItemDto['type']) {
    setGeneratingCode(true);
    try {
      const { code } = await itemsApi.nextCode(type);
      setForm((f) => ({ ...f, code }));
    } catch {
      // diamkan — user tetap bisa mengisi kode manual
    } finally {
      setGeneratingCode(false);
    }
  }

  function openCreate() {
    setEditingId(null);
    setForm({ ...emptyForm, baseUomId: uoms[0]?.id ?? 0 });
    setFormError(null);
    setShowForm(true);
    void suggestCode(emptyForm.type);
  }

  function handleTypeChange(type: UpsertItemDto['type']) {
    setForm((f) => ({ ...f, type }));
    // Auto-generate kode hanya saat membuat item baru; saat edit, kode existing tidak diubah otomatis.
    if (editingId === null) void suggestCode(type);
  }

  function openEdit(item: ItemDto) {
    setEditingId(item.id);
    setForm({
      code: item.code,
      name: item.name,
      type: item.type,
      baseUomId: item.baseUomId,
      categoryId: item.categoryId,
      standardMaterialCost: item.standardMaterialCost,
      costingMethod: item.costingMethod,
      density: item.density,
      isActive: item.isActive,
    });
    setFormError(null);
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setFormError(null);
    try {
      if (editingId === null) {
        await itemsApi.create(form);
      } else {
        await itemsApi.update(editingId, form);
      }
      setShowForm(false);
      await load();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Gagal menyimpan.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(item: ItemDto) {
    if (!window.confirm(`Hapus item "${item.code} - ${item.name}"?`)) return;
    try {
      await itemsApi.remove(item.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menghapus.');
    }
  }

  return (
    <div>
      <PageHeader
        title="Bahan & Produk"
        description="Daftar induk semua bahan baku, kemasan, produk setengah jadi, dan produk jadi yang dipakai di resep."
        actions={
          <button type="button" className="btn btn-primary" onClick={openCreate}>
            + Tambah Item
          </button>
        }
      />

      {error && <div className="alert alert-error">{error}</div>}
      {loading ? (
        <p className="muted">Memuat...</p>
      ) : items.length === 0 ? (
        <p className="muted">Belum ada item. Klik "+ Tambah Item" untuk mulai.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Kode</th>
                <th>Nama</th>
                <th>Tipe</th>
                <th>Satuan Dasar</th>
                <th>Kategori</th>
                <th className="num">Biaya Standar</th>
                <th>Metode Biaya</th>
                <th>Aktif</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id}>
                  <td>{item.code}</td>
                  <td>{item.name}</td>
                  <td>{itemTypeLabel(item.type)}</td>
                  <td>{item.baseUomCode}</td>
                  <td>{item.categoryName ?? '-'}</td>
                  <td className="num">{item.standardMaterialCost.toLocaleString('id-ID')}</td>
                  <td>{costingMethodLabel(item.costingMethod)}</td>
                  <td>{item.isActive ? 'Ya' : 'Tidak'}</td>
                  <td className="actions">
                    <button type="button" className="btn btn-sm" onClick={() => openEdit(item)}>
                      Edit
                    </button>
                    <button type="button" className="btn btn-sm btn-danger" onClick={() => handleDelete(item)}>
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
        <Modal title={editingId === null ? 'Tambah Item' : 'Edit Item'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit}>
            {formError && <div className="alert alert-error">{formError}</div>}

            <label>
              Kode
              <div className="input-with-action">
                <input
                  value={form.code}
                  maxLength={30}
                  required
                  placeholder="mis. RM-0001"
                  onChange={(e) => setForm({ ...form, code: e.target.value })}
                />
                {editingId === null && (
                  <button
                    type="button"
                    className="btn btn-sm"
                    disabled={generatingCode}
                    onClick={() => void suggestCode(form.type)}
                    title="Buat ulang kode otomatis berdasarkan tipe (RW/PK/SF/FG-0000)"
                  >
                    {generatingCode ? '...' : 'Auto'}
                  </button>
                )}
              </div>
            </label>

            <label>
              Nama
              <input
                value={form.name}
                maxLength={150}
                required
                placeholder="mis. Kopi Bubuk Robusta"
                onChange={(e) => setForm({ ...form, name: e.target.value })}
              />
            </label>

            <label>
              Tipe
              <select value={form.type} onChange={(e) => handleTypeChange(e.target.value as ItemDto['type'])}>
                {ITEM_TYPES.map((t) => (
                  <option key={t} value={t}>
                    {itemTypeLabel(t)}
                  </option>
                ))}
              </select>
              <span className="field-hint">
                Bahan Baku/Kemasan = dibeli langsung. Setengah Jadi/Produk Jadi = dibuat lewat resep (BOM).
              </span>
            </label>

            <label>
              Satuan Dasar
              <select
                value={form.baseUomId}
                required
                onChange={(e) => setForm({ ...form, baseUomId: Number(e.target.value) })}
              >
                <option value={0} disabled>
                  -- pilih --
                </option>
                {uoms.map((u) => (
                  <option key={u.id} value={u.id}>
                    {u.code} - {u.name}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Kategori (opsional)
              <select
                value={form.categoryId ?? ''}
                onChange={(e) => setForm({ ...form, categoryId: e.target.value ? Number(e.target.value) : null })}
              >
                <option value="">-- tanpa kategori --</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Biaya Standar / {uoms.find((u) => u.id === form.baseUomId)?.code ?? 'satuan'}
              <input
                type="number"
                step="0.0001"
                min="0"
                value={form.standardMaterialCost}
                onChange={(e) => setForm({ ...form, standardMaterialCost: Number(e.target.value) })}
              />
              <span className="field-hint">
                Untuk item dibeli: harga per satuan kalau belum ada harga supplier. Untuk item ber-resep, biaya
                dihitung otomatis - angka ini diabaikan.
              </span>
            </label>

            <label>
              Metode Biaya
              <select
                value={form.costingMethod}
                onChange={(e) => setForm({ ...form, costingMethod: e.target.value as ItemDto['costingMethod'] })}
              >
                {COSTING_METHODS.map((m) => (
                  <option key={m} value={m}>
                    {costingMethodLabel(m)}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Massa Jenis / Density (opsional)
              <input
                type="number"
                step="0.000001"
                placeholder="mis. 1.03 (kg per liter)"
                value={form.density ?? ''}
                onChange={(e) => setForm({ ...form, density: e.target.value ? Number(e.target.value) : null })}
              />
              <span className="field-hint">
                Isi hanya kalau bahan ini dipakai di resep dengan satuan beda dimensi (mis. ditimbang dalam kg
                tapi dipakai di resep dalam liter).
              </span>
            </label>

            <label className="checkbox">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              />
              Aktif (muncul di pilihan resep/transaksi baru)
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
