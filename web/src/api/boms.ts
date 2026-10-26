import { api } from './client';
import type {
  BomDto,
  BomExplosionNodeDto,
  BomListItemDto,
  BomStatus,
  UpsertBomDto,
} from './types';

const BASE = '/api/boms';

export interface BomListParams {
  productItemId?: number;
  status?: BomStatus;
}

export const bomsApi = {
  list: (params?: BomListParams) => {
    const q = new URLSearchParams();
    if (params?.productItemId) q.set('productItemId', String(params.productItemId));
    if (params?.status) q.set('status', params.status);
    const qs = q.toString();
    return api.get<BomListItemDto[]>(qs ? `${BASE}?${qs}` : BASE);
  },
  get: (id: number) => api.get<BomDto>(`${BASE}/${id}`),
  create: (dto: UpsertBomDto) => api.post<BomDto>(BASE, dto),
  update: (id: number, dto: UpsertBomDto) => api.put<void>(`${BASE}/${id}`, dto),
  remove: (id: number) => api.del(`${BASE}/${id}`),
  activate: (id: number) => api.post<BomDto>(`${BASE}/${id}/activate`, {}),
  newVersion: (id: number) => api.post<BomDto>(`${BASE}/${id}/new-version`, {}),
  explode: (id: number, qty?: number) =>
    api.get<BomExplosionNodeDto>(
      qty != null ? `${BASE}/${id}/explode?qty=${qty}` : `${BASE}/${id}/explode`,
    ),
};
