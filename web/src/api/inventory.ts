import { api } from './client';
import type { InventoryBalanceDto, UpsertInventoryBalanceDto } from './types';

const BASE = '/api/inventory';

export const inventoryApi = {
  list: () => api.get<InventoryBalanceDto[]>(BASE),
  upsert: (itemId: number, dto: UpsertInventoryBalanceDto) =>
    api.put<InventoryBalanceDto>(`${BASE}/${itemId}`, dto),
};
