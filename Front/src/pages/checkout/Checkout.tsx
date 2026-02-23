import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { useCartStore } from '../../store/cartStore'
import { useAuthStore } from '../../store/authStore'
import { ordersApi, type ShippingAddressRequest } from '../../api/ordersApi'
import { unwrapServiceResponse } from '../../api/types'

// Icons
const Check = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M20 6 9 17l-5-5"/></svg>
const ChevronRight = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m9 18 6-6-6-6"/></svg>
const ChevronLeft = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m15 18-6-6 6-6"/></svg>
const Truck = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M14 18V6a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2v11a1 1 0 0 0 1 1h2"/><path d="M15 18H9"/><path d="M19 18h2a1 1 0 0 0 1-1v-3.65a1 1 0 0 0-.22-.624l-3.48-4.35A1 1 0 0 0 17.52 8H14"/><circle cx="17" cy="18" r="2"/><circle cx="7" cy="18" r="2"/></svg>
const CreditCard = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect width="20" height="14" x="2" y="5" rx="2"/><line x1="2" x2="22" y1="10" y2="10"/></svg>
const MapPin = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"/><circle cx="12" cy="10" r="3"/></svg>
const Package = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m7.5 4.27 9 5.15"/><path d="M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z"/><path d="m3.3 7 8.7 5 8.7-5"/><path d="M12 22V12"/></svg>
const Loader2 = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 12a9 9 0 1 1-6.219-8.56"/></svg>

type CheckoutStep = 'shipping' | 'delivery' | 'payment' | 'review'

const deliveryMethods = [
  { id: 'standard', name: 'Standard Shipping', price: 0, duration: '5-7 business days' },
  { id: 'express', name: 'Express Shipping', price: 15, duration: '2-3 business days' },
  { id: 'overnight', name: 'Overnight Shipping', price: 35, duration: '1 business day' },
]

const paymentMethods = [
  { id: 'card', name: 'Credit/Debit Card', icon: CreditCard },
  { id: 'paypal', name: 'PayPal', icon: null },
  { id: 'cod', name: 'Cash on Delivery', icon: null },
]

