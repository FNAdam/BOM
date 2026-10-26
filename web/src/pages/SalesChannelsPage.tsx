import { useEffect, useState } from 'react';
import { salesChannelsApi } from '../api/salesChannels';
import type { SalesChannelDto, UpsertSalesChannelDto } from '../api/types';
import { Modal } from '../components/Modal';

interface ChannelForm {
  code: string;
  name: string;
  marginPct: string; // persen (mis. "12")
  sequence: number;
  isActive: boolean;
}

const emptyForm: ChannelForm = { code: '', name: '', marginPct: '', sequence: 0, isActive: true };

export function SalesChannelsPage() {
  const [channels, setChannels] = useState<SalesChannelDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<ChannelForm>(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setChannels(await salesChannelsApi.list());
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

  function openEdit(c: SalesChannelDto) {
    setEditingId(c.id);
    setForm({
      code: c.code,
      name: c.name,
      marginPct: String(c.defaultMarginPercent * 100),
      sequence: c.sequence,
      isActive: c.isActive,
    });
    setFormError(null);
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setFormError(null);
    const dto: UpsertSalesChannelDto = {
      code: form.code,
      name: form.name,
      defaultMarginPercent: form.marginPct.trim() === '' ? 0 : Number(form.marginPct) / 100,
      sequence: form.sequence,
      isActive: form.isActive,
    };
    try {
      if (editingId === null) await salesChannelsApi.create(dto);
      else await salesChannelsApi.update(editingId, dto);
      setShowForm(false);
      await load();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Gagal menyimpan.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(c: SalesChannelDto) {
    if (!window.confirm(`Hapus kanal "${c.code} - ${c.name}"?`)) return;
    try {
      await salesChannelsApi.remove(c.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menghapus.');
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1>Kanal Penjualan</h1>
        <button type="button" className="btn btn-primary" onClick={openCreate}>
          + Tambah Kanal
        </button>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {loading ? (
        <p className="muted">Memuat...</p>
      ) : channels.length === 0 ? (
        <p className="muted">Belum ada kanal.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th className="num">Urutan</th>
                <th>Code</th>
                <th>Name</th>
                <th className="num">Margin default</th>
                <th>Active</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {channels.map((c) => (
                <tr key={c.id}>
                  <td className="num">{c.sequence}</td>
                  <td>{c.code}</td>
                  <td>{c.name}</td>
                  <td className="num">{(c.defaultMarginPercent * 100).toLocaleString('id-ID', { maximumFractionDigits: 2 })}%</td>
                  <td>{c.isActive ? 'Ya' : 'Tidak'}</td>
                  <td className="actions">
                    <button type="button" className="btn btn-sm" onClick={() => openEdit(c)}>
                      Edit
                    </button>
                    <button type="button" className="btn btn-sm btn-danger" onClick={() => handleDelete(c)}>
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
        <Modal title={editingId === null ? 'Tambah Kanal' : 'Edit Kanal'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit}>
            {formError && <div className="alert alert-error">{formError}</div>}

            <label>
              Code
              <input value={form.code} maxLength={20} required onChange={(e) => setForm({ ...form, code: e.target.value })} />
            </label>
            <label>
              Name
              <input value={form.name} maxLength={60} required onChange={(e) => setForm({ ...form, name: e.target.value })} />
            </label>
            <label>
              Margin default (%)
              <input
                type="number"
                step="any"
                min="0"
                value={form.marginPct}
                onChange={(e) => setForm({ ...form, marginPct: e.target.value })}
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
              <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />
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
