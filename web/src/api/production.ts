import { api } from './client';
import type {
  CloseProductionDto,
  CogsReportDto,
  ProductionOrderDto,
  ProductionOrderListDto,
  ProductionStatus,
  RecordConsumptionDto,
  UpsertProductionOrderDto,
} from './types';

const BASE = '/api/production-orders';

export interface ProductionListParams {
  status?: ProductionStatus;
  productItemId?: number;
}

export const productionApi = {
  list: (params?: ProductionListParams) => {
    const q = new URLSearchParams();
    if (params?.status) q.set('status', params.status);
    if (params?.productItemId) q.set('productItemId', String(params.productItemId));
    const qs = q.toString();
    return api.get<ProductionOrderListDto[]>(qs ? `${BASE}?${qs}` : BASE);
  },
  get: (id: number) => api.get<ProductionOrderDto>(`${BASE}/${id}`),
  create: (dto: UpsertProductionOrderDto) => api.post<ProductionOrderDto>(BASE, dto),
  update: (id: number, dto: UpsertProductionOrderDto) => api.put<void>(`${BASE}/${id}`, dto),
  remove: (id: number) => api.del(`${BASE}/${id}`),
  recordConsumption: (id: number, dto: RecordConsumptionDto) =>
    api.post<ProductionOrderDto>(`${BASE}/${id}/consumption`, dto),
  close: (id: number, dto: CloseProductionDto) => api.post<CogsReportDto>(`${BASE}/${id}/close`, dto),
  cogs: (id: number) => api.get<CogsReportDto>(`${BASE}/${id}/cogs`),
};
