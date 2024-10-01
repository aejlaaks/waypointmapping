import React from 'react';
import { BrowserRouter as Router, Route, Routes, Navigate } from 'react-router-dom';
import Login from './components/Login';
import Register from './components/Register';
import MapComponent from './components/MapComponent';
import Navigation from './components/Navigation';
import 'bootstrap/dist/css/bootstrap.min.css';

const App = () => {
    // Tarkistetaan, onko k‰ytt‰j‰ kirjautunut
    const isAuthenticated = !!localStorage.getItem('token');

    return (
        <Router>
            <Navigation />
            <div className="container-fluid">
                <Routes>
                    <Route path="/register" element={<Register />} />
                    <Route path="/login" element={<Login />} />
                    {/* Jos k‰ytt‰j‰ on kirjautunut sis‰‰n, ohjaa karttan‰kym‰‰n, muutoin kirjautumissivulle */}
                    <Route
                        path="/map"
                        element={isAuthenticated ? <MapComponent /> : <Navigate to="/login" />}
                    />
                    {/* Jos k‰ytt‰j‰ menee tuntemattomaan reittiin, ohjaa joko kirjautumissivulle tai karttaan */}
                    <Route path="*" element={<Navigate to={isAuthenticated ? "/map" : "/login"} />} />
                </Routes>
            </div>
        </Router>
    );
};

export default App;
