import React from 'react';

interface WarningAlertProps {
  children: React.ReactNode;
  className?: string;
  onClose?: () => void;
}

/**
 * Semantic warning alert component
 * Replaces: bg-yellow-100 dark:bg-yellow-900/30 border border-yellow-400 dark:border-yellow-700 text-yellow-700 dark:text-yellow-300
 * Usage: <WarningAlert>Proceed with caution</WarningAlert>
 */
export const WarningAlert: React.FC<WarningAlertProps> = ({ children, className = '', onClose }) => {
  return (
    <div
      className={`bg-warning-light dark:bg-warning-dark/20 border border-warning dark:border-warning/50 text-warning-text dark:text-warning px-4 py-3 rounded flex items-start justify-between gap-3 ${className}`}
    >
      <div>{children}</div>
      {onClose && (
        <button
          onClick={onClose}
          className="flex-shrink-0 text-warning-text dark:text-warning hover:opacity-75 transition-opacity"
          aria-label="Close"
        >
          ✕
        </button>
      )}
    </div>
  );
};

export default WarningAlert;
