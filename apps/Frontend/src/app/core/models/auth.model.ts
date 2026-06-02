export interface UserCredentials {
  username: string;
  password: string;
}

export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
}

export interface RefreshTokenPayload {
  refreshToken: string;
}

export interface UpdateUsernamePayload {
  username: string;
}

export interface UpdateTenantTimezonePayload {
  timezone: string;
}

export interface Tenant {
  id: string;
  name: string;
  timezone: string;
}

export interface CreateTenantPayload {
  name: string;
}

export interface UpdateTenantNamePayload {
  name: string;
}
