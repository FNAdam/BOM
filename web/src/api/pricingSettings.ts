import { api } from './client';
import type { PricingSettingDto, UpsertPricingSettingDto } from './types';

const BASE = '/api/pricing-settings';

export const pricingSettingsApi = {
  get: () => api.get<PricingSettingDto>(BASE),
  update: (dto: UpsertPricingSettingDto) => api.put<PricingSettingDto>(BASE, dto),
};
