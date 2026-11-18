import { api } from './client';
import type {
  SalesOrderDto,
  SalesOrderListDto,
  SalesOrderStatus,
  SalesReceiptDto,
  UpsertSalesOrderDto,
} from './types';

const BASE = '/api/sales-orders';

export interface SalesOrderListParams {
  status?: SalesOrderStatus;
  salesChannelId?: number;
}

export const salesOrdersApi = {
  list: (params?: SalesOrderListParams) => {
    const q = new URLSearchParams();
    if (params?.status) q.set('status', params.status);
    if (params?.salesChannelId) q.set('salesChannelId', String(params.salesChannelId));
    const qs = q.toString();
    return api.get<SalesOrderListDto[]>(qs ? `${BASE}?${qs}` : BASE);
  },
  get: (id: number) => api.get<SalesOrderDto>(`${BASE}/${id}`),
  receipt: (id: number) => api.get<SalesReceiptDto>(`${BASE}/${id}/receipt`),
  create: (dto: UpsertSalesOrderDto) => api.post<SalesOrderDto>(BASE, dto),
  update: (id: number, dto: UpsertSalesOrderDto) => api.put<void>(`${BASE}/${id}`, dto),
  remove: (id: number) => api.del(`${BASE}/${id}`),
  complete: (id: number) => api.post<SalesOrderDto>(`${BASE}/${id}/complete`, {}),
  cancel: (id: number) => api.post<SalesOrderDto>(`${BASE}/${id}/cancel`, {}),
};
