import { api } from './client';
import type { PackLevelDto, UpsertPackLevelDto } from './types';

const base = (itemId: number) => `/api/items/${itemId}/pack-levels`;

export const packLevelsApi = {
  list: (itemId: number) => api.get<PackLevelDto[]>(base(itemId)),
  create: (itemId: number, dto: UpsertPackLevelDto) => api.post<PackLevelDto>(base(itemId), dto),
  update: (itemId: number, id: number, dto: UpsertPackLevelDto) =>
    api.put<void>(`${base(itemId)}/${id}`, dto),
  remove: (itemId: number, id: number) => api.del(`${base(itemId)}/${id}`),
};
