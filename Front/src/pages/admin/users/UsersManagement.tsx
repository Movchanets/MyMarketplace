import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { AdminUserDto } from '../../../api/adminApi'
import {
  useAdminRoles,
  useAdminUsers,
  useAssignUserRoles,
  useLockUser,
  useUnlockUser,
} from '../../../hooks/queries/useAdminManagement'

export default function UsersManagement() {
  const { t } = useTranslation()
  const [error, setError] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState('')
  const [actionLoadingUserId, setActionLoadingUserId] = useState<string | null>(null)
  const usersQuery = useAdminUsers()
  const rolesQuery = useAdminRoles()
  const assignUserRolesMutation = useAssignUserRoles()
  const lockUserMutation = useLockUser()
  const unlockUserMutation = useUnlockUser()
  const users = usersQuery.data ?? []
  const roles = rolesQuery.data ?? []
  const loading = usersQuery.isLoading || rolesQuery.isLoading
  const queryError =
    (usersQuery.error instanceof Error ? usersQuery.error.message : null) ||
    (rolesQuery.error instanceof Error ? rolesQuery.error.message : null)
  
  // Role assignment modal
  const [selectedUser, setSelectedUser] = useState<AdminUserDto | null>(null)
  const [selectedRoles, setSelectedRoles] = useState<string[]>([])
  const [showRoleModal, setShowRoleModal] = useState(false)
  const [saving, setSaving] = useState(false)

  const handleOpenRoleModal = (user: AdminUserDto) => {
    setSelectedUser(user)
    setSelectedRoles([...user.roles])
    setShowRoleModal(true)
  }

  const handleSaveRoles = async () => {
    if (!selectedUser) return
    
    try {
      setError(null)
      setSaving(true)
      await assignUserRolesMutation.mutateAsync({ userId: selectedUser.id, data: { roles: selectedRoles } })
      setShowRoleModal(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('admin.users.save_error'))
    } finally {
      setSaving(false)
    }
  }

  const handleToggleLock = async (user: AdminUserDto) => {
    try {
      setError(null)
      setActionLoadingUserId(user.id)
      if (user.isLocked) {
        await unlockUserMutation.mutateAsync(user.id)
      } else {
        await lockUserMutation.mutateAsync(user.id)
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : t('admin.users.lock_error'))
    } finally {
      setActionLoadingUserId(null)
    }
  }

  const toggleRole = (roleName: string) => {
    setSelectedRoles(prev => 
      prev.includes(roleName)
        ? prev.filter(r => r !== roleName)
        : [...prev, roleName]
    )
  }

  const filteredUsers = useMemo(() => {
    const query = searchQuery.toLowerCase()
    return users.filter(user => (
      user.username.toLowerCase().includes(query) ||
      user.email.toLowerCase().includes(query) ||
      user.name.toLowerCase().includes(query) ||
      user.surname.toLowerCase().includes(query)
    ))
  }, [users, searchQuery])

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
        <h2 className="text-2xl font-bold text-foreground">{t('admin.users.title')}</h2>
        <span className="text-foreground-muted">{t('admin.users.total', { count: users.length })}</span>
      </div>

      {(error || queryError) && (
        <div className="bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 p-4 rounded-lg">
          {error || queryError}
          <button onClick={() => setError(null)} className="ml-4 underline">{t('common.dismiss')}</button>
        </div>
      )}

      {/* Search */}
      <div className="relative">
        <svg className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-foreground-muted" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input
          type="text"
          placeholder={t('admin.users.search_placeholder')}
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="w-full pl-10 pr-4 py-2 rounded-lg border border-border bg-surface text-foreground focus:ring-2 focus:ring-brand focus:border-transparent"
        />
      </div>

      {/* Users Table */}
      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-surface-alt">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-medium text-foreground-muted">{t('admin.users.user')}</th>
                <th className="px-4 py-3 text-left text-sm font-medium text-foreground-muted">{t('admin.users.email')}</th>
                <th className="px-4 py-3 text-left text-sm font-medium text-foreground-muted">{t('admin.users.roles')}</th>
                <th className="px-4 py-3 text-left text-sm font-medium text-foreground-muted">{t('admin.users.status')}</th>
                <th className="px-4 py-3 text-left text-sm font-medium text-foreground-muted">{t('admin.users.created')}</th>
                <th className="px-4 py-3 text-right text-sm font-medium text-foreground-muted">{t('admin.users.actions')}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {filteredUsers.map(user => (
                <tr key={user.id} className="hover:bg-surface-alt/50">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      {user.avatarUrl ? (
                        <img 
                          src={user.avatarUrl} 
                          alt={user.username}
                          className="w-10 h-10 rounded-full object-cover"
                        />
                      ) : (
                        <div className="w-10 h-10 rounded-full bg-brand/10 flex items-center justify-center text-brand font-medium">
                          {user.name?.[0] || user.username?.[0] || '?'}
                        </div>
                      )}
                      <div>
                        <div className="font-medium text-foreground">{user.name} {user.surname}</div>
                        <div className="text-sm text-foreground-muted">@{user.username}</div>
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      <span className="text-foreground">{user.email}</span>
                      {user.isEmailConfirmed && (
                        <span className="text-green-500 text-xs">✓</span>
                      )}
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap gap-1">
                      {user.roles.map(role => (
                        <span 
                          key={role}
                          className={`px-2 py-0.5 text-xs rounded-full ${
                            role === 'Admin' 
                              ? 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300'
                              : role === 'Seller'
                              ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300'
                              : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300'
                          }`}
                        >
                          {role}
                        </span>
                      ))}
                    </div>
                  </td>
                  <td className="px-4 py-3">
                      {user.isLocked ? (
                      <span className="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium rounded-full bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300">
                        <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                        </svg>
                        {t('admin.users.locked')}
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium rounded-full bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300">
                        {t('admin.users.active')}
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-sm text-foreground-muted">
                    {new Date(user.createdAt).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center justify-end gap-2">
                      <button
                        onClick={() => handleOpenRoleModal(user)}
                        className="p-2 text-foreground-muted hover:text-brand hover:bg-brand/10 rounded-lg transition-colors"
                        title={t('admin.users.assign_roles')}
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                        </svg>
                      </button>
                      <button
                        onClick={() => handleToggleLock(user)}
                        disabled={actionLoadingUserId === user.id}
                        className={`p-2 rounded-lg transition-colors ${
                          user.isLocked 
                            ? 'text-green-600 hover:bg-green-100 dark:hover:bg-green-900/30'
                            : 'text-red-600 hover:bg-red-100 dark:hover:bg-red-900/30'
                        }`}
                        title={user.isLocked ? t('admin.users.unlock') : t('admin.users.lock')}
                      >
                        {user.isLocked ? (
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 11V7a4 4 0 118 0m-4 8v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2z" />
                          </svg>
                        ) : (
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                          </svg>
                        )}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Role Assignment Modal */}
      {showRoleModal && selectedUser && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-surface rounded-xl shadow-xl max-w-md w-full">
            <div className="flex items-center justify-between p-4 border-b border-border">
              <h3 className="text-lg font-semibold text-foreground">
                {t('admin.users.assign_roles_to', { name: selectedUser.username })}
              </h3>
              <button 
                onClick={() => setShowRoleModal(false)}
                className="text-foreground-muted hover:text-foreground"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            
            <div className="p-4 space-y-3 max-h-96 overflow-y-auto">
              {roles.map(role => (
                <label 
                  key={role.id}
                  className={`flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-colors ${
                    selectedRoles.includes(role.name)
                      ? 'bg-brand/10 border border-brand'
                      : 'bg-surface-alt border border-transparent hover:border-border'
                  }`}
                >
                  <input
                    type="checkbox"
                    checked={selectedRoles.includes(role.name)}
                    onChange={() => toggleRole(role.name)}
                    className="w-4 h-4 text-brand rounded focus:ring-brand"
                  />
                  <div className="flex-1">
                    <div className="font-medium text-foreground">{role.name}</div>
                    {role.description && (
                      <div className="text-sm text-foreground-muted">{role.description}</div>
                    )}
                    <div className="text-xs text-foreground-muted mt-1">
                      {t('admin.users.permissions_count', { count: role.permissions.length })}
                    </div>
                  </div>
                </label>
              ))}
            </div>

            <div className="flex justify-end gap-3 p-4 border-t border-border">
              <button
                onClick={() => setShowRoleModal(false)}
                className="px-4 py-2 text-foreground-muted hover:text-foreground"
              >
                {t('common.cancel')}
              </button>
              <button
                onClick={handleSaveRoles}
                disabled={saving}
                className="btn-primary"
              >
                {saving ? t('common.saving') : t('common.save')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
