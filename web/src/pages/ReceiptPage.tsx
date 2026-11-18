import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { salesOrdersApi } from '../api/salesOrders';
import type { SalesReceiptDto } from '../api/types';
import logo from '../assets/logo.png';

function rupiah(n: number): string {
  return n.toLocaleString('id-ID', { style: 'currency', currency: 'IDR', minimumFractionDigits: 0, maximumFractionDigits: 0 });
}

function qty(n: number): string {
  return n.toLocaleString('id-ID', { maximumFractionDigits: 6 });
}

export function ReceiptPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [receipt, setReceipt] = useState<SalesReceiptDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      try {
        setReceipt(await salesOrdersApi.receipt(Number(id)));
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Gagal memuat struk.');
      }
    })();
  }, [id]);

  if (error) {
    return (
      <div>
        <div className="alert alert-error">{error}</div>
        <button type="button" className="btn no-print" onClick={() => navigate('/sales-orders')}>← Kembali</button>
      </div>
    );
  }

  if (!receipt) return <p className="muted">Memuat...</p>;

  return (
    <div className="receipt-page">
      <div className="page-actions no-print" style={{ marginBottom: 16 }}>
        <button type="button" className="btn" onClick={() => navigate('/sales-orders')}>← Kembali</button>
        <button type="button" className="btn btn-primary" onClick={() => window.print()}>🖨️ Cetak</button>
      </div>

      <div className="receipt-sheet">
        <div className="receipt-header">
          <span className="logo-badge"><img src={logo} alt="Ramuan BunSa" /></span>
          <div className="receipt-header-info">
            <div><strong>No. Struk:</strong> {receipt.orderNo}</div>
            <div><strong>Tanggal:</strong> {receipt.orderDate}</div>
            {receipt.customerName && <div><strong>Customer:</strong> {receipt.customerName}</div>}
            {receipt.salesChannelName && <div><strong>Kanal:</strong> {receipt.salesChannelName}</div>}
          </div>
        </div>

        <table className="receipt-table">
          <thead>
            <tr>
              <th>Produk</th>
              <th className="num">Qty</th>
              <th>Satuan</th>
              <th className="num">Harga</th>
              <th className="num">Subtotal</th>
            </tr>
          </thead>
          <tbody>
            {receipt.lines.map((l) => (
              <tr key={l.id}>
                <td>{l.productCode} - {l.productName}</td>
                <td className="num">{qty(l.qty)}</td>
                <td>{l.packLevelName ?? l.baseUomCode}</td>
                <td className="num">{rupiah(l.unitPrice)}</td>
                <td className="num">{rupiah(l.lineTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <div className="receipt-totals">
          <div><span>Subtotal</span><span>{rupiah(receipt.subtotal)}</span></div>
          <div><span>PPN ({(receipt.taxPercent * 100).toLocaleString('id-ID', { maximumFractionDigits: 2 })}%)</span><span>{rupiah(receipt.taxAmount)}</span></div>
          <div className="receipt-grand-total"><span>Total</span><span>{rupiah(receipt.grandTotal)}</span></div>
        </div>

        <p className="receipt-footer">Terima kasih atas pembelian Anda.</p>
      </div>
    </div>
  );
}
