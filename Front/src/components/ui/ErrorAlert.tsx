import React from 'react';

interface ErrorAlertProps {
  children: React.ReactNode;
  className?: string;
  onClose?: () => void;
}

/**
 * Semantic error alert component
 * Replaces: bg-red-100 dark:bg-red-900/30 border border-red-400 dark:border-red-700 text-red-700 dark:text-red-300
 * Usage: <ErrorAlert>Something went wrong</ErrorAlert>
 */
export const ErrorAlert: React.FC<ErrorAlertProps> = ({ children, className = '', onClose }) => {
  return (
    <div
      className={`bg-error-light dark:bg-error-dark/20 border border-error dark:border-error/50 text-error-text dark:text-error px-4 py-3 rounded flex items-start justify-between gap-3 ${className}`}
    >
      <div>{children}</div>
      {onClose && (
        <button
          onClick={onClose}
          className="flex-shrink-0 text-error-text dark:text-error hover:opacity-75 transition-opacity"
          aria-label="Close"
        >
          ✕
        </button>
      )}
    </div>
  );
};

export default ErrorAlert;
