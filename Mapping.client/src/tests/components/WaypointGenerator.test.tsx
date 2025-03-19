import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import WaypointGenerator from '../../components/WaypointGenerator';
import { WaypointService } from '../../services/waypointService';
import { LatLng } from 'leaflet';

// Mock the waypoint service
jest.mock('../../services/waypointService');

describe('WaypointGenerator Component', () => {
  const mockWaypointService = WaypointService as jest.Mocked<typeof WaypointService>;
  
  beforeEach(() => {
    // Clear all mocks before each test
    jest.clearAllMocks();
    
    // Mock the generateWaypoints method
    mockWaypointService.generateWaypoints.mockResolvedValue([
      { lat: 60.0, lng: 24.0, alt: 100, speed: 10, index: 1, action: 'takePhoto' },
      { lat: 60.5, lng: 24.5, alt: 100, speed: 10, index: 2, action: 'takePhoto' },
      { lat: 61.0, lng: 25.0, alt: 100, speed: 10, index: 3, action: 'takePhoto' }
    ]);
  });

  test('renders waypoint form with correct inputs', () => {
    render(<WaypointGenerator />);
    
    // Check that all the required form elements exist
    expect(screen.getByLabelText(/altitude/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/speed/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/line spacing/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/photo interval/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /generate/i })).toBeInTheDocument();
  });

  test('generates waypoints for rectangle shape on form submit', async () => {
    render(<WaypointGenerator />);
    
    // Fill the form
    fireEvent.change(screen.getByLabelText(/altitude/i), { target: { value: '100' } });
    fireEvent.change(screen.getByLabelText(/speed/i), { target: { value: '10' } });
    fireEvent.change(screen.getByLabelText(/line spacing/i), { target: { value: '100' } });
    fireEvent.change(screen.getByLabelText(/photo interval/i), { target: { value: '3' } });
    
    // Select rectangle shape
    fireEvent.click(screen.getByLabelText(/rectangle/i));
    
    // Submit the form
    fireEvent.click(screen.getByRole('button', { name: /generate/i }));
    
    // Wait for the service to be called
    await waitFor(() => {
      expect(mockWaypointService.generateWaypoints).toHaveBeenCalled();
      expect(mockWaypointService.generateWaypoints).toHaveBeenCalledWith(
        expect.objectContaining({
          altitude: 100,
          speed: 10,
          lineSpacing: 100,
          photoInterval: 3,
          boundsType: 'rectangle'
        })
      );
    });
  });

  test('generates waypoints for polyline shape on form submit', async () => {
    render(<WaypointGenerator />);
    
    // Fill the form
    fireEvent.change(screen.getByLabelText(/altitude/i), { target: { value: '100' } });
    fireEvent.change(screen.getByLabelText(/speed/i), { target: { value: '10' } });
    fireEvent.change(screen.getByLabelText(/line spacing/i), { target: { value: '100' } });
    fireEvent.change(screen.getByLabelText(/photo interval/i), { target: { value: '3' } });
    
    // Select polyline shape
    fireEvent.click(screen.getByLabelText(/polyline/i));
    
    // Add a test polyline with 3 points
    const mockPolyline = [
      new LatLng(60.0, 24.0),
      new LatLng(60.5, 24.5),
      new LatLng(61.0, 25.0)
    ];
    
    // Set the bounds (normally this would be done through map interaction)
    const component = screen.getByTestId('waypoint-generator');
    // Assuming there's a method to set bounds programmatically for testing
    component.setBounds(mockPolyline);
    
    // Submit the form
    fireEvent.click(screen.getByRole('button', { name: /generate/i }));
    
    // Wait for the service to be called
    await waitFor(() => {
      expect(mockWaypointService.generateWaypoints).toHaveBeenCalled();
      expect(mockWaypointService.generateWaypoints).toHaveBeenCalledWith(
        expect.objectContaining({
          altitude: 100,
          speed: 10,
          lineSpacing: 100,
          photoInterval: 3,
          boundsType: 'polyline',
          bounds: expect.arrayContaining([
            expect.objectContaining({ lat: 60.0, lng: 24.0 }),
            expect.objectContaining({ lat: 60.5, lng: 24.5 }),
            expect.objectContaining({ lat: 61.0, lng: 25.0 })
          ])
        })
      );
    });
  });

  test('toggles useEndpointsOnly property correctly', async () => {
    render(<WaypointGenerator />);
    
    // Fill basic form data
    fireEvent.change(screen.getByLabelText(/altitude/i), { target: { value: '100' } });
    fireEvent.change(screen.getByLabelText(/speed/i), { target: { value: '10' } });
    
    // Select polyline shape
    fireEvent.click(screen.getByLabelText(/polyline/i));
    
    // Check the useEndpointsOnly checkbox
    fireEvent.click(screen.getByLabelText(/use endpoints only/i));
    
    // Submit the form
    fireEvent.click(screen.getByRole('button', { name: /generate/i }));
    
    // Wait for the service to be called
    await waitFor(() => {
      expect(mockWaypointService.generateWaypoints).toHaveBeenCalledWith(
        expect.objectContaining({
          useEndpointsOnly: true,
          boundsType: 'polyline'
        })
      );
    });
  });

  test('displays generated waypoints after successful API call', async () => {
    render(<WaypointGenerator />);
    
    // Fill the form and submit
    fireEvent.change(screen.getByLabelText(/altitude/i), { target: { value: '100' } });
    fireEvent.change(screen.getByLabelText(/speed/i), { target: { value: '10' } });
    fireEvent.click(screen.getByLabelText(/polyline/i));
    fireEvent.click(screen.getByRole('button', { name: /generate/i }));
    
    // Wait for the results to be displayed
    await waitFor(() => {
      expect(screen.getByText(/generated waypoints/i)).toBeInTheDocument();
      expect(screen.getByText(/1:/)).toBeInTheDocument(); // First waypoint index
      expect(screen.getByText(/2:/)).toBeInTheDocument(); // Second waypoint index
      expect(screen.getByText(/3:/)).toBeInTheDocument(); // Third waypoint index
    });
    
    // Check waypoint details are displayed
    expect(screen.getByText(/60.0, 24.0/)).toBeInTheDocument();
    expect(screen.getByText(/60.5, 24.5/)).toBeInTheDocument();
    expect(screen.getByText(/61.0, 25.0/)).toBeInTheDocument();
  });

  test('handles error in waypoint generation', async () => {
    // Mock the service to throw an error
    mockWaypointService.generateWaypoints.mockRejectedValue(new Error('API Error'));
    
    render(<WaypointGenerator />);
    
    // Fill the form and submit
    fireEvent.change(screen.getByLabelText(/altitude/i), { target: { value: '100' } });
    fireEvent.change(screen.getByLabelText(/speed/i), { target: { value: '10' } });
    fireEvent.click(screen.getByRole('button', { name: /generate/i }));
    
    // Wait for the error message
    await waitFor(() => {
      expect(screen.getByText(/error generating waypoints/i)).toBeInTheDocument();
    });
  });
}); 