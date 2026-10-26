// Tipe TypeScript yang mencerminkan DTO backend.
// Enum diserialisasi sebagai string (union types).

export type ItemType = 'RawMaterial' | 'Packaging' | 'SemiFinished' | 'FinishedGood';
export type UomType = 'Weight' | 'Volume' | 'Count';
export type CostingMethod = 'Standard' | 'MovingAverage';

export const ITEM_TYPES: ItemType[] = ['RawMaterial', 'Packaging', 'SemiFinished', 'FinishedGood'];
export const UOM_TYPES: UomType[] = ['Weight', 'Volume', 'Count'];
export const COSTING_METHODS: CostingMethod[] = ['Standard', 'MovingAverage'];

// --- Item ---
export interface ItemDto {
  id: number;
  code: string;
  name: string;
  type: ItemType;
  baseUomId: number;
  baseUomCode: string;
  categoryId: number | null;
  categoryName: string | null;
  standardMaterialCost: number;
  costingMethod: CostingMethod;
  density: number | null;
  isActive: boolean;
}

export interface UpsertItemDto {
  code: string;
  name: string;
  type: ItemType;
  baseUomId: number;
  categoryId: number | null;
  standardMaterialCost: number;
  costingMethod: CostingMethod;
  density: number | null;
  isActive: boolean;
}

// --- UOM ---
export interface UomDto {
  id: number;
  code: string;
  name: string;
  type: UomType;
  factorToBase: number;
  isBase: boolean;
}

export interface UpsertUomDto {
  code: string;
  name: string;
  type: UomType;
  factorToBase: number;
  isBase: boolean;
}

// --- Supplier ---
export interface SupplierDto {
  id: number;
  code: string;
  name: string;
  leadTimeDays: number | null;
  isActive: boolean;
}

export interface UpsertSupplierDto {
  code: string;
  name: string;
  leadTimeDays: number | null;
  isActive: boolean;
}

// --- ItemCategory ---
export interface ItemCategoryDto {
  id: number;
  name: string;
  description: string | null;
}

export interface UpsertItemCategoryDto {
  name: string;
  description: string | null;
}

// --- ItemSupplierPrice ---
export interface ItemSupplierPriceDto {
  id: number;
  itemId: number;
  itemName: string;
  supplierId: number;
  supplierName: string;
  uomId: number;
  uomCode: string;
  price: number;
  currency: string;
  minOrderQty: number;
  effectiveFrom: string | null;
  effectiveTo: string | null;
  isPreferred: boolean;
}

export interface UpsertItemSupplierPriceDto {
  itemId: number;
  supplierId: number;
  uomId: number;
  price: number;
  currency: string;
  minOrderQty: number;
  effectiveFrom: string | null;
  effectiveTo: string | null;
  isPreferred: boolean;
}

// --- BOM ---
export type BomStatus = 'Draft' | 'Active' | 'Obsolete';
export const BOM_STATUSES: BomStatus[] = ['Draft', 'Active', 'Obsolete'];

export interface BomOperationDto {
  id: number;
  sequence: number;
  name: string;
  laborCostPerOutputUnit: number;
  overheadRate: number;
}

export interface BomComponentDto {
  id: number;
  componentItemId: number;
  componentCode: string;
  componentName: string;
  quantity: number;
  uomId: number;
  uomCode: string;
  scrapPercent: number;
  sequence: number;
  isOptional: boolean;
  hasBom: boolean;
}

export interface BomDto {
  id: number;
  productItemId: number;
  productCode: string;
  productName: string;
  version: number;
  status: BomStatus;
  outputQuantity: number;
  outputUomId: number;
  outputUomCode: string;
  yieldPercent: number;
  effectiveFrom: string | null;
  effectiveTo: string | null;
  notes: string | null;
  components: BomComponentDto[];
  operations: BomOperationDto[];
}

export interface BomListItemDto {
  id: number;
  productItemId: number;
  productCode: string;
  productName: string;
  version: number;
  status: BomStatus;
  outputQuantity: number;
  outputUomId: number;
  outputUomCode: string;
  effectiveFrom: string | null;
  effectiveTo: string | null;
  componentCount: number;
}

export interface UpsertBomComponentDto {
  componentItemId: number;
  quantity: number;
  uomId: number;
  scrapPercent: number;
  sequence: number;
  isOptional: boolean;
  notes: string | null;
}

export interface UpsertBomOperationDto {
  sequence: number;
  name: string;
  laborCostPerOutputUnit: number;
  overheadRate: number;
}

export interface UpsertBomDto {
  productItemId: number;
  outputQuantity: number;
  outputUomId: number;
  yieldPercent: number;
  effectiveFrom: string | null;
  effectiveTo: string | null;
  notes: string | null;
  components: UpsertBomComponentDto[];
  operations: UpsertBomOperationDto[];
}

export interface BomExplosionNodeDto {
  itemId: number;
  itemCode: string;
  itemName: string;
  itemType: ItemType;
  quantity: number;
  uomCode: string;
  scrapPercent: number;
  effectiveQuantity: number;
  level: number;
  hasBom: boolean;
  children: BomExplosionNodeDto[];
}

