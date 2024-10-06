import React, { useState, useEffect } from 'react';
import { BrowserRouter as Router, Route, Routes, Navigate } from 'react-router-dom';
import Login from './components/Login';
import Register from './components/Register';
import MapComponent from './components/MapComponent';
import Navigation from './components/Navigation';
import 'bootstrap/dist/css/bootstrap.min.css';
import AdminComponent from './components/AdminComponent';
import { getUserRoles } from './services/api';


const App = () => {
    // Käytä tilaa tarkistaaksesi, onko käyttäjä kirjautunut
    const [isAuthenticated, setIsAuthenticated] = useState(!!localStorage.getItem('token'));
    const [userRoles, setUserRoles] = useState([]);

    // Päivitetään autentikointitilaa, jos token muuttuu
    useEffect(() => {
        const token = localStorage.getItem('token');
        setIsAuthenticated(!!token);  // Jos token on olemassa, käyttäjä on kirjautunut
        if (token) {
            // Fetch user roles if authenticated
            getUserRoles().then(roles => {
                setUserRoles(Array.isArray(roles) ? roles : []); // Ensure roles is an array
            }).catch(error => {
                console.error('Failed to fetch user roles:', error);
                setUserRoles([]); // Set to empty array on error
            });
        } else {
            setUserRoles([]); // Set to empty array if not authenticated
        }
    }, []);

    const isAdmin = userRoles.includes('Administrator');

    return (
        <Router>
            <Navigation isAuthenticated={isAuthenticated} isAdmin={isAdmin} />
            <div className="container-fluid">
                <Routes>
                    <Route path="/register" element={<Register />} />
                    <Route path="/login" element={<Login setIsAuthenticated={setIsAuthenticated} />} />
                    <Route path="/admin" element={isAuthenticated && isAdmin ? <AdminComponent /> : <Navigate to="/login" />} />
                    <Route path="/map" element={isAuthenticated ? <MapComponent /> : <Navigate to="/login" />} />
                    <Route path="*" element={<Navigate to={isAuthenticated ? "/map" : "/login"} />} />
                </Routes>
            </div>
        </Router>
    );
};

export default App;
