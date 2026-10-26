import { useEffect, useState } from 'react';
import { uomsApi } from '../api/uoms';
import { UOM_TYPES, type UomDto, type UpsertUomDto } from '../api/types';
import { Modal } from '../components/Modal';

const emptyForm: UpsertUomDto = {
  code: '',
  name: '',
  type: 'Weight',
  factorToBase: 1,
  isBase: false,
};

export function UomsPage() {
  const [uoms, setUoms] = useState<UomDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<UpsertUomDto>(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setUoms(await uomsApi.list());
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

  function openEdit(uom: UomDto) {
    setEditingId(uom.id);
    setForm({ code: uom.code, name: uom.name, type: uom.type, factorToBase: uom.factorToBase, isBase: uom.isBase });
    setFormError(null);
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setFormError(null);
    try {
      if (editingId === null) {
        await uomsApi.create(form);
      } else {
        await uomsApi.update(editingId, form);
      }
      setShowForm(false);
      await load();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Gagal menyimpan.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(uom: UomDto) {
    if (!window.confirm(`Hapus UOM "${uom.code}"?`)) return;
    try {
      await uomsApi.remove(uom.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menghapus.');
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1>Satuan Ukur (UOM)</h1>
        <button type="button" className="btn btn-primary" onClick={openCreate}>
          + Tambah UOM
        </button>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {loading ? (
        <p className="muted">Memuat...</p>
      ) : uoms.length === 0 ? (
        <p className="muted">Belum ada UOM.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Code</th>
                <th>Name</th>
                <th>Type</th>
                <th className="num">Factor to Base</th>
                <th>Base?</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {uoms.map((uom) => (
                <tr key={uom.id}>
                  <td>{uom.code}</td>
                  <td>{uom.name}</td>
                  <td>{uom.type}</td>
                  <td className="num">{uom.factorToBase}</td>
                  <td>{uom.isBase ? 'Ya' : 'Tidak'}</td>
                  <td className="actions">
                    <button type="button" className="btn btn-sm" onClick={() => openEdit(uom)}>
                      Edit
                    </button>
                    <button type="button" className="btn btn-sm btn-danger" onClick={() => handleDelete(uom)}>
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
        <Modal title={editingId === null ? 'Tambah UOM' : 'Edit UOM'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit}>
            {formError && <div className="alert alert-error">{formError}</div>}

            <label>
              Code
              <input
                value={form.code}
                maxLength={10}
                required
                onChange={(e) => setForm({ ...form, code: e.target.value })}
              />
            </label>

            <label>
              Name
              <input
                value={form.name}
                maxLength={50}
                required
                onChange={(e) => setForm({ ...form, name: e.target.value })}
              />
            </label>

            <label>
              Type
              <select value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value as UomDto['type'] })}>
                {UOM_TYPES.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Factor to Base
              <input
                type="number"
                step="0.000001"
                min="0"
                value={form.factorToBase}
                onChange={(e) => setForm({ ...form, factorToBase: Number(e.target.value) })}
              />
            </label>

            <label className="checkbox">
              <input
                type="checkbox"
                checked={form.isBase}
                onChange={(e) => setForm({ ...form, isBase: e.target.checked })}
              />
              Satuan dasar (base)
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
