
    import React, { useState } from 'react';
    import { useNavigate } from 'react-router-dom';
    import { register } from '../services/api';

    const Register = () => {
        const [email, setEmail] = useState('');
        const [password, setPassword] = useState('');
        const [firstName, setFirstName] = useState('');
        const [lastName, setLastName] = useState('');
        const [error, setError] = useState('');
        const navigate = useNavigate();

        const handleRegister = async (e) => {
            e.preventDefault();
            try {
                await register({ email, password, firstName, lastName });
                navigate('/login');
            } catch (err) {
                setError('Registration failed');
            }
        };

        return (
            <div className="container">
                <h2>Register</h2>
                <form onSubmit={handleRegister}>
                    <div className="mb-3">
                        <label htmlFor="email" className="form-label">Email</label>
                        <input
                            type="email"
                            className="form-control"
                            id="email"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            required
                        />
                    </div>
                    <div className="mb-3">
                        <label htmlFor="password" className="form-label">Password</label>
                        <input
                            type="password"
                            className="form-control"
                            id="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            required
                        />
                    </div>
                    <div className="mb-3">
                        <label htmlFor="firstName" className="form-label">First Name</label>
                        <input
                            type="text"
                            className="form-control"
                            id="firstName"
                            value={firstName}
                            onChange={(e) => setFirstName(e.target.value)}
                            required
                        />
                    </div>
                    <div className="mb-3">
                        <label htmlFor="lastName" className="form-label">Last Name</label>
                        <input
                            type="text"
                            className="form-control"
                            id="lastName"
                            value={lastName}
                            onChange={(e) => setLastName(e.target.value)}
                            required
                        />
                    </div>
                    {error && <div className="alert alert-danger">{error}</div>}
                    <button type="submit" className="btn btn-primary">Register</button>
                </form>
            </div>
        );
    };

    export default Register;
