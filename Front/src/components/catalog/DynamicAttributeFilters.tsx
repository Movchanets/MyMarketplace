import React from 'react';
import type { AttributeFilterDto, AttributeFilterValue } from '../../api/catalogApi';

interface DynamicAttributeFiltersProps {
  availableFilters: AttributeFilterDto[];
  selectedFilters: Record<string, AttributeFilterValue>;
  onFilterChange: (code: string, value: AttributeFilterValue | null) => void;
  onClearAll: () => void;
}

export const DynamicAttributeFilters: React.FC<DynamicAttributeFiltersProps> = ({
  availableFilters,
  selectedFilters,
  onFilterChange,
  onClearAll,
}) => {
  const hasAnyFilters = Object.keys(selectedFilters).length > 0;

  const handleStringFilterChange = (code: string, value: string, checked: boolean) => {
    const currentFilter = selectedFilters[code];
    let newIn = currentFilter?.in ? [...currentFilter.in] : [];

    if (checked) {
      if (!newIn.includes(value)) {
        newIn.push(value);
      }
    } else {
      newIn = newIn.filter((v) => v !== value);
    }

    if (newIn.length > 0) {
      onFilterChange(code, { in: newIn });
    } else {
      onFilterChange(code, null);
    }
  };

  const handleNumberRangeChange = (code: string, type: 'min' | 'max', value: number | null) => {
    const currentFilter = selectedFilters[code] || {};
    
    if (type === 'min') {
      if (value !== null) {
        onFilterChange(code, { ...currentFilter, gte: value });
      } else {
        const { gte, ...rest } = currentFilter;
        onFilterChange(code, Object.keys(rest).length > 0 ? rest : null);
      }
    } else {
      if (value !== null) {
        onFilterChange(code, { ...currentFilter, lte: value });
      } else {
        const { lte, ...rest } = currentFilter;
        onFilterChange(code, Object.keys(rest).length > 0 ? rest : null);
      }
    }
  };

   if (availableFilters.length === 0) {
     return (
       <div className="text-foreground-muted text-sm">
         Немає доступних фільтрів для цієї категорії
       </div>
     );
   }

  return (
    <div className="space-y-6">
      {/* Clear All Button */}
      {hasAnyFilters && (
         <button
           onClick={onClearAll}
           className="w-full py-2 px-4 text-sm font-medium text-info hover:text-info border border-info hover:border-info rounded-lg transition-colors"
         >
           Скинути всі фільтри
         </button>
      )}

      {/* Filter Groups */}
       {availableFilters.map((filter) => (
         <div key={filter.code} className="border-b border-border pb-4">
           <h3 className="font-semibold text-foreground mb-3">
             {filter.name}
             {filter.unit && <span className="text-foreground-muted text-sm ml-1">({filter.unit})</span>}
           </h3>

          {/* String/Boolean Filters - Checkboxes */}
          {filter.availableValues && filter.availableValues.length > 0 && (
            <div className="space-y-2">
              {filter.availableValues.map((option) => {
                const isChecked = selectedFilters[filter.code]?.in?.includes(option.value) || false;
                return (
                   <label
                     key={option.value}
                     className="flex items-center gap-2 cursor-pointer hover:bg-surface-hover p-2 rounded"
                   >
                     <input
                       type="checkbox"
                       checked={isChecked}
                       onChange={(e) => handleStringFilterChange(filter.code, option.value, e.target.checked)}
                       className="w-4 h-4 text-info border-border rounded focus:ring-info"
                     />
                     <span className="text-sm text-foreground flex-1">{option.value}</span>
                     <span className="text-xs text-foreground-muted">({option.count})</span>
                   </label>
                );
              })}
            </div>
          )}

          {/* Number Filters - Range Inputs */}
          {filter.numberRange && (
             <div className="space-y-3">
               <div className="flex items-center gap-2 text-xs text-foreground-muted">
                 <span>Діапазон: {filter.numberRange.min}</span>
                 <span>—</span>
                 <span>{filter.numberRange.max}</span>
                 {filter.unit && <span>{filter.unit}</span>}
               </div>

               <div className="grid grid-cols-2 gap-2">
                 <div>
                   <label className="block text-xs text-foreground-muted mb-1">Від</label>
                   <input
                     type="number"
                     min={filter.numberRange.min}
                     max={filter.numberRange.max}
                     step={filter.numberRange.step || 1}
                     value={selectedFilters[filter.code]?.gte ?? ''}
                     onChange={(e) =>
                       handleNumberRangeChange(
                         filter.code,
                         'min',
                         e.target.value ? Number(e.target.value) : null
                       )
                     }
                     placeholder={String(filter.numberRange.min)}
                     className="w-full px-3 py-2 text-sm border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-info"
                   />
                 </div>

                 <div>
                   <label className="block text-xs text-foreground-muted mb-1">До</label>
                   <input
                     type="number"
                     min={filter.numberRange.min}
                     max={filter.numberRange.max}
                     step={filter.numberRange.step || 1}
                     value={selectedFilters[filter.code]?.lte ?? ''}
                     onChange={(e) =>
                       handleNumberRangeChange(
                         filter.code,
                         'max',
                         e.target.value ? Number(e.target.value) : null
                       )
                     }
                     placeholder={String(filter.numberRange.max)}
                     className="w-full px-3 py-2 text-sm border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-info"
                   />
                 </div>
               </div>
             </div>
          )}
        </div>
      ))}
    </div>
  );
};
