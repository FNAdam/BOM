import { api } from './client';
import type { UomDto, UpsertUomDto } from './types';

const BASE = '/api/uoms';

export const uomsApi = {
  list: () => api.get<UomDto[]>(BASE),
  get: (id: number) => api.get<UomDto>(`${BASE}/${id}`),
  create: (dto: UpsertUomDto) => api.post<UomDto>(BASE, dto),
  update: (id: number, dto: UpsertUomDto) => api.put<void>(`${BASE}/${id}`, dto),
  remove: (id: number) => api.del(`${BASE}/${id}`),
};
