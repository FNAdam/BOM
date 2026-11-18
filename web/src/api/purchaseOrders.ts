import { api } from './client';
import type {
  PurchaseOrderDto,
  PurchaseOrderListDto,
  PurchaseOrderStatus,
  ReceivePurchaseOrderDto,
  UpsertPurchaseOrderDto,
} from './types';

const BASE = '/api/purchase-orders';

export interface PurchaseOrderListParams {
  status?: PurchaseOrderStatus;
  supplierId?: number;
}

export const purchaseOrdersApi = {
  list: (params?: PurchaseOrderListParams) => {
    const q = new URLSearchParams();
    if (params?.status) q.set('status', params.status);
    if (params?.supplierId) q.set('supplierId', String(params.supplierId));
    const qs = q.toString();
    return api.get<PurchaseOrderListDto[]>(qs ? `${BASE}?${qs}` : BASE);
  },
  get: (id: number) => api.get<PurchaseOrderDto>(`${BASE}/${id}`),
  create: (dto: UpsertPurchaseOrderDto) => api.post<PurchaseOrderDto>(BASE, dto),
  update: (id: number, dto: UpsertPurchaseOrderDto) => api.put<void>(`${BASE}/${id}`, dto),
  remove: (id: number) => api.del(`${BASE}/${id}`),
  send: (id: number) => api.post<PurchaseOrderDto>(`${BASE}/${id}/send`, {}),
  receive: (id: number, dto: ReceivePurchaseOrderDto) =>
    api.post<PurchaseOrderDto>(`${BASE}/${id}/receive`, dto),
  cancel: (id: number) => api.post<PurchaseOrderDto>(`${BASE}/${id}/cancel`, {}),
};
