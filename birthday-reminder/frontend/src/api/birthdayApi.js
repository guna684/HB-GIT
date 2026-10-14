import axiosClient from './axiosClient';

/**
 * Birthday API calls for CRUD operations.
 * Notice that Create and Update endpoints accept a FormData object
 * to support uploading an optional photo.
 */

// Get all birthdays belonging to the logged-in user
export const getAllBirthdays = async () => {
  const response = await axiosClient.get('/birthdays');
  return response.data;
};

// Get upcoming birthdays in the next 30 days
export const getUpcomingBirthdays = async () => {
  const response = await axiosClient.get('/birthdays/upcoming');
  return response.data;
};

// Get a single birthday by ID
export const getBirthdayById = async (id) => {
  const response = await axiosClient.get(`/birthdays/${id}`);
  return response.data;
};

// Create a new birthday with optional photo
export const createBirthday = async (formData) => {
  const response = await axiosClient.post('/birthdays', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
  return response.data;
};

// Update an existing birthday with optional new photo
export const updateBirthday = async (id, formData) => {
  const response = await axiosClient.put(`/birthdays/${id}`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
  return response.data;
};

// Delete a birthday and its photo
export const deleteBirthday = async (id) => {
  const response = await axiosClient.delete(`/birthdays/${id}`);
  return response.data;
};
