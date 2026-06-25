import axios from 'axios';

// 与其它端一致：相对 /api，由部署处反代到后端（开发期走 vite proxy）
const http = axios.create({ baseURL: '/api', timeout: 15000 });

export interface CreateConsultationRequest {
  contactName?: string | null;
  phone: string;
  content: string;
  source?: string | null;
}

export const consultationApi = {
  submit: (req: CreateConsultationRequest) =>
    http.post<{ success: boolean; message: string }>('/consultations', req).then((r) => r.data)
};
