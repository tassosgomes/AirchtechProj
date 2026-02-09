import axios from 'axios';
import * as Sentry from '@sentry/react';
import { authStorage } from './authStorage';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
  timeout: 30000,
});

const sentryEnabled = Boolean(import.meta.env.VITE_SENTRY_DSN) && import.meta.env.PROD;

apiClient.interceptors.request.use((config) => {
  const token = authStorage.getToken();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

apiClient.interceptors.response.use(
  (response) => {
    const requestId = response.headers['x-request-id'];

    if (requestId && sentryEnabled) {
      Sentry.setTag('requestId', requestId);
    }

    return response;
  },
  (error) => {
    const requestId = error?.response?.headers?.['x-request-id'];

    if (requestId && sentryEnabled) {
      Sentry.setTag('requestId', requestId);
    }

    return Promise.reject(error);
  }
);

export default apiClient;
