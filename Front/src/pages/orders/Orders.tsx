import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { type OrderStatus } from '../../api/ordersApi'
import { useAuthStore } from '../../store/authStore'
import { useNavigate } from 'react-router-dom'
import { useCancelOrder, useOrder, useOrders } from '../../hooks/queries/useOrders'

// Icons
const Package = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m7.5 4.27 9 5.15"/><path d="M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z"/><path d="m3.3 7 8.7 5 8.7-5"/><path d="M12 22V12"/></svg>
const Truck = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M14 18V6a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2v11a1 1 0 0 0 1 1h2"/><path d="M15 18H9"/><path d="M19 18h2a1 1 0 0 0 1-1v-3.65a1 1 0 0 0-.22-.624l-3.48-4.35A1 1 0 0 0 17.52 8H14"/><circle cx="17" cy="18" r="2"/><circle cx="7" cy="18" r="2"/></svg>
const CheckCircle = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><path d="m9 11 3 3L22 4"/></svg>
const XCircle = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10"/><path d="m15 9-6 6"/><path d="m9 9 6 6"/></svg>
const Clock = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
const RefreshCw = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8"/><path d="M21 3v5h-5"/><path d="M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16"/><path d="M8 16H3v5"/></svg>
const Eye = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/><circle cx="12" cy="12" r="3"/></svg>
const X = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M18 6 6 18"/><path d="m6 6 12 12"/></svg>
const ChevronLeft = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m15 18-6-6 6-6"/></svg>
const ChevronRight = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m9 18 6-6-6-6"/></svg>

const statusConfig: Record<OrderStatus, { label: string; color: string; icon: React.FC<{ className?: string }> }> = {
  Pending: { label: 'Pending', color: 'bg-yellow-100 text-yellow-800', icon: Clock },
  Confirmed: { label: 'Confirmed', color: 'bg-blue-100 text-blue-800', icon: CheckCircle },
  Processing: { label: 'Processing', color: 'bg-purple-100 text-purple-800', icon: Package },
  Shipped: { label: 'Shipped', color: 'bg-indigo-100 text-indigo-800', icon: Truck },
  Delivered: { label: 'Delivered', color: 'bg-green-100 text-green-800', icon: CheckCircle },
  Cancelled: { label: 'Cancelled', color: 'bg-red-100 text-red-800', icon: XCircle },
}

