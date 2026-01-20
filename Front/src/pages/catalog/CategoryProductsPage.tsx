import React, { useEffect, useState } from 'react';
import { useParams, useSearchParams } from 'react-router-dom';
import {
  categoriesApi,
  productsApi,
  type CategoryDto,
  type CategoryAvailableFiltersDto,
  type ProductSummaryDto,
  type ProductFilterRequest,
  type AttributeFilterValue,
  ProductSort,
} from '../../api/catalogApi';
import ProductCard from '../../components/catalog/ProductCard';
import { DynamicAttributeFilters } from '../../components/catalog/DynamicAttributeFilters';

export const CategoryProductsPage: React.FC = () => {
  const { slug } = useParams<{ slug: string }>();
  const [, setSearchParams] = useSearchParams();

  const [category, setCategory] = useState<CategoryDto | null>(null);
  const [availableFilters, setAvailableFilters] = useState<CategoryAvailableFiltersDto | null>(null);
  const [products, setProducts] = useState<ProductSummaryDto[]>([]);
  const [selectedFilters, setSelectedFilters] = useState<Record<string, AttributeFilterValue>>({});
  const [priceRange, setPriceRange] = useState<{ min: number | null; max: number | null }>({
    min: null,
    max: null,
  });
  const [inStockOnly, setInStockOnly] = useState(false);
  const [sort, setSort] = useState<ProductSort>(ProductSort.Newest);
  const [page, setPage] = useState(1);
  const [totalProducts, setTotalProducts] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const pageSize = 24;

  // Load category and available filters
  useEffect(() => {
    if (!slug) return;

    const loadCategoryAndFilters = async () => {
      try {
        setLoading(true);
        setError(null);

        const categoryResponse = await categoriesApi.getBySlug(slug);
        if (!categoryResponse.isSuccess || !categoryResponse.payload) {
          setError('Категорію не знайдено');
          return;
        }

        setCategory(categoryResponse.payload);

        const filtersResponse = await categoriesApi.getAvailableFilters(categoryResponse.payload.id);
        if (filtersResponse.isSuccess && filtersResponse.payload) {
          setAvailableFilters(filtersResponse.payload);
        }
      } catch (err) {
        setError('Помилка завантаження категорії');
        console.error(err);
      } finally {
        setLoading(false);
      }
    };

    loadCategoryAndFilters();
  }, [slug]);

  // Load products when filters change
  useEffect(() => {
    if (!category) return;

    const loadProducts = async () => {
      try {
        const filterRequest: ProductFilterRequest = {
          categoryId: category.id,
          minPrice: priceRange.min,
          maxPrice: priceRange.max,
          inStock: inStockOnly || null,
          attributes: Object.keys(selectedFilters).length > 0 ? selectedFilters : null,
          sort,
          page,
          pageSize,
        };

        const response = await productsApi.filter(filterRequest);
        
        if (response.isSuccess && response.payload) {
          setProducts(response.payload.items);
          setTotalProducts(response.payload.total);
        } else {
          setProducts([]);
          setTotalProducts(0);
        }
      } catch (err) {
        console.error('Помилка фільтрації товарів:', err);
        setProducts([]);
      }
    };

    loadProducts();
  }, [category, selectedFilters, priceRange, inStockOnly, sort, page]);

  // Update URL params when filters change
  useEffect(() => {
    const params = new URLSearchParams();

    if (priceRange.min) params.set('minPrice', String(priceRange.min));
    if (priceRange.max) params.set('maxPrice', String(priceRange.max));
    if (inStockOnly) params.set('inStock', 'true');
    if (sort !== ProductSort.Newest) params.set('sort', sort);
    if (page > 1) params.set('page', String(page));

    Object.entries(selectedFilters).forEach(([code, filter]) => {
      if (filter.in) params.set(`attr_${code}_in`, filter.in.join(','));
      if (filter.gte) params.set(`attr_${code}_gte`, String(filter.gte));
      if (filter.lte) params.set(`attr_${code}_lte`, String(filter.lte));
    });

    setSearchParams(params, { replace: true });
  }, [selectedFilters, priceRange, inStockOnly, sort, page]);

  const handleFilterChange = (code: string, value: AttributeFilterValue | null) => {
    setSelectedFilters((prev) => {
      const newFilters = { ...prev };
      if (value === null) {
        delete newFilters[code];
      } else {
        newFilters[code] = value;
      }
      return newFilters;
    });
    setPage(1); // Reset to first page
  };

  const handleClearAllFilters = () => {
    setSelectedFilters({});
    setPriceRange({ min: null, max: null });
    setInStockOnly(false);
    setPage(1);
  };

  const totalPages = Math.ceil(totalProducts / pageSize);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-lg text-text">Завантаження...</div>
      </div>
    );
  }

  if (error || !category) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-lg text-red-500 dark:text-red-400">{error || 'Категорію не знайдено'}</div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
       {/* Header */}
       <div className="mb-8">
         <h1 className="text-3xl font-bold text-text mb-2">
           {category.emoji && <span className="mr-2">{category.emoji}</span>}
           {category.name}
         </h1>
         {category.description && (
           <p className="text-text-muted">{category.description}</p>
         )}
         <p className="text-sm text-text-muted mt-2">
           Знайдено товарів: {totalProducts}
         </p>
       </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Filters Sidebar */}
         <aside className="lg:col-span-1">
           <div className="sticky top-4 bg-surface-card rounded-lg shadow p-6">
             <h2 className="text-xl font-semibold mb-4">Фільтри</h2>

             {/* Price Range */}
             {availableFilters?.priceRange && (
               <div className="mb-6 border-b border-surface pb-4">
                 <h3 className="font-semibold text-text mb-3">Ціна (грн)</h3>
                 <div className="grid grid-cols-2 gap-2">
                   <input
                     type="number"
                     placeholder={`Від ${availableFilters.priceRange.min}`}
                     value={priceRange.min ?? ''}
                     onChange={(e) =>
                       setPriceRange((prev) => ({
                         ...prev,
                         min: e.target.value ? Number(e.target.value) : null,
                       }))
                     }
                     className="px-3 py-2 text-sm border border-surface bg-surface-card text-text rounded-md focus:outline-none focus:ring-2 focus:ring-brand/50"
                   />
                   <input
                     type="number"
                     placeholder={`До ${availableFilters.priceRange.max}`}
                     value={priceRange.max ?? ''}
                     onChange={(e) =>
                       setPriceRange((prev) => ({
                         ...prev,
                         max: e.target.value ? Number(e.target.value) : null,
                       }))
                     }
                     className="px-3 py-2 text-sm border border-surface bg-surface-card text-text rounded-md focus:outline-none focus:ring-2 focus:ring-brand/50"
                   />
                 </div>
               </div>
             )}

             {/* In Stock */}
             <div className="mb-6 border-b border-surface pb-4">
               <label className="flex items-center gap-2 cursor-pointer">
                 <input
                   type="checkbox"
                   checked={inStockOnly}
                   onChange={(e) => setInStockOnly(e.target.checked)}
                   className="w-4 h-4 text-brand border-surface rounded focus:ring-brand/50"
                 />
                 <span className="text-sm text-text">Тільки в наявності</span>
               </label>
             </div>

            {/* Dynamic Attribute Filters */}
            {availableFilters && (
              <DynamicAttributeFilters
                availableFilters={availableFilters.attributes}
                selectedFilters={selectedFilters}
                onFilterChange={handleFilterChange}
                onClearAll={handleClearAllFilters}
              />
            )}
          </div>
        </aside>

        {/* Products Grid */}
        <main className="lg:col-span-3">
          {/* Sorting */}
          <div className="flex items-center justify-between mb-6">
            <div className="text-sm text-text-muted">
              Сторінка {page} з {totalPages}
            </div>
            <select
              value={sort}
              onChange={(e) => setSort(e.target.value as ProductSort)}
              className="px-4 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value={ProductSort.Newest}>Новинки</option>
              <option value={ProductSort.PriceAsc}>Ціна: зростання</option>
              <option value={ProductSort.PriceDesc}>Ціна: спадання</option>
            </select>
          </div>

          {/* Products */}
          {products.length > 0 ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              {products.map((product) => (
                <ProductCard key={product.id} product={product} />
              ))}
            </div>
          ) : (
            <div className="text-center py-12 text-gray-500">
              Товари не знайдено. Спробуйте змінити фільтри.
            </div>
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex justify-center items-center gap-2 mt-8">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-4 py-2 border border-gray-300 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
              >
                Попередня
              </button>
              
              <span className="px-4 py-2 text-sm text-gray-700">
                {page} / {totalPages}
              </span>

              <button
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
                className="px-4 py-2 border border-gray-300 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
              >
                Наступна
              </button>
            </div>
          )}
        </main>
      </div>
    </div>
  );
};
