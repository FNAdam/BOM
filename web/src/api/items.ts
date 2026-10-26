import { api } from './client';
import type { ItemDto, ItemType, UpsertItemDto } from './types';

const BASE = '/api/items';

export const itemsApi = {
  list: () => api.get<ItemDto[]>(BASE),
  get: (id: number) => api.get<ItemDto>(`${BASE}/${id}`),
  create: (dto: UpsertItemDto) => api.post<ItemDto>(BASE, dto),
  update: (id: number, dto: UpsertItemDto) => api.put<void>(`${BASE}/${id}`, dto),
  remove: (id: number) => api.del(`${BASE}/${id}`),
  // Saran kode berikutnya berdasarkan tipe, format "PREFIX-0000" (RW/PK/SF/FG).
  nextCode: (type: ItemType) =>
    api.get<{ code: string }>(`${BASE}/next-code?type=${encodeURIComponent(type)}`),
};
