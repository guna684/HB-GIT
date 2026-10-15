import axiosClient from './axiosClient';

/**
 * =============================================================================
 * BIRTHDAY API FUNCTIONS
 * =============================================================================
 * These functions are the bridge between your React components and the backend
 * ASP.NET Core API at /api/birthdays.
 * 
 * Notice:
 * - createBirthday & updateBirthday accept a FormData object because they support
 *   uploading an optional profile picture file (.jpg, .png, .webp).
 */

/**
 * 1. GET /api/birthdays
 * Fetches all birthdays belonging to the logged-in user, sorted by upcoming first.
 */
export const getAllBirthdays = async () => {
  const response = await axiosClient.get('/birthdays');
  return response.data;
};

/**
 * 2. GET /api/birthdays/upcoming
 * Fetches only birthdays occurring within the next 30 days (0 to 30 days remaining).
 */
export const getUpcomingBirthdays = async () => {
  const response = await axiosClient.get('/birthdays/upcoming');
  return response.data;
};

/**
 * 3. GET /api/birthdays/{id}
 * Fetches a single birthday record by its integer ID (used on the Edit page).
 */
export const getBirthdayById = async (id) => {
  const response = await axiosClient.get(`/birthdays/${id}`);
  return response.data;
};

/**
 * 4. POST /api/birthdays
 * Creates a new birthday record. Accepts FormData containing person details and optional photo.
 */
export const createBirthday = async (formData) => {
  // Note for Beginners: Do NOT manually set 'Content-Type': 'multipart/form-data'!
  // Axios and the browser automatically set the correct header WITH the unique boundary string.
  const response = await axiosClient.post('/birthdays', formData);
  return response.data;
};

/**
 * 5. PUT /api/birthdays/{id}
 * Updates an existing birthday record by ID.
 */
export const updateBirthday = async (id, formData) => {
  const response = await axiosClient.put(`/birthdays/${id}`, formData);
  return response.data;
};

/**
 * 6. DELETE /api/birthdays/{id}
 * Deletes a birthday record and removes its uploaded photo from the server.
 */
export const deleteBirthday = async (id) => {
  const response = await axiosClient.delete(`/birthdays/${id}`);
  return response.data;
};

