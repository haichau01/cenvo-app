import type { components } from './schema'

type Schemas = components['schemas']

// Short names for frequently used DTOs. Types are generated from Swagger (`npm run gen:api`), never hand-written.
export type TokenResponse = Schemas['Cenvo.Api.Auth.TokenResponse']
export type UserIdentity = Schemas['Cenvo.Modules.Organization.Application.Auth.UserIdentity']
export type BranchSummary = Schemas['Cenvo.Modules.Organization.Application.Auth.BranchSummary']
export type LoginRequest = Schemas['Cenvo.Api.Endpoints.LoginRequest']
