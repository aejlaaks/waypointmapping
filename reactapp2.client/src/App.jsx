import React from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import MapComponent from './components/MapComponent';
import SimpleMapComponent from './components/SimpleMapComponent';
import 'bootstrap/dist/css/bootstrap.min.css';

const App = () => {
    return (
        <Router>
            <div className="app-container">
                <Routes>
                    <Route path="/map" element={<MapComponent />} />
                    <Route path="/simple-map" element={<SimpleMapComponent />} />
                    <Route path="*" element={<MapComponent />} />
                </Routes>
            </div>
        </Router>
    );
};

export default App;
