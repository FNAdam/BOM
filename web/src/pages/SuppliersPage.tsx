import { useEffect, useState } from 'react';
import { suppliersApi } from '../api/suppliers';
import type { SupplierDto, UpsertSupplierDto } from '../api/types';
import { Modal } from '../components/Modal';

const emptyForm: UpsertSupplierDto = {
  code: '',
  name: '',
  leadTimeDays: null,
  isActive: true,
};

export function SuppliersPage() {
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<UpsertSupplierDto>(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setSuppliers(await suppliersApi.list());
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
    setEditingId(null);
    setForm(emptyForm);
    setFormError(null);
    setShowForm(true);
  }

  function openEdit(supplier: SupplierDto) {
    setEditingId(supplier.id);
    setForm({
      code: supplier.code,
      name: supplier.name,
      leadTimeDays: supplier.leadTimeDays,
      isActive: supplier.isActive,
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
        await suppliersApi.create(form);
      } else {
        await suppliersApi.update(editingId, form);
      }
      setShowForm(false);
      await load();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Gagal menyimpan.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(supplier: SupplierDto) {
    if (!window.confirm(`Hapus supplier "${supplier.code} - ${supplier.name}"?`)) return;
    try {
      await suppliersApi.remove(supplier.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menghapus.');
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1>Suppliers</h1>
        <button type="button" className="btn btn-primary" onClick={openCreate}>
          + Tambah Supplier
        </button>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {loading ? (
        <p className="muted">Memuat...</p>
      ) : suppliers.length === 0 ? (
        <p className="muted">Belum ada supplier.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Code</th>
                <th>Name</th>
                <th className="num">Lead Time (hari)</th>
                <th>Active</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {suppliers.map((supplier) => (
                <tr key={supplier.id}>
                  <td>{supplier.code}</td>
                  <td>{supplier.name}</td>
                  <td className="num">{supplier.leadTimeDays ?? '-'}</td>
                  <td>{supplier.isActive ? 'Ya' : 'Tidak'}</td>
                  <td className="actions">
                    <button type="button" className="btn btn-sm" onClick={() => openEdit(supplier)}>
                      Edit
                    </button>
                    <button type="button" className="btn btn-sm btn-danger" onClick={() => handleDelete(supplier)}>
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
        <Modal title={editingId === null ? 'Tambah Supplier' : 'Edit Supplier'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit}>
            {formError && <div className="alert alert-error">{formError}</div>}

            <label>
              Code
              <input
                value={form.code}
                maxLength={20}
                required
                onChange={(e) => setForm({ ...form, code: e.target.value })}
              />
            </label>

            <label>
              Name
              <input
                value={form.name}
                maxLength={150}
                required
                onChange={(e) => setForm({ ...form, name: e.target.value })}
              />
            </label>

            <label>
              Lead Time (hari, opsional)
              <input
                type="number"
                min="0"
                value={form.leadTimeDays ?? ''}
                onChange={(e) => setForm({ ...form, leadTimeDays: e.target.value ? Number(e.target.value) : null })}
              />
            </label>

            <label className="checkbox">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              />
              Active
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
