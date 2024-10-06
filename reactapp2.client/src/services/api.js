import axios from 'axios'; 
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || '';// Luodaan axios-instanssi, jossa on perus URL-osoit

const api = axios.create({
  baseURL: `${apiBaseUrl}/api/`,  // Oikea tapa käyttää template stringiä
});

// Lisää autentikointipyyntöihin JWT-tunnus otsikkoon
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token'); // Hae token localStoragesta
  if (token) {
      config.headers.Authorization = `Bearer ${token}`;  // Oikea interpolointi ilman ylimääräisiä välilyöntejä
      console.log("Authorization Header Set:", config.headers.Authorization); // Log for debugging

  }
  return config;
}, (error) => {
  return Promise.reject(error);
});

// API-kutsu hyväksymättömien käyttäjien hakemiselle
export const fetchPendingUsers = async () => {
    try {
        const response = await api.get('/admin/pendingApprovals');
        return response.data; // Oletetaan, että käyttäjätiedot ovat response.data.users:ssa
    } catch (error) {
        throw error.response?.data || 'Failed to fetch pending users';
    }
};

// API-kutsu käyttäjän hyväksymiselle
export const approveUser = async (userId) => {
    try {
        await api.post(`/admin/approveUser?userId=${userId}`);
    } catch (error) {
        throw error.response?.data || 'Failed to approve user';
    }
};

// API-kutsu rekisteröinnille
export const register = async (userData) => {
  try {
    const response
    = await
    api.post('/user/register',
    userData); return
    response.data;
  }
  catch (error)
  {
    throw error.response?.data
    || 'Registration failed';
  }
};

export const login = async (email, password) => {
  try {
    const response = await api.post('/user/login', { email, password });

    // Konsolitulostus nähdäksesi koko vastauksen
    console.log("Full response:", response);

    // Tarkista että response sisältää dataa
    if (response && response.data) {
      console.log("Token:", response.data.token);
      return response.data;  // Palautetaan kaikki response.data
    } else {
      throw new Error("No data in response");
    }
  } catch (error) {
    console.error("Error during login:", error);
    throw error.response?.data || 'Login failed';
  }
};

// API-kutsu käyttäjän roolien hakemiselle
export const getUserRoles = async () => {
    try {
        const response = await api.get('/user/roles');
        return response.data.roles; // Oletetaan, että roolit ovat response.data.roles
    } catch (error) {
        throw error.response?.data || 'Failed to fetch user roles';
    }
};

// API-kutsu käyttäjän tietojen hakemiselle
export const getCurrentUser = async () => {
  try {
    const response
    = await
    api.get('/user/me'); return
    response.data;
  }
  catch (error)
  {
    throw error.response?.data
    || 'Failed to fetch user data';
  }
};

// API-kutsu waypointtien generointiin
export const generateWaypoints = async (request) => {
  try {
    const response
    = await
    api.post('/waypoints/generatePoints',
    request); return
    response.data; // Palauttaa listan generoituja waypointteja
  }
  catch (error)
  {
    throw error.response?.data
    || 'Failed to generate waypoints';
  }
};

// API-kutsu waypointin päivittämiseen
export const updateWaypoint = async (id, updatedWaypoint) => {
  try {
    const response
    = await
    api.put(`/waypoints/$ { id }
    `,
    updatedWaypoint); return
    response.data; // Palauttaa päivitetyn waypointin
  }
  catch (error)
  {
    throw error.response?.data
    || 'Failed to update waypoint';
  }
};

// API-kutsu waypointin poistamiseen
export const deleteWaypoint = async (id) => {
  try {
    const response
    = await
    api.delete(`/waypoints/$ { id }
    `); return
    response.data; // Palauttaa poistettuun waypointtiin liittyvät tiedot
  }
  catch (error)
  {
    throw error.response?.data
    || 'Failed to delete waypoint';
  }
};
