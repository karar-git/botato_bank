// API_BASE reads from Vite env at build time.
// In dev: proxied via vite.config.js to localhost:5000
// In prod: set VITE_API_URL env var in Vercel to your Railway backend URL
const API_BASE = import.meta.env.VITE_API_URL || '/api';

function getToken() {
  return localStorage.getItem('corebank_token');
}

async function request(path, options = {}) {
  const token = getToken();
  const headers = {
    ...(options.isFormData ? {} : { 'Content-Type': 'application/json' }),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...options.headers,
  };

  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  });

  if (!res.ok) {
    const error = await res.json().catch(() => ({ message: res.statusText }));
    throw { status: res.status, ...error };
  }

  // Handle empty responses (204, etc.)
  const text = await res.text();
  return text ? JSON.parse(text) : null;
}

export const api = {
  // Auth
  register: (data) => {
    const formData = new FormData();
    formData.append('fullName', data.fullName);
    formData.append('email', data.email);
    formData.append('password', data.password);
    formData.append('idCardFront', data.idCardFront);
    formData.append('idCardBack', data.idCardBack);
    if (data.nationalIdNumber) formData.append('nationalIdNumber', data.nationalIdNumber);
    return request('/auth/register', { method: 'POST', body: formData, isFormData: true });
  },
  login: (data) => request('/auth/login', { method: 'POST', body: JSON.stringify(data) }),
  me: () => request('/auth/me'),

  // Accounts
  getAccounts: () => request('/accounts'),
  createAccount: (data) => request('/accounts', { method: 'POST', body: JSON.stringify(data) }),
  getAccount: (id) => request(`/accounts/${id}`),
  reconcile: (id) => request(`/accounts/${id}/reconcile`),

  // Wallet
  deposit: (accountId, data) =>
    request(`/accounts/${accountId}/deposit`, { method: 'POST', body: JSON.stringify(data) }),
  withdraw: (accountId, data) =>
    request(`/accounts/${accountId}/withdraw`, { method: 'POST', body: JSON.stringify(data) }),

  // Transfers
  transfer: (data) => request('/transfers', { method: 'POST', body: JSON.stringify(data) }),

  // Chat
  chat: (message, conversationHistory = []) =>
    request('/chat', {
      method: 'POST',
      body: JSON.stringify({ message, conversationHistory }),
    }),

  // Transactions
  getTransactions: (accountId, params = {}) => {
    const query = new URLSearchParams(params).toString();
    return request(`/accounts/${accountId}/transactions?${query}`);
  },
  exportCsv: (accountId) => `${API_BASE}/accounts/${accountId}/transactions/export/csv`,
  exportXlsx: (accountId) => `${API_BASE}/accounts/${accountId}/transactions/export/xlsx`,

  // Admin
  getPendingUsers: () => request('/admin/pending-users'),
  getAllUsers: () => request('/admin/users'),
  getUser: (userId) => request(`/admin/users/${userId}`),
  updateKyc: (userId, data) =>
    request(`/admin/users/${userId}/kyc`, { method: 'POST', body: JSON.stringify(data) }),
  getStats: () => request('/admin/stats'),
  getAdminTransactions: (params = {}) => {
    const query = new URLSearchParams(params).toString();
    return request(`/admin/transactions?${query}`);
  },
  getAdminTransfers: (params = {}) => {
    const query = new URLSearchParams(params).toString();
    return request(`/admin/transfers?${query}`);
  },

  // Employee
  uploadCsv: (file) => {
    const formData = new FormData();
    formData.append('file', file);
    return request('/employee/csv-upload', { method: 'POST', body: formData, isFormData: true });
  },
};

export function setToken(token) {
  localStorage.setItem('corebank_token', token);
}

export function clearToken() {
  localStorage.removeItem('corebank_token');
}

export function isAuthenticated() {
  return !!getToken();
}
