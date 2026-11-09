import type { ReactNode } from 'react';

interface PageHeaderProps {
  title: string;
  description?: string;
  actions?: ReactNode;
}

/** Judul halaman + 1 kalimat penjelasan singkat (biar jelas fungsi halaman tanpa harus menebak). */
export function PageHeader({ title, description, actions }: PageHeaderProps) {
  return (
    <div className="page-header">
      <div>
        <h1>{title}</h1>
        {description && <p className="page-desc">{description}</p>}
      </div>
      {actions && <div className="page-actions">{actions}</div>}
    </div>
  );
}
