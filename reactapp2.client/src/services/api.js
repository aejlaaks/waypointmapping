import axios from 'axios';

// Luodaan axios-instanssi, jossa on perus URL-osoite
const api = axios.create({
    baseURL: 'http://localhost:5037/api', // Tämä pitäisi olla ASP.NET Core -API:si perus-URL
});

// Lisää autentikointipyyntöihin JWT-tunnus otsikkoon
api.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem('token'); // Hae token localStorage:sta
        if (token) {
            config.headers.Authorization = `Bearer ${token}`; // Lisää Authorization-otsikko
        }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// API-kutsu rekisteröinnille
export const register = async (userData) => {
    try {
        const response = await api.post('/user/register', userData);
        return response.data;
    } catch (error) {
        throw error.response?.data || 'Registration failed';
    }
};

// API-kutsu kirjautumiselle
export const login = async (email, password) => {
    try {
        const response = await api.post('/user/login', { email, password });
        return response.data; // Oletetaan, että tässä palautetaan token
    } catch (error) {
        throw error.response?.data || 'Login failed';
    }
};

// API-kutsu käyttäjän tietojen hakemiselle
export const getCurrentUser = async () => {
    try {
        const response = await api.get('/user/me');
        return response.data;
    } catch (error) {
        throw error.response?.data || 'Failed to fetch user data';
    }
};

// API-kutsu waypointtien generointiin
export const generateWaypoints = async (request) => {
    try {
        const response = await api.post('/waypoints/generatePoints', request);
        return response.data; // Palauttaa listan generoituja waypointteja
    } catch (error) {
        throw error.response?.data || 'Failed to generate waypoints';
    }
};

// API-kutsu waypointin päivittämiseen
export const updateWaypoint = async (id, updatedWaypoint) => {
    try {
        const response = await api.put(`/waypoints/${id}`, updatedWaypoint);
        return response.data; // Palauttaa päivitetyn waypointin
    } catch (error) {
        throw error.response?.data || 'Failed to update waypoint';
    }
};

// API-kutsu waypointin poistamiseen
export const deleteWaypoint = async (id) => {
    try {
        const response = await api.delete(`/waypoints/${id}`);
        return response.data; // Palauttaa poistettuun waypointtiin liittyvät tiedot
    } catch (error) {
        throw error.response?.data || 'Failed to delete waypoint';
    }
};
