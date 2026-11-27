import type { components } from './schema'

type Schemas = components['schemas']

// Short names for frequently used DTOs. Types are generated from Swagger (`npm run gen:api`), never hand-written.
export type TokenResponse = Schemas['Cenvo.Api.Auth.TokenResponse']
export type UserIdentity = Schemas['Cenvo.Modules.Organization.Application.Auth.UserIdentity']
export type BranchSummary = Schemas['Cenvo.Modules.Organization.Application.Auth.BranchSummary']
export type LoginRequest = Schemas['Cenvo.Api.Endpoints.LoginRequest']

// Cafe (POS)
export type OrderDto = Schemas['Cenvo.Modules.Cafe.Application.Orders.OrderDto']
export type OrderLine = Schemas['Cenvo.Modules.Cafe.Application.Orders.LineDto']
export type AddLineInput = Schemas['Cenvo.Modules.Cafe.Application.Orders.AddLineInput']
export type CreateOrderInput = Schemas['Cenvo.Modules.Cafe.Application.Orders.CreateOrderInput']
export type TenderedPayment = Schemas['Cenvo.Modules.Cafe.Domain.Calculations.TenderedPayment']
export type PaymentMethod = Schemas['Cenvo.Modules.Cafe.Domain.PaymentMethod']
export type KitchenStatus = Schemas['Cenvo.Modules.Cafe.Domain.KitchenStatus']
export type MenuItem = Schemas['Cenvo.Modules.Cafe.Application.Setup.MenuItemDto']
export type AreaDto = Schemas['Cenvo.Modules.Cafe.Application.Setup.AreaDto']
export type TableDto = Schemas['Cenvo.Modules.Cafe.Application.Setup.TableDto']
export type ShiftSummary = Schemas['Cenvo.Modules.Cafe.Application.Shifts.ShiftSummary']
export type KitchenTicket = Schemas['Cenvo.Modules.Cafe.Application.Kitchen.KitchenTicket']

// Inventory
export type StockDocumentDto = Schemas['Cenvo.Modules.Inventory.Application.Documents.StockDocumentDto']
export type StockDocumentLineDto = Schemas['Cenvo.Modules.Inventory.Application.Documents.StockDocumentLineDto']
export type StockDocumentInput = Schemas['Cenvo.Modules.Inventory.Application.Documents.StockDocumentInput']
export type StockDocumentType = Schemas['Cenvo.Modules.Inventory.Domain.StockDocumentType']
export type DocumentStatus = Schemas['Cenvo.SharedKernel.DocumentStatus']
export type TransferState = Schemas['Cenvo.Modules.Inventory.Domain.TransferState']
export type StockWarning = Schemas['Cenvo.Modules.Inventory.Posting.StockWarning']
export type BalanceDto = Schemas['Cenvo.Modules.Inventory.Application.Stock.BalanceDto']
export type StockCardDto = Schemas['Cenvo.Modules.Inventory.Application.Stock.StockCardDto']
export type WarehouseDto = Schemas['Cenvo.Modules.Inventory.Application.Warehouses.WarehouseDto']
export type SupplierDto = Schemas['Cenvo.Modules.Inventory.Application.Suppliers.SupplierDto']
export type ProductDto = Schemas['Cenvo.Modules.Catalog.Application.Products.ProductDto']

// Reports
export type SalesSummary = Schemas['Cenvo.Modules.Cafe.Application.Reports.SalesSummary']
export type DailyRevenue = Schemas['Cenvo.Modules.Cafe.Application.Reports.DailyRevenue']
export type TopItem = Schemas['Cenvo.Modules.Cafe.Application.Reports.TopItem']
export type ShiftDto = Schemas['Cenvo.Modules.Cafe.Application.Shifts.ShiftDto']
export type ConsumptionDto = Schemas['Cenvo.Modules.Inventory.Application.Stock.ConsumptionDto']
export type ReceiptDto = Schemas['Cenvo.Modules.Cafe.Application.Orders.ReceiptDto']
