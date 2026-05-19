import { http } from './http';
export const authApi = {
    login: (req) => http().post('/auth/login', req).then((r) => r.data),
    me: () => http().get('/auth/me').then((r) => r.data)
};
export const storesApi = {
    list: () => http().get('/stores').then((r) => r.data),
    create: (body) => http().post('/stores', body).then((r) => r.data),
    update: (id, body) => http().put(`/stores/${id}`, body).then((r) => r.data)
};
export const servicesApi = {
    list: (includeInactive = false) => http().get('/services', { params: { includeInactive } }).then((r) => r.data),
    create: (body) => http().post('/services', body).then((r) => r.data),
    update: (id, body) => http().put(`/services/${id}`, body).then((r) => r.data),
    remove: (id) => http().delete(`/services/${id}`)
};
export const membersApi = {
    list: (q) => http().get('/members', { params: q }).then((r) => r.data),
    get: (id) => http().get(`/members/${id}`).then((r) => r.data),
    create: (body) => http().post('/members', body).then((r) => r.data),
    update: (id, body) => http().put(`/members/${id}`, body).then((r) => r.data),
    recharge: (body) => http().post('/members/recharge', body).then((r) => r.data),
    rechargeHistory: (id) => http().get(`/members/${id}/recharges`).then((r) => r.data),
    consumptionHistory: (id) => http().get(`/members/${id}/orders`).then((r) => r.data),
    refund: (id, body) => http().post(`/members/${id}/refund`, body).then((r) => r.data),
    transfer: (id, body) => http().post(`/members/${id}/transfer`, body).then((r) => r.data),
    referrals: (id) => http().get(`/members/${id}/referrals`).then((r) => r.data)
};
export const ordersApi = {
    list: (params) => http().get('/orders', { params }).then((r) => r.data),
    get: (id) => http().get(`/orders/${id}`).then((r) => r.data),
    create: (body) => http().post('/orders', body).then((r) => r.data),
    checkout: (id, body) => http().post(`/orders/${id}/checkout`, body).then((r) => r.data),
    refund: (id, reason) => http().post(`/orders/${id}/refund`, { reason }).then((r) => r.data),
    cancel: (id) => http().post(`/orders/${id}/cancel`)
};
export const queueApi = {
    list: (storeId) => http().get('/queue', { params: { storeId } }).then((r) => r.data),
    setState: (technicianId, state) => http().post(`/queue/${technicianId}/state`, { state }),
    callNext: (storeId) => http().post('/queue/call-next', { storeId }).then((r) => r.data),
    resetDay: (storeId) => http().post('/queue/reset-day', null, { params: { storeId } })
};
export const staffApi = {
    list: (params) => http().get('/staff', { params }).then((r) => r.data),
    create: (body) => http().post('/staff', body).then((r) => r.data),
    update: (id, body) => http().put(`/staff/${id}`, body).then((r) => r.data),
    resetPassword: (id, newPassword) => http().post(`/staff/${id}/reset-password`, { newPassword }),
    transfers: (params) => http().get('/staff/transfers', { params }).then((r) => r.data),
    transfer: (id, body) => http().post(`/staff/${id}/transfer`, body).then((r) => r.data),
    returnTransfer: (transferId) => http().post(`/staff/transfers/${transferId}/return`).then((r) => r.data)
};
export const commissionsApi = {
    list: (serviceId, technicianId) => http().get('/commission-rules', { params: { serviceId, technicianId } }).then((r) => r.data),
    create: (body) => http().post('/commission-rules', body).then((r) => r.data),
    update: (id, body) => http().put(`/commission-rules/${id}`, body).then((r) => r.data),
    remove: (id) => http().delete(`/commission-rules/${id}`)
};
export const reportsApi = {
    daily: (storeId, date) => http().get('/reports/daily', { params: { storeId, date } }).then((r) => r.data),
    technicianPerformance: (storeId, from, to) => http().get('/reports/technician-performance', { params: { storeId, from, to } }).then((r) => r.data),
    monthly: (storeId, year, month) => http().get('/reports/monthly', { params: { storeId, year, month } }).then((r) => r.data),
    yearly: (storeId, year) => http().get('/reports/yearly', { params: { storeId, year } }).then((r) => r.data),
    servicePopularity: (storeId, from, to) => http().get('/reports/service-popularity', { params: { storeId, from, to } }).then((r) => r.data),
    customerFlow: (storeId, from, to) => http().get('/reports/customer-flow', { params: { storeId, from, to } }).then((r) => r.data),
    memberAnalysis: (storeId) => http().get('/reports/member-analysis', { params: { storeId } }).then((r) => r.data),
    serviceTrend: (storeId, months) => http().get('/reports/service-trend', { params: { storeId, months } }).then((r) => r.data),
    technicianQuality: (storeId, from, to) => http().get('/reports/technician-quality', { params: { storeId, from, to } }).then((r) => r.data)
};
export const subscriptionsApi = {
    me: () => http().get('/subscriptions/me').then((r) => r.data)
};
export const appointmentsApi = {
    list: (params) => http().get('/appointments', { params }).then((r) => r.data),
    confirm: (id, remark) => http().post(`/appointments/${id}/confirm`, { remark }).then((r) => r.data),
    arrive: (id) => http().post(`/appointments/${id}/arrive`).then((r) => r.data),
    cancel: (id, reason) => http().post(`/appointments/${id}/cancel`, { reason }).then((r) => r.data)
};
export const roomsApi = {
    list: (storeId, includeInactive = false) => http().get('/rooms', { params: { storeId, includeInactive } }).then((r) => r.data),
    create: (body) => http().post('/rooms', body).then((r) => r.data),
    update: (id, body) => http().put(`/rooms/${id}`, body).then((r) => r.data),
    remove: (id) => http().delete(`/rooms/${id}`)
};
export const timedRoomsApi = {
    sessions: (storeId, params) => http().get('/timed-rooms/sessions', { params: { storeId, ...params } }).then((r) => r.data),
    start: (roomId, body) => http().post(`/timed-rooms/${roomId}/start`, body).then((r) => r.data),
    stop: (id, payMethod) => http().post(`/timed-rooms/sessions/${id}/stop`, { payMethod }).then((r) => r.data),
    cancel: (id) => http().post(`/timed-rooms/sessions/${id}/cancel`).then((r) => r.data)
};
export const dayClosesApi = {
    preview: (storeId, date) => http().get('/day-closes/preview', { params: { storeId, date } }).then((r) => r.data),
    submit: (body) => http().post('/day-closes', body).then((r) => r.data),
    history: (storeId, from, to) => http().get('/day-closes', { params: { storeId, from, to } }).then((r) => r.data)
};
export const ordersTransferApi = {
    transfer: (orderId, itemId, body) => http().patch(`/orders/${orderId}/items/${itemId}/transfer`, body).then((r) => r.data),
    addItems: (orderId, items) => http().post(`/orders/${orderId}/items`, { items }).then((r) => r.data),
    reopen: (orderId, reason) => http().post(`/orders/${orderId}/reopen`, { reason }).then((r) => r.data),
    setTip: (orderId, tipAmount) => http().post(`/orders/${orderId}/tip`, { tipAmount }).then((r) => r.data)
};
export const vouchersApi = {
    list: (params) => http().get('/vouchers', { params }).then((r) => r.data),
    create: (body) => http().post('/vouchers', body).then((r) => r.data),
    redeem: (body) => http().post('/vouchers/redeem', body).then((r) => r.data),
    cancel: (id) => http().post(`/vouchers/${id}/cancel`)
};
export const inventoryApi = {
    items: (storeId, onlyLowStock = false) => http().get('/inventory/items', { params: { storeId, onlyLowStock } }).then((r) => r.data),
    createItem: (body) => http().post('/inventory/items', body).then((r) => r.data),
    updateItem: (id, body) => http().put(`/inventory/items/${id}`, body).then((r) => r.data),
    movements: (itemId, take = 50) => http().get('/inventory/movements', { params: { itemId, take } }).then((r) => r.data),
    move: (body) => http().post('/inventory/movements', body).then((r) => r.data)
};
export const memberPackagesApi = {
    list: (params) => http().get('/member-packages', { params }).then((r) => r.data),
    create: (body) => http().post('/member-packages', body).then((r) => r.data),
    cancel: (id) => http().post(`/member-packages/${id}/cancel`)
};
export const servicePackagesApi = {
    list: (includeInactive = false) => http().get('/service-packages', { params: { includeInactive } }).then((r) => r.data),
    create: (body) => http().post('/service-packages', body).then((r) => r.data),
    update: (id, body) => http().put(`/service-packages/${id}`, body).then((r) => r.data),
    remove: (id) => http().delete(`/service-packages/${id}`)
};
export const reviewsApi = {
    list: (params) => http().get('/reviews', { params }).then((r) => r.data),
    technicianSummary: (params) => http().get('/reviews/technician-summary', { params }).then((r) => r.data),
    me: () => http().get('/reviews/me').then((r) => r.data)
};
export const payrollApi = {
    profiles: (storeId) => http().get('/payroll/profiles', { params: { storeId } }).then((r) => r.data),
    profile: (userId) => http().get(`/payroll/profiles/${userId}`).then((r) => r.data),
    upsertProfile: (userId, body) => http().put(`/payroll/profiles/${userId}`, body).then((r) => r.data),
    periods: (storeId, year) => http().get('/payroll/periods', { params: { storeId, year } }).then((r) => r.data),
    period: (id) => http().get(`/payroll/periods/${id}`).then((r) => r.data),
    generate: (storeId, year, month, remark) => http().post('/payroll/periods', { storeId, year, month, remark }).then((r) => r.data),
    lock: (id, remark) => http().post(`/payroll/periods/${id}/lock`, { remark }).then((r) => r.data),
    markPaid: (id) => http().post(`/payroll/periods/${id}/mark-paid`).then((r) => r.data),
    removeDraft: (id) => http().delete(`/payroll/periods/${id}`),
    updateItem: (id, overtimeHours, attendanceBonusOverride, remark) => http().patch(`/payroll/items/${id}`, { overtimeHours, attendanceBonusOverride, remark }).then((r) => r.data),
    addAdjustment: (itemId, kind, amount, reason) => http().post(`/payroll/items/${itemId}/adjustments`, { kind, amount, reason }).then((r) => r.data),
    removeAdjustment: (itemId, adjId) => http().delete(`/payroll/items/${itemId}/adjustments/${adjId}`).then((r) => r.data),
    me: (take = 6) => http().get('/payroll/me', { params: { take } }).then((r) => r.data)
};
export const complaintsApi = {
    list: (params) => http().get('/complaints', { params }).then((r) => r.data),
    get: (id) => http().get(`/complaints/${id}`).then((r) => r.data),
    create: (body) => http().post('/complaints', body).then((r) => r.data),
    resolve: (id, body) => http().patch(`/complaints/${id}/resolve`, body).then((r) => r.data),
    cancel: (id) => http().post(`/complaints/${id}/cancel`).then((r) => r.data)
};
export const schedulesApi = {
    list: (storeId, from, to) => http().get('/schedules', { params: { storeId, from, to } }).then((r) => r.data),
    create: (body) => http().post('/schedules', body).then((r) => r.data),
    remove: (id) => http().delete(`/schedules/${id}`),
    leaves: (params) => http().get('/schedules/leaves', { params }).then((r) => r.data),
    submitLeave: (body) => http().post('/schedules/leaves', body).then((r) => r.data),
    approveLeave: (id, approve, reason) => http().post(`/schedules/leaves/${id}/approve`, { approve, reason }).then((r) => r.data)
};
