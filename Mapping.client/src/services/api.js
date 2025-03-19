import axios from 'axios'; 
const apiBaseUrl = (typeof import.meta !== 'undefined' && import.meta.env && import.meta.env.VITE_API_BASE_URL) || '';

if (!import.meta || !import.meta.env || !import.meta.env.VITE_API_BASE_URL) {
    console.warn('VITE_API_BASE_URL is not defined or empty. Using default value: ');
}

const api = axios.create({
  baseURL: `${apiBaseUrl}/api/`,
});

// API-kutsu waypointtien generointiin
export const generateWaypoints = async (request) => {
  try {
    const response = await api.post('/waypoints/generate', request); 
    return response.data; // Palauttaa listan generoituja waypointteja
  }
  catch (error) {
    throw error.response?.data || 'Failed to generate waypoints';
  }
};

// API-kutsu waypointin päivittämiseen
export const updateWaypoint = async (id, updatedWaypoint) => {
  try {
    const response = await api.put(`/waypoints/${id}`, updatedWaypoint); 
    return response.data; // Palauttaa päivitetyn waypointin
  }
  catch (error) {
    throw error.response?.data || 'Failed to update waypoint';
  }
};

// API-kutsu waypointin poistamiseen
export const deleteWaypoint = async (id) => {
  try {
    const response = await api.delete(`/waypoints/${id}`); 
    return response.data; // Palauttaa poistettuun waypointtiin liittyvät tiedot
  }
  catch (error) {
    throw error.response?.data || 'Failed to delete waypoint';
  }
};
