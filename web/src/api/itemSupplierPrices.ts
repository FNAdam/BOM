import { api } from './client';
import type { ItemSupplierPriceDto, UpsertItemSupplierPriceDto } from './types';

const BASE = '/api/item-supplier-prices';

export const itemSupplierPricesApi = {
  list: () => api.get<ItemSupplierPriceDto[]>(BASE),
  get: (id: number) => api.get<ItemSupplierPriceDto>(`${BASE}/${id}`),
  create: (dto: UpsertItemSupplierPriceDto) => api.post<ItemSupplierPriceDto>(BASE, dto),
  update: (id: number, dto: UpsertItemSupplierPriceDto) => api.put<void>(`${BASE}/${id}`, dto),
  remove: (id: number) => api.del(`${BASE}/${id}`),
};
