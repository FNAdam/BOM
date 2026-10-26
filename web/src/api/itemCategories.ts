import { api } from './client';
import type { ItemCategoryDto, UpsertItemCategoryDto } from './types';

const BASE = '/api/item-categories';

export const itemCategoriesApi = {
  list: () => api.get<ItemCategoryDto[]>(BASE),
  get: (id: number) => api.get<ItemCategoryDto>(`${BASE}/${id}`),
  create: (dto: UpsertItemCategoryDto) => api.post<ItemCategoryDto>(BASE, dto),
  update: (id: number, dto: UpsertItemCategoryDto) => api.put<void>(`${BASE}/${id}`, dto),
  remove: (id: number) => api.del(`${BASE}/${id}`),
};
