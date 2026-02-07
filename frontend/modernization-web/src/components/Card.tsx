import type { ReactNode } from 'react';

type CardProps = {
  title?: string;
  meta?: string;
  children: ReactNode;
  className?: string;
};

export function Card({ title, meta, children, className }: CardProps) {
  return (
    <div className={['card', className].filter(Boolean).join(' ')}>
      {(title || meta) && (
        <div className="card__header">
          {title && <h3 className="card__title">{title}</h3>}
          {meta && <span className="card__meta">{meta}</span>}
        </div>
      )}
      {children}
    </div>
  );
}
