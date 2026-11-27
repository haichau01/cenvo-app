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
