import axios from 'axios';

export const apiClient = axios.create({
  baseURL: 'https://localhost:7217',
  headers: { 'Content-Type': 'application/json' }
});