import type { InputHTMLAttributes } from 'react';

type InputProps = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  helperText?: string;
};

export function Input({ label, helperText, className, ...props }: InputProps) {
  return (
    <label className={['input-field', className].filter(Boolean).join(' ')}>
      <span className="input-field__label">{label}</span>
      <input className="input-field__control" {...props} />
      {helperText && <span className="input-field__helper">{helperText}</span>}
    </label>
  );
}
