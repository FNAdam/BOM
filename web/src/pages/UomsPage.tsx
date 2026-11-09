import { useEffect, useState } from 'react';
import { uomsApi } from '../api/uoms';
import { UOM_TYPES, type UomDto, type UpsertUomDto } from '../api/types';
import { Modal } from '../components/Modal';
import { PageHeader } from '../components/PageHeader';
import { uomTypeLabel } from '../labels';

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
      <PageHeader
        title="Satuan Ukur"
        description="Daftar satuan (kg, g, L, ml, pcs, dst) beserta faktor konversinya ke satuan dasar per jenis."
        actions={
          <button type="button" className="btn btn-primary" onClick={openCreate}>
            + Tambah Satuan
          </button>
        }
      />

      {error && <div className="alert alert-error">{error}</div>}
      {loading ? (
        <p className="muted">Memuat...</p>
      ) : uoms.length === 0 ? (
        <p className="muted">Belum ada satuan ukur.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Kode</th>
                <th>Nama</th>
                <th>Jenis</th>
                <th className="num">Faktor ke Satuan Dasar</th>
                <th>Satuan Dasar?</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {uoms.map((uom) => (
                <tr key={uom.id}>
                  <td>{uom.code}</td>
                  <td>{uom.name}</td>
                  <td>{uomTypeLabel(uom.type)}</td>
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
              Kode
              <input
                value={form.code}
                maxLength={10}
                required
                placeholder="mis. kg, g, L, ml, pcs"
                onChange={(e) => setForm({ ...form, code: e.target.value })}
              />
            </label>

            <label>
              Nama
              <input
                value={form.name}
                maxLength={50}
                required
                placeholder="mis. Kilogram"
                onChange={(e) => setForm({ ...form, name: e.target.value })}
              />
            </label>

            <label>
              Jenis
              <select value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value as UomDto['type'] })}>
                {UOM_TYPES.map((t) => (
                  <option key={t} value={t}>
                    {uomTypeLabel(t)}
                  </option>
                ))}
              </select>
              <span className="field-hint">
                Konversi cuma bisa antar satuan dengan jenis yang sama (Berat ke Berat, Volume ke Volume, dst).
              </span>
            </label>

            <label>
              Faktor ke Satuan Dasar
              <input
                type="number"
                step="0.000001"
                min="0"
                value={form.factorToBase}
                onChange={(e) => setForm({ ...form, factorToBase: Number(e.target.value) })}
              />
              <span className="field-hint">
                Berapa satuan dasar yang sama dengan 1 satuan ini. Contoh: 1 gram = 0,001 kg -&gt; isi 0,001.
              </span>
            </label>

            <label className="checkbox">
              <input
                type="checkbox"
                checked={form.isBase}
                onChange={(e) => setForm({ ...form, isBase: e.target.checked })}
              />
              Ini satuan dasar untuk jenisnya (faktor otomatis 1)
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
