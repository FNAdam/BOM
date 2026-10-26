import { api } from './client';
import type { CostBreakdownDto, CostSnapshotDto } from './types';

export const costingApi = {
  breakdown: (itemId: number) => api.get<CostBreakdownDto>(`/api/items/${itemId}/cost-breakdown`),
  rollup: (itemId: number) => api.post<CostBreakdownDto>(`/api/costing/rollup/${itemId}`, {}),
  snapshots: (itemId?: number) =>
    api.get<CostSnapshotDto[]>(
      itemId != null ? `/api/costing/snapshots?itemId=${itemId}` : '/api/costing/snapshots',
    ),
};
