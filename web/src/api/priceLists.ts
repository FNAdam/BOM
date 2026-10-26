import { api } from './client';
import type {
  PriceListDto,
  PriceListListItemDto,
  PriceListStatus,
  SavePriceListRequest,
} from './types';

const BASE = '/api/price-lists';

export interface PriceListListParams {
  productItemId?: number;
  status?: PriceListStatus;
}

export const priceListsApi = {
  list: (params?: PriceListListParams) => {
    const q = new URLSearchParams();
    if (params?.productItemId) q.set('productItemId', String(params.productItemId));
    if (params?.status) q.set('status', params.status);
    const qs = q.toString();
    return api.get<PriceListListItemDto[]>(qs ? `${BASE}?${qs}` : BASE);
  },
  get: (id: number) => api.get<PriceListDto>(`${BASE}/${id}`),
  create: (req: SavePriceListRequest) => api.post<PriceListDto>(BASE, req),
  update: (id: number, req: SavePriceListRequest) => api.put<PriceListDto>(`${BASE}/${id}`, req),
  remove: (id: number) => api.del(`${BASE}/${id}`),
  activate: (id: number) => api.post<PriceListDto>(`${BASE}/${id}/activate`, {}),
};
