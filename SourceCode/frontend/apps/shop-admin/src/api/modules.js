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
    consumptionHistory: (id) => http().get(`/members/${id}/orders`).then((r) => r.data)
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
    resetPassword: (id, newPassword) => http().post(`/staff/${id}/reset-password`, { newPassword })
};
export const commissionsApi = {
    list: (serviceId, technicianId) => http().get('/commission-rules', { params: { serviceId, technicianId } }).then((r) => r.data),
    create: (body) => http().post('/commission-rules', body).then((r) => r.data),
    update: (id, body) => http().put(`/commission-rules/${id}`, body).then((r) => r.data),
    remove: (id) => http().delete(`/commission-rules/${id}`)
};
export const reportsApi = {
    daily: (storeId, date) => http().get('/reports/daily', { params: { storeId, date } }).then((r) => r.data),
    technicianPerformance: (storeId, from, to) => http().get('/reports/technician-performance', { params: { storeId, from, to } }).then((r) => r.data)
};
export const subscriptionsApi = {
    me: () => http().get('/subscriptions/me').then((r) => r.data)
};
