import { api } from './client';
import type { DashboardMarginDto } from './types';

export const dashboardApi = {
  margin: () => api.get<DashboardMarginDto>('/api/dashboard/margin'),
};
