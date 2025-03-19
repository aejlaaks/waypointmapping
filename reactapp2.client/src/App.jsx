import React from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import SimpleMapComponent from './components/SimpleMapComponent';
import Navigation from './components/Navigation';
import 'bootstrap/dist/css/bootstrap.min.css';

const App = () => {
    return (
        <Router>
            <Navigation />
            <div className="container-fluid">
                <Routes>
                    <Route path="/map" element={<SimpleMapComponent />} />
                    <Route path="*" element={<SimpleMapComponent />} />
                </Routes>
            </div>
        </Router>
    );
};

export default App;
