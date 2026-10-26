import { api } from './client';
import type { SupplierDto, UpsertSupplierDto } from './types';

const BASE = '/api/suppliers';

export const suppliersApi = {
  list: () => api.get<SupplierDto[]>(BASE),
  get: (id: number) => api.get<SupplierDto>(`${BASE}/${id}`),
  create: (dto: UpsertSupplierDto) => api.post<SupplierDto>(BASE, dto),
  update: (id: number, dto: UpsertSupplierDto) => api.put<void>(`${BASE}/${id}`, dto),
  remove: (id: number) => api.del(`${BASE}/${id}`),
};
