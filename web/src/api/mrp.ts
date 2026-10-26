import { api } from './client';
import type { MrpRequestDto, MrpResultDto } from './types';

export const mrpApi = {
  explode: (request: MrpRequestDto) => api.post<MrpResultDto>('/api/mrp/explode', request),
};
