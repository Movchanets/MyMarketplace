import React from 'react';

interface SuccessAlertProps {
  children: React.ReactNode;
  className?: string;
  onClose?: () => void;
}

/**
 * Semantic success alert component
 * Replaces: bg-green-100 dark:bg-green-900/30 border border-green-400 dark:border-green-700 text-green-700 dark:text-green-300
 * Usage: <SuccessAlert>Operation completed successfully</SuccessAlert>
 */
export const SuccessAlert: React.FC<SuccessAlertProps> = ({ children, className = '', onClose }) => {
  return (
    <div
      className={`bg-success-light dark:bg-success-dark/20 border border-success dark:border-success/50 text-success-text dark:text-success px-4 py-3 rounded flex items-start justify-between gap-3 ${className}`}
    >
      <div>{children}</div>
      {onClose && (
        <button
          onClick={onClose}
          className="flex-shrink-0 text-success-text dark:text-success hover:opacity-75 transition-opacity"
          aria-label="Close"
        >
          ✕
        </button>
      )}
    </div>
  );
};

export default SuccessAlert;