// --- Costing (Fase 3) ---
export interface CostLineDto {
  itemId: number;
  code: string;
  name: string;
  quantityPerOutput: number;
  uomCode: string;
  scrapPercent: number;
  unitCost: number;
  extendedCost: number;
  hasBom: boolean;
  children: CostLineDto[] | null;
}

export interface CostBreakdownDto {
  itemId: number;
  itemCode: string;
  itemName: string;
  outputUomCode: string;
  materialCost: number;
  laborCost: number;
  overheadCost: number;
  totalCost: number;
  isPurchased: boolean;
  components: CostLineDto[];
}

export interface CostSnapshotDto {
  id: number;
  itemId: number;
  itemCode: string;
  itemName: string;
  asOfDate: string;
  materialCost: number;
  laborCost: number;
  overheadCost: number;
  totalCost: number;
  outputUomId: number;
  outputUomCode: string;
  sourceBomId: number | null;
  bomVersion: number | null;
}

// --- Inventory (Fase 4) ---
export interface InventoryBalanceDto {
  itemId: number;
  itemCode: string;
  itemName: string;
  uomCode: string;
  qtyOnHand: number;
  averageCost: number;
}

export interface UpsertInventoryBalanceDto {
  qtyOnHand: number;
  averageCost: number | null;
}

// --- MRP (Fase 4) ---
export type MrpSource = 'Buy' | 'Make';

export interface MrpDemandInputDto {
  productItemId: number;
  quantity: number;
}

export interface MrpRequestDto {
  demands: MrpDemandInputDto[];
}

export interface MrpDemandLineDto {
  productItemId: number;
  productCode: string;
  productName: string;
  quantity: number;
  uomCode: string;
}

export interface MrpLineDto {
  itemId: number;
  code: string;
  name: string;
  itemType: ItemType;
  source: MrpSource;
  grossQty: number;
  onHandQty: number;
  netQty: number;
  uomCode: string;
  supplierId: number | null;
  supplierName: string | null;
  unitCost: number | null;
  minOrderQty: number | null;
  suggestedOrderQty: number | null;
  leadTimeDays: number | null;
  extendedCost: number | null;
}

export interface MrpResultDto {
  demands: MrpDemandLineDto[];
  lines: MrpLineDto[];
  totalPurchaseCost: number;
}

// --- Production & COGS (Fase 5) ---
export type ProductionStatus = 'Open' | 'Closed' | 'Cancelled';

export type StockMovementType =
  | 'Receipt'
  | 'Issue'
  | 'ProductionIssue'
  | 'ProductionReceipt'
  | 'Adjustment';

export interface ProductionConsumptionDto {
  id: number;
  componentItemId: number;
  componentCode: string;
  componentName: string;
  uomCode: string;
  plannedQty: number;
  actualQty: number;
  actualUnitCost: number;
  actualCost: number;
}

export interface ProductionOrderDto {
  id: number;
  bomId: number;
  bomVersion: number;
  productItemId: number;
  productCode: string;
  productName: string;
  outputUomCode: string;
  batchNo: string;
  plannedQty: number;
  actualQty: number | null;
  status: ProductionStatus;
  startDate: string | null;
  endDate: string | null;
  notes: string | null;
  consumptions: ProductionConsumptionDto[];
}

export interface ProductionOrderListDto {
  id: number;
  batchNo: string;
  bomId: number;
  productItemId: number;
  productCode: string;
  productName: string;
  outputUomCode: string;
  plannedQty: number;
  actualQty: number | null;
  status: ProductionStatus;
  startDate: string | null;
  endDate: string | null;
}

export interface UpsertProductionOrderDto {
  bomId: number;
  plannedQty: number;
  batchNo: string;
  startDate: string | null;
  endDate: string | null;
  notes: string | null;
}

export interface RecordConsumptionLineDto {
  componentItemId: number;
  actualQty: number;
}

export interface RecordConsumptionDto {
  lines: RecordConsumptionLineDto[];
}

export interface CloseProductionDto {
  actualQty: number;
}

export interface CogsVarianceLineDto {
  componentId: number;
  code: string;
  name: string;
  uomCode: string;
  actualQty: number;
  actualUnitCost: number;
  actualCost: number;
  stdQtyAllowed: number;
  stdUnitCost: number;
  priceVariance: number;
  usageVariance: number;
}

export interface CogsReportDto {
  orderId: number;
  actualQty: number;
  materialCost: number;
  conversionCost: number;
  cogsTotal: number;
  cogsUnit: number;
  standardTotal: number;
  totalVariance: number;
  materialVariance: number;
  yieldVariance: number;
  lines: CogsVarianceLineDto[];
}

export interface StockMovementDto {
  id: number;
  itemId: number;
  itemCode: string;
  itemName: string;
  type: StockMovementType;
  qty: number;
  unitCost: number;
  movementDate: string;
  refType: string | null;
  refId: number | null;
  notes: string | null;
}

// --- Pricing (Fase 6) ---
export type PricingMethod = 'Margin' | 'Markup' | 'Manual';
export type CostBasis = 'Standard' | 'LastCOGS';
export type TaxMode = 'Exclusive' | 'Inclusive';
export type PriceListStatus = 'Draft' | 'Active' | 'Obsolete';
export type RoundingRule = 'None' | 'Nearest100' | 'Nearest500' | 'Psych990';