export default function Checkout() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)
  const { cart, getTotalPrice, loadCart } = useCartStore()

  const [currentStep, setCurrentStep] = useState<CheckoutStep>('shipping')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [orderComplete, setOrderComplete] = useState(false)
  const [orderNumber, setOrderNumber] = useState('')
  const [error, setError] = useState<string | null>(null)

  // Form data
  const [shippingAddress, setShippingAddress] = useState<ShippingAddressRequest>({
    firstName: '',
    lastName: '',
    phoneNumber: '',
    email: '',
    addressLine1: '',
    addressLine2: '',
    city: '',
    state: '',
    postalCode: '',
    country: '',
  })
  const [selectedDelivery, setSelectedDelivery] = useState('standard')
  const [selectedPayment, setSelectedPayment] = useState('card')
  const [promoCode, setPromoCode] = useState('')
  const [customerNotes, setCustomerNotes] = useState('')

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/auth', { state: { from: '/checkout' } })
      return
    }
    loadCart()
  }, [isAuthenticated, navigate, loadCart])

  const steps: { id: CheckoutStep; label: string; icon: React.FC<{ className?: string }> }[] = [
    { id: 'shipping', label: t('checkout.steps.shipping', 'Shipping'), icon: MapPin },
    { id: 'delivery', label: t('checkout.steps.delivery', 'Delivery'), icon: Truck },
    { id: 'payment', label: t('checkout.steps.payment', 'Payment'), icon: CreditCard },
    { id: 'review', label: t('checkout.steps.review', 'Review'), icon: Package },
  ]

  const validateShipping = (): boolean => {
    const required = ['firstName', 'lastName', 'phoneNumber', 'email', 'addressLine1', 'city', 'postalCode', 'country']
    for (const field of required) {
      if (!shippingAddress[field as keyof ShippingAddressRequest]?.trim()) {
        setError(t('checkout.errors.requiredFields', 'Please fill in all required fields'))
        return false
      }
    }
    return true
  }

  const handleNext = () => {
    setError(null)

    if (currentStep === 'shipping') {
      if (!validateShipping()) return
      setCurrentStep('delivery')
    } else if (currentStep === 'delivery') {
      setCurrentStep('payment')
    } else if (currentStep === 'payment') {
      setCurrentStep('review')
    }
  }

  const handleBack = () => {
    setError(null)
    if (currentStep === 'delivery') setCurrentStep('shipping')
    else if (currentStep === 'payment') setCurrentStep('delivery')
    else if (currentStep === 'review') setCurrentStep('payment')
  }

  const handleSubmit = async () => {
    setIsSubmitting(true)
    setError(null)

    try {
      const deliveryMethod = deliveryMethods.find(d => d.id === selectedDelivery)?.name || 'Standard Shipping'
      const paymentMethod = paymentMethods.find(p => p.id === selectedPayment)?.name || 'Credit Card'

      const response = await ordersApi.createOrder({
        shippingAddress,
        deliveryMethod,
        paymentMethod,
        promoCode: promoCode || undefined,
        customerNotes: customerNotes || undefined,
        idempotencyKey: crypto.randomUUID(),
      })

      const order = unwrapServiceResponse(response)
      setOrderNumber(order.orderNumber)
      setOrderComplete(true)
      // FIX: Don't call clearCart() — backend already clears the cart atomically
      // during order creation. Calling it again would make a redundant DELETE /api/cart
      // that may fail on an already-empty cart.
      // Instead, just reset local cart state:
      useCartStore.setState({ cart: null })
    } catch (err) {
      setError(err instanceof Error ? err.message : t('checkout.errors.submit', 'Failed to create order'))
    } finally {
      setIsSubmitting(false)
    }
  }

  if (orderComplete) {
    return (
      <div className="container mx-auto px-4 py-16">
        <div className="max-w-md mx-auto text-center">
          <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6">
            <Check className="w-8 h-8 text-green-600" />
          </div>
          <h1 className="text-2xl font-bold text-gray-900 mb-2">
            {t('checkout.success.title', 'Order Placed Successfully!')}
          </h1>
          <p className="text-gray-600 mb-4">
            {t('checkout.success.message', 'Thank you for your order. Your order number is:')}
          </p>
          <p className="text-xl font-semibold text-blue-600 mb-6">{orderNumber}</p>
          <div className="space-y-3">
            <button
              onClick={() => navigate('/orders')}
              className="w-full px-6 py-3 bg-blue-600 text-white font-medium rounded-md hover:bg-blue-700"
            >
              {t('checkout.success.viewOrders', 'View My Orders')}
            </button>
            <button
              onClick={() => navigate('/')}
              className="w-full px-6 py-3 bg-gray-100 text-gray-700 font-medium rounded-md hover:bg-gray-200"
            >
              {t('checkout.success.continueShopping', 'Continue Shopping')}
            </button>
          </div>
        </div>
      </div>
    )
  }

  const subtotal = getTotalPrice()
  const deliveryPrice = deliveryMethods.find(d => d.id === selectedDelivery)?.price || 0
  const total = subtotal + deliveryPrice

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold text-gray-900 mb-8">
        {t('checkout.title', 'Checkout')}
      </h1>

      {/* Progress Steps */}
      <div className="mb-8">
        <div className="flex items-center justify-center">
          {steps.map((step, index) => {
            const StepIcon = step.icon
            const isActive = step.id === currentStep
            const isCompleted = steps.findIndex(s => s.id === currentStep) > index

            return (
              <div key={step.id} className="flex items-center">
                <div
                  className={`flex items-center gap-2 px-4 py-2 rounded-full ${
                    isActive
                      ? 'bg-blue-600 text-white'
                      : isCompleted
                      ? 'bg-green-100 text-green-700'
                      : 'bg-gray-100 text-gray-500'
                  }`}
                >
                  {isCompleted ? (
                    <Check className="w-5 h-5" />
                  ) : (
                    <StepIcon className="w-5 h-5" />
                  )}
                  <span className="hidden sm:inline font-medium">{step.label}</span>
                </div>
                {index < steps.length - 1 && (
                  <ChevronRight className="w-5 h-5 text-gray-400 mx-2" />
                )}
              </div>
            )
          })}
        </div>
      </div>

      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-md">
          <p className="text-red-700">{error}</p>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Main Content */}
        <div className="lg:col-span-2">
          {/* Shipping Step */}
          {currentStep === 'shipping' && (
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">
                {t('checkout.shipping.title', 'Shipping Address')}
              </h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    {t('checkout.shipping.firstName', 'First Name')} *
                  </label>
                  <input
                    type="text"
                    value={shippingAddress.firstName}
                    onChange={(e) => setShippingAddress({ ...shippingAddress, firstName: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    {t('checkout.shipping.lastName', 'Last Name')} *
                  </label>
                  <input
                    type="text"
                    value={shippingAddress.lastName}
                    onChange={(e) => setShippingAddress({ ...shippingAddress, lastName: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    {t('checkout.shipping.email', 'Email')} *
                  </label>
                  <input
                    type="email"
                    value={shippingAddress.email}
                    onChange={(e) => setShippingAddress({ ...shippingAddress, email: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    {t('checkout.shipping.phone', 'Phone Number')} *
                  </label>
                  <input
                    type="tel"
                    value={shippingAddress.phoneNumber}
                    onChange={(e) => setShippingAddress({ ...shippingAddress, phoneNumber: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div className="md:col-span-2">
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    {t('checkout.shipping.address1', 'Address Line 1')} *
                  </label>
                  <input
                    type="text"
                    value={shippingAddress.addressLine1}
                    onChange={(e) => setShippingAddress({ ...shippingAddress, addressLine1: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div className="md:col-span-2">
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    {t('checkout.shipping.address2', 'Address Line 2')}
                  </label>
                  <input
                    type="text"
                    value={shippingAddress.addressLine2 || ''}
                    onChange={(e) => setShippingAddress({ ...shippingAddress, addressLine2: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    {t('checkout.shipping.city', 'City')} *
                  </label>
                  <input
                    type="text"
                    value={shippingAddress.city}
                    onChange={(e) => setShippingAddress({ ...shippingAddress, city: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    {t('checkout.shipping.state', 'State/Province')}
                  </label>
                  <input
                    type="text"
                    value={shippingAddress.state || ''}
                    onChange={(e) => setShippingAddress({ ...shippingAddress, state: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    {t('checkout.shipping.postalCode', 'Postal Code')} *
                  </label>
                  <input
                    type="text"
                    value={shippingAddress.postalCode}
                    onChange={(e) => setShippingAddress({ ...shippingAddress, postalCode: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    {t('checkout.shipping.country', 'Country')} *
                  </label>
                  <input
                    type="text"
                    value={shippingAddress.country}
                    onChange={(e) => setShippingAddress({ ...shippingAddress, country: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>
            </div>
          )}

          {/* Delivery Step */}
          {currentStep === 'delivery' && (
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">
                {t('checkout.delivery.title', 'Delivery Method')}
              </h2>
              <div className="space-y-4">
                {deliveryMethods.map((method) => (
                  <label
                    key={method.id}
                    className={`flex items-center justify-between p-4 border rounded-lg cursor-pointer transition-colors ${
                      selectedDelivery === method.id
                        ? 'border-blue-500 bg-blue-50'
                        : 'border-gray-200 hover:border-gray-300'
                    }`}
                  >
                    <div className="flex items-center gap-4">
                      <input
                        type="radio"
                        name="delivery"
                        value={method.id}
                        checked={selectedDelivery === method.id}
                        onChange={() => setSelectedDelivery(method.id)}
                        className="w-4 h-4 text-blue-600"
                      />
                      <div>
                        <p className="font-medium text-gray-900">{method.name}</p>
                        <p className="text-sm text-gray-500">{method.duration}</p>
                      </div>
                    </div>
                    <span className="font-semibold text-gray-900">
                      {method.price === 0 ? t('checkout.free', 'Free') : `₴${method.price.toFixed(2)}`}
                    </span>
                  </label>
                ))}
              </div>
            </div>
          )}

          {/* Payment Step */}
          {currentStep === 'payment' && (
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">
                {t('checkout.payment.title', 'Payment Method')}
              </h2>
              <div className="space-y-4">
                {paymentMethods.map((method) => (
                  <label
                    key={method.id}
                    className={`flex items-center gap-4 p-4 border rounded-lg cursor-pointer transition-colors ${
                      selectedPayment === method.id
                        ? 'border-blue-500 bg-blue-50'
                        : 'border-gray-200 hover:border-gray-300'
                    }`}
                  >
                    <input
                      type="radio"
                      name="payment"
                      value={method.id}
                      checked={selectedPayment === method.id}
                      onChange={() => setSelectedPayment(method.id)}
                      className="w-4 h-4 text-blue-600"
                    />
                    {method.icon && <method.icon className="w-6 h-6 text-gray-600" />}
                    <span className="font-medium text-gray-900">{method.name}</span>
                  </label>
                ))}
              </div>

              <div className="mt-6">
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  {t('checkout.notes', 'Order Notes (Optional)')}
                </label>
                <textarea
                  value={customerNotes}
                  onChange={(e) => setCustomerNotes(e.target.value)}
                  rows={3}
                  placeholder={t('checkout.notesPlaceholder', 'Any special instructions for your order...')}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
            </div>
          )}

          {/* Review Step */}
          {currentStep === 'review' && (
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">
                {t('checkout.review.title', 'Review Your Order')}
              </h2>

              {/* Shipping Summary */}
              <div className="mb-6 p-4 bg-gray-50 rounded-lg">
                <h3 className="font-medium text-gray-900 mb-2">
                  {t('checkout.review.shipping', 'Shipping Address')}
                </h3>
                <p className="text-sm text-gray-600">
                  {shippingAddress.firstName} {shippingAddress.lastName}<br />
                  {shippingAddress.addressLine1}<br />
                  {shippingAddress.addressLine2 && <>{shippingAddress.addressLine2}<br /></>}
                  {shippingAddress.city}, {shippingAddress.state} {shippingAddress.postalCode}<br />
                  {shippingAddress.country}
                </p>
              </div>

              {/* Delivery Summary */}
              <div className="mb-6 p-4 bg-gray-50 rounded-lg">
                <h3 className="font-medium text-gray-900 mb-2">
                  {t('checkout.review.delivery', 'Delivery Method')}
                </h3>
                <p className="text-sm text-gray-600">
                  {deliveryMethods.find(d => d.id === selectedDelivery)?.name}
                </p>
              </div>

              {/* Payment Summary */}
              <div className="mb-6 p-4 bg-gray-50 rounded-lg">
                <h3 className="font-medium text-gray-900 mb-2">
                  {t('checkout.review.payment', 'Payment Method')}
                </h3>
                <p className="text-sm text-gray-600">
                  {paymentMethods.find(p => p.id === selectedPayment)?.name}
                </p>
              </div>

              {/* Items Summary */}
              <div className="mb-6">
                <h3 className="font-medium text-gray-900 mb-3">
                  {t('checkout.review.items', 'Order Items')}
                </h3>
                <div className="space-y-3">
                  {cart?.items.map((item) => (
                    <div key={item.id} className="flex justify-between items-center py-2 border-b border-gray-100">
                      <div className="flex items-center gap-3">
                        <span className="text-sm font-medium">{item.quantity}x</span>
                        <span className="text-sm">{item.productName}</span>
                      </div>
                      <span className="text-sm font-medium">₴{item.subtotal.toFixed(2)}</span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* Navigation Buttons */}
          <div className="flex justify-between mt-6">
            {currentStep !== 'shipping' ? (
              <button
                onClick={handleBack}
                className="flex items-center gap-2 px-6 py-3 border border-gray-300 text-gray-700 font-medium rounded-md hover:bg-gray-50"
              >
                <ChevronLeft className="w-5 h-5" />
                {t('checkout.back', 'Back')}
              </button>
            ) : (
              <div />
            )}

            {currentStep !== 'review' ? (
              <button
                onClick={handleNext}
                className="flex items-center gap-2 px-6 py-3 bg-blue-600 text-white font-medium rounded-md hover:bg-blue-700"
              >
                {t('checkout.next', 'Next')}
                <ChevronRight className="w-5 h-5" />
              </button>
            ) : (
              <button
                onClick={handleSubmit}
                disabled={isSubmitting}
                className="flex items-center gap-2 px-6 py-3 bg-green-600 text-white font-medium rounded-md hover:bg-green-700 disabled:opacity-50"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="w-5 h-5 animate-spin" />
                    {t('checkout.processing', 'Processing...')}
                  </>
                ) : (
                  <>
                    {t('checkout.placeOrder', 'Place Order')}
                    <Check className="w-5 h-5" />
                  </>
                )}
              </button>
            )}
          </div>
        </div>

        {/* Order Summary Sidebar */}
        <div className="lg:col-span-1">
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6 sticky top-4">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
              {t('checkout.orderSummary', 'Order Summary')}
            </h2>

            <div className="space-y-2 mb-4">
              <div className="flex justify-between text-sm">
                <span className="text-gray-600">{t('checkout.subtotal', 'Subtotal')}</span>
                <span className="font-medium">₴{subtotal.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-gray-600">{t('checkout.shipping', 'Shipping')}</span>
                <span className="font-medium">
                  {deliveryPrice === 0 ? t('checkout.free', 'Free') : `₴${deliveryPrice.toFixed(2)}`}
                </span>
              </div>
              <div className="border-t border-gray-200 pt-2 mt-2">
                <div className="flex justify-between">
                  <span className="text-lg font-semibold text-gray-900">{t('checkout.total', 'Total')}</span>
                  <span className="text-xl font-bold text-gray-900">₴{total.toFixed(2)}</span>
                </div>
              </div>
            </div>

            {/* Promo Code */}
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {t('checkout.promoCode', 'Promo Code')}
              </label>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={promoCode}
                  onChange={(e) => setPromoCode(e.target.value)}
                  placeholder={t('checkout.enterPromoCode', 'Enter code')}
                  className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <button
                  disabled={!promoCode.trim()}
                  className="px-4 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 disabled:opacity-50"
                >
                  {t('checkout.apply', 'Apply')}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
