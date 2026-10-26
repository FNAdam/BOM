import { api } from './client';
import type { PricingResultDto, SimulatePricingRequest } from './types';

const BASE = '/api/pricing';

export const pricingApi = {
  simulate: (req: SimulatePricingRequest) => api.post<PricingResultDto>(`${BASE}/simulate`, req),
};
