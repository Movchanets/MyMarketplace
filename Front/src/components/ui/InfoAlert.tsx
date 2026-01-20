import React from 'react';

interface InfoAlertProps {
  children: React.ReactNode;
  className?: string;
  onClose?: () => void;
}

/**
 * Semantic info alert component
 * Replaces: bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 text-blue-700 dark:text-blue-300
 * Usage: <InfoAlert>This is informational</InfoAlert>
 */
export const InfoAlert: React.FC<InfoAlertProps> = ({ children, className = '', onClose }) => {
  return (
    <div
      className={`bg-info-light dark:bg-info-dark/20 border border-info dark:border-info/50 text-info-text dark:text-info px-4 py-3 rounded flex items-start justify-between gap-3 ${className}`}
    >
      <div>{children}</div>
      {onClose && (
        <button
          onClick={onClose}
          className="flex-shrink-0 text-info-text dark:text-info hover:opacity-75 transition-opacity"
          aria-label="Close"
        >
          ✕
        </button>
      )}
    </div>
  );
};

export default InfoAlert;
