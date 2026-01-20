import React from 'react';

interface ErrorTextProps {
  children: React.ReactNode;
  className?: string;
}

/**
 * Semantic error text component
 * Replaces: text-red-500, text-red-600, text-sm text-red-XXX
 * Usage: <ErrorText>Your error message</ErrorText>
 */
export const ErrorText: React.FC<ErrorTextProps> = ({ children, className = '' }) => {
  return (
    <div className={`text-sm text-error ${className}`}>
      {children}
    </div>
  );
};

export default ErrorText;