export default function Orders() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)

  const [error, setError] = useState<string | null>(null)
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState<OrderStatus | null>(null)
  const [sortBy, setSortBy] = useState<'date' | 'status'>('date')
  const [pageNumber, setPageNumber] = useState(1)
  const [showCancelModal, setShowCancelModal] = useState(false)
  const [cancelReason, setCancelReason] = useState('')
  const ordersQuery = useOrders({
    status: statusFilter,
    sortBy: sortBy === 'date' ? 'CreatedAt' : 'Status',
    pageNumber,
    pageSize: 10,
  })
  const orderDetailsQuery = useOrder(selectedOrderId)
  const cancelOrderMutation = useCancelOrder()

  const orders = ordersQuery.data?.orders ?? []
  const totalPages = ordersQuery.data?.totalPages ?? 1
  const isLoading = ordersQuery.isPending
  const isLoadingDetails = orderDetailsQuery.isPending
  const selectedOrder = orderDetailsQuery.data ?? null

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/auth', { state: { from: '/orders' } })
    }
  }, [isAuthenticated, navigate])

  useEffect(() => {
    if (ordersQuery.error) {
      setError(ordersQuery.error.message || 'Failed to load orders')
    }
  }, [ordersQuery.error])

  const viewOrderDetails = async (orderId: string) => {
    setSelectedOrderId(orderId)
  }

  const handleCancelOrder = async () => {
    if (!selectedOrder) return

    try {
      const response = await cancelOrderMutation.mutateAsync({
        orderId: selectedOrder.id,
        data: { reason: cancelReason },
      })
      if (response.isSuccess) {
        setShowCancelModal(false)
        setCancelReason('')
        setSelectedOrderId(null)
      } else {
        setError(response.message)
      }
    } catch (err) {
      setError('Failed to cancel order')
    }
  }

  const canCancel = (status: OrderStatus) => {
    return status === 'Pending' || status === 'Confirmed'
  }

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="animate-pulse">
          <div className="h-8 bg-gray-200 rounded w-1/4 mb-8"></div>
          <div className="space-y-4">
            {[1, 2, 3].map((i) => (
              <div key={i} className="h-24 bg-gray-200 rounded"></div>
            ))}
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold text-gray-900 mb-8">
        {t('orders.title', 'My Orders')}
      </h1>

      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-md">
          <p className="text-red-700">{error}</p>
          <button
            onClick={() => setError(null)}
            className="text-sm text-red-600 underline mt-1"
          >
            {t('common.dismiss', 'Dismiss')}
          </button>
        </div>
      )}

      {/* Filters */}
      <div className="mb-6 flex flex-wrap gap-4">
        <select
          value={statusFilter || ''}
          onChange={(e) => setStatusFilter(e.target.value as OrderStatus || null)}
          className="px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">{t('orders.allStatuses', 'All Statuses')}</option>
          <option value="Pending">{t('orders.status.pending', 'Pending')}</option>
          <option value="Confirmed">{t('orders.status.confirmed', 'Confirmed')}</option>
          <option value="Processing">{t('orders.status.processing', 'Processing')}</option>
          <option value="Shipped">{t('orders.status.shipped', 'Shipped')}</option>
          <option value="Delivered">{t('orders.status.delivered', 'Delivered')}</option>
          <option value="Cancelled">{t('orders.status.cancelled', 'Cancelled')}</option>
        </select>

        <select
          value={sortBy}
          onChange={(e) => setSortBy(e.target.value as 'date' | 'status')}
          className="px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="date">{t('orders.sort.date', 'Sort by Date')}</option>
          <option value="status">{t('orders.sort.status', 'Sort by Status')}</option>
        </select>

        <button
          onClick={() => ordersQuery.refetch()}
          className="flex items-center gap-2 px-4 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200"
        >
          <RefreshCw className="w-4 h-4" />
          {t('orders.refresh', 'Refresh')}
        </button>
      </div>

      {/* Orders List */}
      {orders.length === 0 ? (
        <div className="text-center py-12">
          <Package className="mx-auto h-16 w-16 text-gray-400 mb-4" />
          <h2 className="text-xl font-medium text-gray-900 mb-2">
            {t('orders.empty.title', 'No orders yet')}
          </h2>
          <p className="text-gray-600">
            {t('orders.empty.description', 'When you place orders, they will appear here.')}
          </p>
        </div>
      ) : (
        <>
          <div className="space-y-4">
            {orders.map((order) => {
              const config = statusConfig[order.status]
              const StatusIcon = config.icon

              return (
                <div
                  key={order.id}
                  className="bg-white rounded-lg shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow cursor-pointer"
                  onClick={() => viewOrderDetails(order.id)}
                >
                  <div className="flex flex-wrap items-center justify-between gap-4">
                    <div>
                      <p className="text-sm text-gray-500">{t('orders.orderNumber', 'Order #')}</p>
                      <p className="text-lg font-semibold text-gray-900">{order.orderNumber}</p>
                      <p className="text-sm text-gray-500 mt-1">
                        {new Date(order.createdAt).toLocaleDateString()}
                      </p>
                    </div>

                    <div className="flex items-center gap-4">
                      <div className={`flex items-center gap-2 px-3 py-1 rounded-full ${config.color}`}>
                        <StatusIcon className="w-4 h-4" />
                        <span className="text-sm font-medium">{config.label}</span>
                      </div>

                      <div className="text-right">
                        <p className="text-lg font-semibold text-gray-900">
                          ${order.totalPrice.toFixed(2)}
                        </p>
                        <p className="text-sm text-gray-500">
                          {order.totalItems} {t('orders.items', 'items')}
                        </p>
                      </div>

                      <Eye className="w-5 h-5 text-gray-400" />
                    </div>
                  </div>

                  {order.trackingNumber && (
                    <div className="mt-4 pt-4 border-t border-gray-100">
                      <p className="text-sm text-gray-600">
                        {t('orders.tracking', 'Tracking')}: {order.shippingCarrier} - {order.trackingNumber}
                      </p>
                    </div>
                  )}
                </div>
              )
            })}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex justify-center items-center gap-4 mt-8">
              <button
                onClick={() => setPageNumber(p => Math.max(1, p - 1))}
                disabled={pageNumber === 1}
                className="flex items-center gap-2 px-4 py-2 border border-gray-300 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
              >
                <ChevronLeft className="w-4 h-4" />
                {t('orders.previous', 'Previous')}
              </button>
              <span className="text-gray-600">
                {t('orders.page', 'Page {{page}} of {{total}}', { page: pageNumber, total: totalPages })}
              </span>
              <button
                onClick={() => setPageNumber(p => Math.min(totalPages, p + 1))}
                disabled={pageNumber === totalPages}
                className="flex items-center gap-2 px-4 py-2 border border-gray-300 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
              >
                {t('orders.next', 'Next')}
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          )}
        </>
      )}

      {/* Order Detail Modal */}
      {selectedOrder && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-2xl w-full max-h-[90vh] overflow-y-auto">
            <div className="p-6 border-b border-gray-200 flex justify-between items-center">
              <h2 className="text-xl font-semibold text-gray-900">
                {t('orders.orderDetails', 'Order Details')}
              </h2>
              <button
                onClick={() => setSelectedOrderId(null)}
                className="p-2 hover:bg-gray-100 rounded-full"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="p-6">
              {isLoadingDetails ? (
                <div className="animate-pulse space-y-4">
                  <div className="h-4 bg-gray-200 rounded w-3/4"></div>
                  <div className="h-4 bg-gray-200 rounded w-1/2"></div>
                </div>
              ) : (
                <>
                  {/* Order Header */}
                  <div className="mb-6">
                    <p className="text-sm text-gray-500">{t('orders.orderNumber', 'Order #')}</p>
                    <p className="text-xl font-semibold text-gray-900">{selectedOrder.orderNumber}</p>
                    <div className="flex items-center gap-2 mt-2">
                      {(() => {
                        const config = statusConfig[selectedOrder.status]
                        const StatusIcon = config.icon
                        return (
                          <span className={`flex items-center gap-1 px-2 py-1 rounded-full text-sm ${config.color}`}>
                            <StatusIcon className="w-4 h-4" />
                            {config.label}
                          </span>
                        )
                      })()}
                    </div>
                  </div>

                  {/* Shipping Address */}
                  <div className="mb-6 p-4 bg-gray-50 rounded-lg">
                    <h3 className="font-medium text-gray-900 mb-2">
                      {t('orders.shippingAddress', 'Shipping Address')}
                    </h3>
                    <p className="text-sm text-gray-600">
                      {selectedOrder.shippingAddress.fullName}<br />
                      {selectedOrder.shippingAddress.formattedAddress}
                    </p>
                  </div>

                  {/* Order Items */}
                  <div className="mb-6">
                    <h3 className="font-medium text-gray-900 mb-3">
                      {t('orders.items', 'Items')}
                    </h3>
                    <div className="space-y-3">
                      {selectedOrder.items.map((item) => (
                        <div key={item.id} className="flex justify-between items-center py-2 border-b border-gray-100">
                          <div className="flex items-center gap-3">
                            {item.productImageUrl ? (
                              <img
                                src={item.productImageUrl}
                                alt={item.productName}
                                className="w-12 h-12 object-cover rounded"
                              />
                            ) : (
                              <div className="w-12 h-12 bg-gray-200 rounded flex items-center justify-center">
                                <Package className="w-6 h-6 text-gray-400" />
                              </div>
                            )}
                            <div>
                              <p className="font-medium text-gray-900">{item.productName}</p>
                              <p className="text-sm text-gray-500">{item.skuCode}</p>
                              <p className="text-sm text-gray-500">Qty: {item.quantity}</p>
                            </div>
                          </div>
                          <span className="font-medium">${item.subtotal.toFixed(2)}</span>
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Order Summary */}
                  <div className="border-t border-gray-200 pt-4">
                    <div className="space-y-2">
                      <div className="flex justify-between text-sm">
                        <span className="text-gray-600">{t('orders.subtotal', 'Subtotal')}</span>
                        <span>${selectedOrder.subtotal.toFixed(2)}</span>
                      </div>
                      <div className="flex justify-between text-sm">
                        <span className="text-gray-600">{t('orders.shipping', 'Shipping')}</span>
                        <span>${selectedOrder.shippingCost.toFixed(2)}</span>
                      </div>
                      {selectedOrder.discountAmount > 0 && (
                        <div className="flex justify-between text-sm">
                          <span className="text-gray-600">{t('orders.discount', 'Discount')}</span>
                          <span className="text-green-600">-${selectedOrder.discountAmount.toFixed(2)}</span>
                        </div>
                      )}
                      <div className="flex justify-between text-lg font-semibold pt-2 border-t border-gray-200">
                        <span>{t('orders.total', 'Total')}</span>
                        <span>${selectedOrder.totalPrice.toFixed(2)}</span>
                      </div>
                    </div>
                  </div>

                  {/* Actions */}
                  {canCancel(selectedOrder.status) && (
                    <div className="mt-6 pt-4 border-t border-gray-200">
                      <button
                        onClick={() => setShowCancelModal(true)}
                        className="w-full px-4 py-2 border border-red-300 text-red-600 rounded-md hover:bg-red-50"
                      >
                        {t('orders.cancelOrder', 'Cancel Order')}
                      </button>
                    </div>
                  )}
                </>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Cancel Modal */}
      {showCancelModal && selectedOrder && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-md w-full p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">
              {t('orders.cancel.title', 'Cancel Order')}
            </h3>
            <p className="text-gray-600 mb-4">
              {t('orders.cancel.confirmation', 'Are you sure you want to cancel this order?')}
            </p>
            <textarea
              value={cancelReason}
              onChange={(e) => setCancelReason(e.target.value)}
              placeholder={t('orders.cancel.reasonPlaceholder', 'Reason for cancellation (optional)')}
              className="w-full px-3 py-2 border border-gray-300 rounded-md mb-4 focus:outline-none focus:ring-2 focus:ring-blue-500"
              rows={3}
            />
            <div className="flex gap-3">
              <button
                onClick={() => {
                  setShowCancelModal(false)
                  setCancelReason('')
                }}
                className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-md hover:bg-gray-50"
              >
                {t('common.cancel', 'Cancel')}
              </button>
              <button
                onClick={handleCancelOrder}
                disabled={cancelOrderMutation.isPending}
                className="flex-1 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {cancelOrderMutation.isPending ? t('orders.cancel.processing', 'Processing...') : t('orders.cancel.confirm', 'Confirm Cancel')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
