import { Routes, Route } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Layout } from './components/layout/Layout'
import { ProtectedRoute } from './components/ProtectedRoute'
import { ThemeProvider } from './contexts/ThemeContext'
import Home from './pages/home/Home'
import About from './pages/info/About'
import Contacts from './pages/info/Contacts'
import Auth from './pages/auth/Auth'
import ResetPassword from './pages/reset/ResetPassword'
import SettingsPage from './pages/Cabinet/SettingsPage'
import Cabinet from './pages/Cabinet/Cabinet'
import Favorites from './pages/Cabinet/Favorites'
import AdminPanel from './pages/admin/AdminPanel'
import CategoriesManagement from './pages/admin/CategoriesManagement'
import TagsManagement from './pages/admin/TagsManagement'
import AttributeDefinitionsManagement from './pages/admin/AttributeDefinitionsManagement'
import StoresManagement from './pages/admin/StoresManagement'
import UsersManagement from './pages/admin/users/UsersManagement'
import RolesManagement from './pages/admin/roles/RolesManagement'
import MyStore from './pages/store/MyStore'
import CreateStore from './pages/store/CreateStore'
import ProductCreate from './pages/store/ProductCreate'
import ProductEdit from './pages/store/ProductEdit'
import SkuManagement from './pages/store/SkuManagement'
import MyProducts from './pages/store/MyProducts'
import StorePage from './pages/store/StorePage'
import ProductPage from './pages/product/ProductPage'
import { CategoryProductsPage } from './pages/catalog/CategoryProductsPage'
import NotFound from './pages/NotFound'
// no top-level Fragment needed here

export default function App() {
  const { t } = useTranslation()
  return (
    <ThemeProvider>
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<Home />} />
          <Route path="about" element={<About />} />
          <Route path="contacts" element={<Contacts />} />
          <Route path="store/:slug" element={<StorePage />} />
          <Route path="category/:slug" element={<CategoryProductsPage />} />
          <Route path="product/:productSlug" element={<ProductPage />} />
          <Route path="product/:productSlug/:skuCode" element={<ProductPage />} />
          <Route path="auth" element={<Auth />} />
          <Route path="reset-password" element={<ResetPassword />} />

          {/* Protected routes - require authentication */}
          <Route
            path="cabinet/*"
            element={
              <ProtectedRoute requireAuth>
                <Cabinet />
              </ProtectedRoute>
            }
          >
            <Route index element={<div className="p-6">{t('greeting', { name: '' })}</div>} />
            <Route path="user/settings" element={<SettingsPage />} />
            <Route path="my-store" element={<MyStore />} />
            <Route path="create-store" element={<CreateStore />} />
            <Route path="products" element={<MyProducts />} />
            <Route path="products/create" element={<ProductCreate />} />
            <Route path="products/:productId/edit" element={<ProductEdit />} />
            <Route path="products/:productId/skus" element={<SkuManagement />} />
            <Route path="orders" element={<div className="p-6">{t('menu.orders')} ({t('common.empty')})</div>} />
            <Route path="tracking" element={<div className="p-6">{t('menu.tracking')} ({t('common.empty')})</div>} />
            <Route path="favorites" element={<Favorites />} />
            <Route path="wallet" element={<div className="p-6">{t('menu.wallet')} ({t('common.empty')})</div>} />
            <Route path="support" element={<div className="p-6">{t('menu.support')} ({t('common.empty')})</div>} />
            <Route path="help" element={<div className="p-6">{t('menu.help')} ({t('common.empty')})</div>} />
          </Route>

          {/* Admin routes - require Admin role */}
          <Route
            path="admin"
            element={
              <ProtectedRoute requireAuth requiredRoles={['Admin']}>
                <AdminPanel />
              </ProtectedRoute>
            }
          >
            <Route path="categories" element={<CategoriesManagement />} />
            <Route path="tags" element={<TagsManagement />} />
            <Route path="attributes" element={<AttributeDefinitionsManagement />} />
            <Route path="stores" element={<StoresManagement />} />
            <Route path="users" element={<UsersManagement />} />
            <Route path="roles" element={<RolesManagement />} />
          </Route>

          {/* 404 routes */}
          <Route path="404" element={<NotFound />} />
          <Route path="*" element={<NotFound />} />
        </Route>
      </Routes>
    </ThemeProvider>
  )
}
