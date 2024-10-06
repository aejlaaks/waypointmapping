import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { Spinner, Table, Button, Alert } from 'react-bootstrap';
import { fetchPendingUsers, approveUser } from '../services/api';


const AdminComponent = () => {
    const [pendingUsers, setPendingUsers] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    useEffect(() => {
        fetchPendingUsersData();
    }, []);

    // Funktio hyväksymättömien käyttäjien hakemiseen
    const fetchPendingUsersData = async () => {
        setLoading(true);
        try {
            const users = await fetchPendingUsers();
            setPendingUsers(users);
            setError('');
        } catch (err) {
            setError('Failed to fetch pending users');
        } finally {
            setLoading(false);
        }
    };

    // Funktio käyttäjän hyväksymiseen
    const handleApproveUser = async (userId) => {
        setLoading(true);
        try {
            await approveUser(userId);
            // Poista hyväksytty käyttäjä listasta
            setPendingUsers(pendingUsers.filter(user => user.id !== userId));
            setError('');
        } catch (err) {
            setError('Failed to approve user');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="container mt-5">
            <h1>Admin Dashboard</h1>
            {loading ? (
                <Spinner animation="border" role="status">
                    <span className="visually-hidden">Loading...</span>
                </Spinner>
            ) : (
                <div>
                    {error && <Alert variant="danger">{error}</Alert>}
                    <h2 className="my-4">Pending Users for Approval</h2>
                    {pendingUsers.length === 0 ? (
                        <Alert variant="info">No pending users</Alert>
                    ) : (
                        <Table striped bordered hover>
                            <thead>
                                <tr>
                                    <th>Email</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {pendingUsers.map(user => (
                                    <tr key={user.id}>
                                        <td>{user.email}</td>
                                        <td>
                                            <Button
                                                variant="success"
                                                onClick={() => approveUser(user.id)}
                                            >
                                                Approve
                                            </Button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </Table>
                    )}
                </div>
            )}
        </div>
    );
};

export default AdminComponent;
