import { useEffect, useState } from 'react';
import { pricingSettingsApi } from '../api/pricingSettings';
import { ROUNDING_RULES, type RoundingRule, type UpsertPricingSettingDto } from '../api/types';

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
      <div className="page-header">
        <h1>Setelan Harga</h1>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {notice && <div className="alert alert-info">{notice}</div>}
      {loading ? (
        <p className="muted">Memuat...</p>
      ) : (
        <div className="table-wrap" style={{ padding: 20, maxWidth: 460 }}>
          <form onSubmit={handleSubmit}>
            <label>
              Target margin default (%)
              <input
                type="number"
                step="any"
                value={form.targetPct}
                onChange={(e) => setForm({ ...form, targetPct: e.target.value })}
              />
            </label>
            <label>
              PPN default (%)
              <input
                type="number"
                step="any"
                value={form.taxPct}
                onChange={(e) => setForm({ ...form, taxPct: e.target.value })}
              />
            </label>
            <label>
              Margin minimum (%)
              <input
                type="number"
                step="any"
                value={form.minMarginPct}
                onChange={(e) => setForm({ ...form, minMarginPct: e.target.value })}
              />
            </label>
            <label>
              Pembulatan
              <select
                value={form.roundingRule}
                onChange={(e) => setForm({ ...form, roundingRule: e.target.value as RoundingRule })}
              >
                {ROUNDING_RULES.map((r) => (
                  <option key={r} value={r}>
                    {r}
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
