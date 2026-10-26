import { api } from './client';
import type { StockMovementDto } from './types';

const BASE = '/api/stock-movements';

export const stockMovementsApi = {
  list: (itemId?: number) =>
    api.get<StockMovementDto[]>(itemId != null ? `${BASE}?itemId=${itemId}` : BASE),
};
