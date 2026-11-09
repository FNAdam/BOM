import { useEffect, useState } from 'react';
import { pricingSettingsApi } from '../api/pricingSettings';
import { ROUNDING_RULES, type RoundingRule, type UpsertPricingSettingDto } from '../api/types';
import { PageHeader } from '../components/PageHeader';
import { roundingRuleLabel } from '../labels';

interface SettingForm {
  targetPct: string;
  taxPct: string;
  minMarginPct: string;
  roundingRule: RoundingRule;
}

export function PricingSettingsPage() {
  const [form, setForm] = useState<SettingForm>({
    targetPct: '',
    taxPct: '',
    minMarginPct: '',
    roundingRule: 'None',
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const s = await pricingSettingsApi.get();
      setForm({
        targetPct: String(s.defaultTargetMarginPercent * 100),
        taxPct: String(s.defaultTaxPercent * 100),
        minMarginPct: String(s.minMarginPercent * 100),
        roundingRule: s.roundingRule,
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal memuat setelan.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError(null);
    setNotice(null);
    const dto: UpsertPricingSettingDto = {
      defaultTargetMarginPercent: Number(form.targetPct || 0) / 100,
      defaultTaxPercent: Number(form.taxPct || 0) / 100,
      minMarginPercent: Number(form.minMarginPct || 0) / 100,
      roundingRule: form.roundingRule,
    };
    try {
      await pricingSettingsApi.update(dto);
      setNotice('Setelan tersimpan.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gagal menyimpan setelan.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <PageHeader
        title="Setelan Harga"
        description="Nilai default yang dipakai saat menetapkan harga jual produk baru - bisa di-override per produk di halaman Tetapkan Harga Jual."
      />

      {error && <div className="alert alert-error">{error}</div>}
      {notice && <div className="alert alert-info">{notice}</div>}
      {loading ? (
        <p className="muted">Memuat...</p>
      ) : (
        <div className="table-wrap" style={{ padding: 20, maxWidth: 460 }}>
          <form onSubmit={handleSubmit}>
            <label>
              Target Margin Default (%)
              <input
                type="number"
                step="any"
                value={form.targetPct}
                onChange={(e) => setForm({ ...form, targetPct: e.target.value })}
              />
            </label>
            <label>
              PPN Default (%)
              <input
                type="number"
                step="any"
                value={form.taxPct}
                onChange={(e) => setForm({ ...form, taxPct: e.target.value })}
              />
            </label>
            <label>
              Margin Minimum (%)
              <input
                type="number"
                step="any"
                value={form.minMarginPct}
                onChange={(e) => setForm({ ...form, minMarginPct: e.target.value })}
              />
              <span className="field-hint">
                Kalau margin produk di bawah angka ini, akan ditandai "di bawah minimum" di Dashboard.
              </span>
            </label>
            <label>
              Pembulatan Harga
              <select
                value={form.roundingRule}
                onChange={(e) => setForm({ ...form, roundingRule: e.target.value as RoundingRule })}
              >
                {ROUNDING_RULES.map((r) => (
                  <option key={r} value={r}>
                    {roundingRuleLabel(r)}
                  </option>
                ))}
              </select>
            </label>
            <div className="form-actions">
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Menyimpan...' : 'Simpan'}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
