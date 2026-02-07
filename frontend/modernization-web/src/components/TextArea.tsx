import type { TextareaHTMLAttributes } from 'react';

type TextAreaProps = TextareaHTMLAttributes<HTMLTextAreaElement> & {
  label: string;
  helperText?: string;
};

export function TextArea({ label, helperText, className, ...props }: TextAreaProps) {
  return (
    <label className={['input-field', className].filter(Boolean).join(' ')}>
      <span className="input-field__label">{label}</span>
      <textarea className="input-field__textarea" rows={4} {...props} />
      {helperText && <span className="input-field__helper">{helperText}</span>}
    </label>
  );
}