export const PRICING_METHODS: PricingMethod[] = ['Margin', 'Markup', 'Manual'];
export const COST_BASES: CostBasis[] = ['Standard', 'LastCOGS'];
export const TAX_MODES: TaxMode[] = ['Exclusive', 'Inclusive'];
export const PRICE_LIST_STATUSES: PriceListStatus[] = ['Draft', 'Active', 'Obsolete'];
export const ROUNDING_RULES: RoundingRule[] = ['None', 'Nearest100', 'Nearest500', 'Psych990'];

export interface SalesChannelDto {
  id: number;
  code: string;
  name: string;
  defaultMarginPercent: number;
  sequence: number;
  isActive: boolean;
}

export interface UpsertSalesChannelDto {
  code: string;
  name: string;
  defaultMarginPercent: number;
  sequence: number;
  isActive: boolean;
}

export interface PackLevelDto {
  id: number;
  finishedItemId: number;
  name: string;
  qtyPerBase: number;
  barcode: string | null;
  sequence: number;
  isDefaultSales: boolean;
}

export interface UpsertPackLevelDto {
  name: string;
  qtyPerBase: number;
  barcode: string | null;
  sequence: number;
  isDefaultSales: boolean;
}

export interface PricingSettingDto {
  defaultTargetMarginPercent: number;
  defaultTaxPercent: number;
  minMarginPercent: number;
  roundingRule: RoundingRule;
}

export interface UpsertPricingSettingDto {
  defaultTargetMarginPercent: number;
  defaultTaxPercent: number;
  minMarginPercent: number;
  roundingRule: RoundingRule;
}

export interface ChannelMarginOverrideDto {
  salesChannelId: number;
  marginPercent: number;
}

export interface PackDiscountDto {
  packLevelId: number;
  discountPercent: number;
}

export interface SimulatePricingRequest {
  productItemId: number;
  pricingMethod: PricingMethod;
  costBasis: CostBasis;
  taxMode: TaxMode;
  targetPercent?: number | null;
  manualBasePrice?: number | null;
  floorCost?: number | null;
  minMarginPercent?: number | null;
  taxPercent?: number | null;
  roundingRule?: RoundingRule | null;
  fixedCostPerPeriod?: number | null;
  channelMargins?: ChannelMarginOverrideDto[] | null;
  packDiscounts?: PackDiscountDto[] | null;
}

export interface PricingPackLevelDto {
  id: number;
  name: string;
  qtyPerBase: number;
}

export interface PricingCellDto {
  tierName: string;
  salesChannelId: number | null;
  packLevelId: number;
  packLevelName: string;
  qtyPerBase: number;
  netPrice: number;
  taxAmount: number;
  grossPrice: number;
}

export interface PricingResultDto {
  productItemId: number;
  cost: number;
  costBasis: CostBasis;
  basePrice: number;
  marginPct: number;
  contributionMargin: number;
  breakEvenQty: number | null;
  floorViolation: boolean;
  tiers: string[];
  packLevels: PricingPackLevelDto[];
  cells: PricingCellDto[];
}

export interface PriceListLineDto {
  id: number;
  salesChannelId: number | null;
  salesChannelName: string | null;
  packLevelId: number;
  packLevelName: string;
  tierName: string;
  marginPercent: number;
  netPrice: number;
  taxAmount: number;
  grossPrice: number;
  contributionMargin: number;
}

export interface PriceListDto {
  id: number;
  productItemId: number;
  productCode: string;
  productName: string;
  pricingMethod: PricingMethod;
  costBasis: CostBasis;
  targetPercent: number;
  manualBasePrice: number | null;
  floorCost: number | null;
  taxMode: TaxMode;
  taxPercent: number;
  effectiveFrom: string | null;
  effectiveTo: string | null;
  status: PriceListStatus;
  notes: string | null;
  lines: PriceListLineDto[];
}

export interface PriceListListItemDto {
  id: number;
  productItemId: number;
  productCode: string;
  productName: string;
  pricingMethod: PricingMethod;
  costBasis: CostBasis;
  targetPercent: number;
  status: PriceListStatus;
  effectiveFrom: string | null;
  effectiveTo: string | null;
  lineCount: number;
}

export interface SavePriceListRequest {
  simulation: SimulatePricingRequest;
  notes?: string | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
}

// --- Dashboard (Fase 7) ---
export interface DashboardMarginItemDto {
  productItemId: number;
  productCode: string;
  productName: string;
  itemType: ItemType;
  standardCost: number;
  exFactoryPrice: number;
  contributionMargin: number;
  marginPct: number;
  belowMinMargin: boolean;
  hasActivePriceList: boolean;
}

export interface DashboardMarginSummaryDto {
  productCount: number;
  avgMarginPct: number;
  belowMinCount: number;
}

export interface DashboardMarginDto {
  minMarginPercent: number;
  items: DashboardMarginItemDto[];
  summary: DashboardMarginSummaryDto;
}
