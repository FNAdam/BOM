import { api } from './client';
import type { SalesChannelDto, UpsertSalesChannelDto } from './types';

const BASE = '/api/sales-channels';

export const salesChannelsApi = {
  list: () => api.get<SalesChannelDto[]>(BASE),
  get: (id: number) => api.get<SalesChannelDto>(`${BASE}/${id}`),
  create: (dto: UpsertSalesChannelDto) => api.post<SalesChannelDto>(BASE, dto),
  update: (id: number, dto: UpsertSalesChannelDto) => api.put<void>(`${BASE}/${id}`, dto),
  remove: (id: number) => api.del(`${BASE}/${id}`),
};
