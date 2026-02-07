import type { InputHTMLAttributes, ReactNode } from 'react';

type InputProps = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  helperText?: string;
  leadingIcon?: ReactNode;
  trailingIcon?: ReactNode;
};

export function Input({
  label,
  helperText,
  leadingIcon,
  trailingIcon,
  className,
  ...props
}: InputProps) {
  const controlClasses = [
    'input-field__control',
    leadingIcon ? 'input-field__control--has-leading' : '',
    trailingIcon ? 'input-field__control--has-trailing' : '',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <label className={['input-field', className].filter(Boolean).join(' ')}>
      <span className="input-field__label">{label}</span>
      <div className="input-field__control-wrap">
        {leadingIcon && <span className="input-field__icon">{leadingIcon}</span>}
        <input className={controlClasses} {...props} />
        {trailingIcon && (
          <span className="input-field__icon input-field__icon--trailing">
            {trailingIcon}
          </span>
        )}
      </div>
      {helperText && <span className="input-field__helper">{helperText}</span>}
    </label>
  );
}
