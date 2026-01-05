import { useState, useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { 
  getRoles, 
  getAllPermissions,
  createRole, 
  updateRole, 
  deleteRole
} from '../../../api/adminApi'
import type { RoleDto, PermissionDto, CreateRoleDto, UpdateRoleDto } from '../../../api/adminApi'

export default function RolesManagement() {
  const { t } = useTranslation()
  const [roles, setRoles] = useState<RoleDto[]>([])
  const [permissions, setPermissions] = useState<Record<string, PermissionDto[]>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  
  // Modal state
  const [showModal, setShowModal] = useState(false)
  const [editingRole, setEditingRole] = useState<RoleDto | null>(null)
  const [formData, setFormData] = useState<CreateRoleDto>({
    name: '',
    description: '',
    permissions: []
  })
  const [expandedCategories, setExpandedCategories] = useState<string[]>([])
  const [saving, setSaving] = useState(false)
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null)

  // Built-in roles that cannot be deleted
  const builtInRoles = ['Admin', 'User', 'Seller']

  const loadData = useCallback(async () => {
    try {
      setLoading(true)
      const [rolesRes, permissionsRes] = await Promise.all([
        getRoles(),
        getAllPermissions()
      ])
      
      if (rolesRes.isSuccess && rolesRes.payload) {
        setRoles(rolesRes.payload)
      }
      if (permissionsRes.isSuccess && permissionsRes.payload) {
        setPermissions(permissionsRes.payload)
      }
    } catch (err) {
      setError(t('admin.roles.load_error'))
      console.error(err)
    } finally {
      setLoading(false)
    }
  }, [t])

  useEffect(() => {
    loadData()
  }, [loadData])

  const handleOpenCreateModal = () => {
    setEditingRole(null)
    setFormData({ name: '', description: '', permissions: [] })
    setExpandedCategories([])
    setShowModal(true)
  }

  const handleOpenEditModal = (role: RoleDto) => {
    setEditingRole(role)
    setFormData({
      name: role.name,
      description: role.description,
      permissions: [...role.permissions]
    })
    // Expand categories that have selected permissions
    const categoriesToExpand = Object.entries(permissions)
      .filter(([, perms]) => perms.some(p => role.permissions.includes(p.name)))
      .map(([category]) => category)
    setExpandedCategories(categoriesToExpand)
    setShowModal(true)
  }

  const handleSave = async () => {
    try {
      setSaving(true)
      
      if (editingRole) {
        const updateData: UpdateRoleDto = {}
        if (formData.name !== editingRole.name) updateData.name = formData.name
        if (formData.description !== editingRole.description) updateData.description = formData.description
        updateData.permissions = formData.permissions
        
        const res = await updateRole(editingRole.id, updateData)
        if (res.isSuccess && res.payload) {
          setRoles(prev => prev.map(r => r.id === editingRole.id ? res.payload! : r))
          setShowModal(false)
        } else {
          setError(res.message)
        }
      } else {
        const res = await createRole(formData)
        if (res.isSuccess && res.payload) {
          setRoles(prev => [...prev, res.payload!])
          setShowModal(false)
        } else {
          setError(res.message)
        }
      }
    } catch (err) {
      setError(t('admin.roles.save_error'))
      console.error(err)
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (roleId: string) => {
    try {
      const res = await deleteRole(roleId)
      if (res.isSuccess) {
        setRoles(prev => prev.filter(r => r.id !== roleId))
        setDeleteConfirm(null)
      } else {
        setError(res.message)
      }
    } catch (err) {
      setError(t('admin.roles.delete_error'))
      console.error(err)
    }
  }

  const toggleCategory = (category: string) => {
    setExpandedCategories(prev => 
      prev.includes(category)
        ? prev.filter(c => c !== category)
        : [...prev, category]
    )
  }

  const togglePermission = (permissionName: string) => {
    setFormData(prev => ({
      ...prev,
      permissions: prev.permissions.includes(permissionName)
        ? prev.permissions.filter(p => p !== permissionName)
        : [...prev.permissions, permissionName]
    }))
  }

  const toggleAllInCategory = (category: string) => {
    const categoryPermissions = permissions[category]?.map(p => p.name) || []
    const allSelected = categoryPermissions.every(p => formData.permissions.includes(p))
    
    setFormData(prev => ({
      ...prev,
      permissions: allSelected
        ? prev.permissions.filter(p => !categoryPermissions.includes(p))
        : [...new Set([...prev.permissions, ...categoryPermissions])]
    }))
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-brand"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h2 className="text-2xl font-bold text-text">{t('admin.roles.title')}</h2>
        <button onClick={handleOpenCreateModal} className="btn-primary flex items-center gap-2">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          {t('admin.roles.create')}
        </button>
      </div>

      {error && (
        <div className="bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 p-4 rounded-lg">
          {error}
          <button onClick={() => setError(null)} className="ml-4 underline">{t('common.dismiss')}</button>
        </div>
      )}

      {/* Roles Grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {roles.map(role => (
          <div key={role.id} className="card p-5">
            <div className="flex items-start justify-between mb-3">
              <div className="flex items-center gap-3">
                <div className={`p-2 rounded-lg ${
                  role.name === 'Admin' 
                    ? 'bg-red-100 dark:bg-red-900/30 text-red-600'
                    : role.name === 'Seller'
                    ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-600'
                    : 'bg-brand/10 text-brand'
                }`}>
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                  </svg>
                </div>
                <div>
                  <h3 className="font-semibold text-text">{role.name}</h3>
                  {role.description && (
                    <p className="text-sm text-text-muted">{role.description}</p>
                  )}
                </div>
              </div>
              
              {!builtInRoles.includes(role.name) && (
                <div className="flex gap-1">
                  <button
                    onClick={() => handleOpenEditModal(role)}
                    className="p-1.5 text-text-muted hover:text-brand hover:bg-brand/10 rounded"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button
                    onClick={() => setDeleteConfirm(role.id)}
                    className="p-1.5 text-text-muted hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              )}
              {builtInRoles.includes(role.name) && (
                <button
                  onClick={() => handleOpenEditModal(role)}
                  className="p-1.5 text-text-muted hover:text-brand hover:bg-brand/10 rounded"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
              )}
            </div>

            <div className="flex items-center gap-4 text-sm text-text-muted">
              <div className="flex items-center gap-1">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                </svg>
                <span>{t('admin.roles.permissions_count', { count: role.permissions.length })}</span>
              </div>
              <div className="flex items-center gap-1">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
                </svg>
                <span>{t('admin.roles.users_count', { count: role.usersCount })}</span>
              </div>
            </div>

            {/* Permissions preview */}
            {role.permissions.length > 0 && (
              <div className="mt-3 flex flex-wrap gap-1">
                {role.permissions.slice(0, 5).map(perm => (
                  <span key={perm} className="px-2 py-0.5 text-xs bg-surface-alt rounded text-text-muted">
                    {perm}
                  </span>
                ))}
                {role.permissions.length > 5 && (
                  <span className="px-2 py-0.5 text-xs bg-surface-alt rounded text-text-muted">
                    +{role.permissions.length - 5}
                  </span>
                )}
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Create/Edit Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-surface rounded-xl shadow-xl max-w-2xl w-full max-h-[90vh] flex flex-col">
            <div className="flex items-center justify-between p-4 border-b border-border">
              <h3 className="text-lg font-semibold text-text">
                {editingRole ? t('admin.roles.edit') : t('admin.roles.create')}
              </h3>
              <button 
                onClick={() => setShowModal(false)}
                className="text-text-muted hover:text-text"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            
            <div className="p-4 space-y-4 overflow-y-auto flex-1">
              {/* Name */}
              <div>
                <label className="block text-sm font-medium text-text mb-1">
                  {t('admin.roles.name')} *
                </label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
                  disabled={!!(editingRole && builtInRoles.includes(editingRole.name))}
                  className="w-full px-3 py-2 rounded-lg border border-border bg-surface text-text focus:ring-2 focus:ring-brand focus:border-transparent disabled:opacity-50"
                  placeholder={t('admin.roles.name_placeholder')}
                />
              </div>

              {/* Description */}
              <div>
                <label className="block text-sm font-medium text-text mb-1">
                  {t('admin.roles.description')}
                </label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
                  className="w-full px-3 py-2 rounded-lg border border-border bg-surface text-text focus:ring-2 focus:ring-brand focus:border-transparent"
                  rows={2}
                  placeholder={t('admin.roles.description_placeholder')}
                />
              </div>

              {/* Permissions */}
              <div>
                <label className="block text-sm font-medium text-text mb-2">
                  {t('admin.roles.permissions')} ({formData.permissions.length})
                </label>
                
                <div className="border border-border rounded-lg divide-y divide-border">
                  {Object.entries(permissions).map(([category, categoryPerms]) => {
                    const isExpanded = expandedCategories.includes(category)
                    const selectedCount = categoryPerms.filter(p => formData.permissions.includes(p.name)).length
                    const allSelected = selectedCount === categoryPerms.length
                    
                    return (
                      <div key={category}>
                        <button
                          type="button"
                          onClick={() => toggleCategory(category)}
                          className="w-full flex items-center justify-between p-3 hover:bg-surface-alt transition-colors"
                        >
                          <div className="flex items-center gap-3">
                            <input
                              type="checkbox"
                              checked={allSelected}
                              onChange={(e) => { e.stopPropagation(); toggleAllInCategory(category) }}
                              className="w-4 h-4 text-brand rounded focus:ring-brand"
                            />
                            <span className="font-medium text-text">{category}</span>
                            <span className="text-sm text-text-muted">
                              ({selectedCount}/{categoryPerms.length})
                            </span>
                          </div>
                          {isExpanded ? (
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" />
                            </svg>
                          ) : (
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                            </svg>
                          )}
                        </button>
                        
                        {isExpanded && (
                          <div className="px-3 pb-3 grid gap-2 sm:grid-cols-2">
                            {categoryPerms.map(perm => (
                              <label 
                                key={perm.name}
                                className={`flex items-start gap-2 p-2 rounded cursor-pointer transition-colors ${
                                  formData.permissions.includes(perm.name)
                                    ? 'bg-brand/10'
                                    : 'hover:bg-surface-alt'
                                }`}
                              >
                                <input
                                  type="checkbox"
                                  checked={formData.permissions.includes(perm.name)}
                                  onChange={() => togglePermission(perm.name)}
                                  className="w-4 h-4 text-brand rounded focus:ring-brand mt-0.5"
                                />
                                <div>
                                  <div className="text-sm font-medium text-text">{perm.name}</div>
                                  <div className="text-xs text-text-muted">{perm.description}</div>
                                </div>
                              </label>
                            ))}
                          </div>
                        )}
                      </div>
                    )
                  })}
                </div>
              </div>
            </div>

            <div className="flex justify-end gap-3 p-4 border-t border-border">
              <button
                onClick={() => setShowModal(false)}
                className="px-4 py-2 text-text-muted hover:text-text"
              >
                {t('common.cancel')}
              </button>
              <button
                onClick={handleSave}
                disabled={saving || !formData.name.trim()}
                className="btn-primary disabled:opacity-50"
              >
                {saving ? t('common.saving') : t('common.save')}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {deleteConfirm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-surface rounded-xl shadow-xl max-w-sm w-full p-6">
            <h3 className="text-lg font-semibold text-text mb-2">{t('admin.roles.delete_confirm_title')}</h3>
            <p className="text-text-muted mb-4">{t('admin.roles.delete_confirm_message')}</p>
            
            <div className="flex justify-end gap-3">
              <button
                onClick={() => setDeleteConfirm(null)}
                className="px-4 py-2 text-text-muted hover:text-text"
              >
                {t('common.cancel')}
              </button>
              <button
                onClick={() => handleDelete(deleteConfirm)}
                className="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600"
              >
                {t('common.delete')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
