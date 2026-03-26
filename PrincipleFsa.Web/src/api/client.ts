import axios from 'axios';

export const apiClient = axios.create({
  baseURL: 'https://localhost:7217', // Match your .NET 9 HTTPS port
  headers: { 'Content-Type': 'application/json' }
});