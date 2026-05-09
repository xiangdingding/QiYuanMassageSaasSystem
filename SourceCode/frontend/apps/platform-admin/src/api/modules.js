import { http } from './http';
export const authApi = {
    login: (req) => http().post('/auth/login', req).then((r) => r.data)
};
export const plansApi = {
    list: (includeInactive = false) => http().get('/plans', { params: { includeInactive } }).then((r) => r.data),
    create: (body) => http().post('/plans', body).then((r) => r.data),
    update: (id, body) => http().put(`/plans/${id}`, body).then((r) => r.data),
    remove: (id) => http().delete(`/plans/${id}`)
};
export const tenantsApi = {
    list: (q) => http().get('/tenants', { params: q }).then((r) => r.data),
    get: (id) => http().get(`/tenants/${id}`).then((r) => r.data),
    create: (body) => http().post('/tenants', body).then((r) => r.data),
    updateStatus: (id, status) => http().patch(`/tenants/${id}/status`, { status })
};
export const dashboardApi = {
    platform: () => http().get('/dashboard/platform').then((r) => r.data)
};
export const subscriptionsApi = {
    activateOffline: (req) => http().post('/subscriptions/activate-offline', req).then((r) => r.data),
    history: (tenantId) => http().get(`/subscriptions/history`, { params: { tenantId } }).then((r) => r.data)
};
