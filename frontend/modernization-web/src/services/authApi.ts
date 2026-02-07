import apiClient from './apiClient';

type LoginResponse = {
  token: string;
};

type LoginPayload = {
  email: string;
  password: string;
};

type RegisterPayload = {
  email: string;
  password: string;
};

export async function loginUser(payload: LoginPayload): Promise<LoginResponse> {
  const response = await apiClient.post<LoginResponse>('/api/v1/auth/login', payload);
  return response.data;
}

export async function registerUser(payload: RegisterPayload): Promise<void> {
  await apiClient.post('/api/v1/auth/register', payload);
}
