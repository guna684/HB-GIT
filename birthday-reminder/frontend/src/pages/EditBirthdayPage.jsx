import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { getBirthdayById, updateBirthday } from '../api/birthdayApi';
import BirthdayForm from '../components/BirthdayForm';

/**
 * =============================================================================
 * PAGE: EditBirthdayPage
 * =============================================================================
 * This page lets users edit an existing birthday reminder.
 * 
 * How it works:
 * 1. Uses useParams() to read the :id from the URL (e.g. /edit/5 -> id is "5").
 * 2. Fetches the existing birthday details from GET /api/birthdays/5.
 * 3. Passes the fetched birthday data into <BirthdayForm initialData={birthday} />.
 * 4. When the user updates fields and submits, it calls PUT /api/birthdays/5.
 * 5. On success, redirects the user back to the Dashboard.
 */
const EditBirthdayPage = () => {
  // Extract ID parameter from the route URL
  const { id } = useParams();
  const navigate = useNavigate();

  // Component state
  const [birthday, setBirthday] = useState(null);         // Existing birthday data from server
  const [loading, setLoading] = useState(true);            // True while fetching existing record
  const [isSubmitting, setIsSubmitting] = useState(false); // True while updating record
  const [fetchError, setFetchError] = useState('');        // Error if birthday cannot be found
  const [serverError, setServerError] = useState('');      // Error if update fails

  // ---------------------------------------------------------------------------
  // Load existing birthday details when page opens
  // ---------------------------------------------------------------------------
  useEffect(() => {
    const loadBirthday = async () => {
      setLoading(true);
      setFetchError('');

      try {
        const data = await getBirthdayById(id);
        setBirthday(data);
      } catch {
        setFetchError('Could not load this birthday reminder. It may have been deleted.');
      } finally {
        setLoading(false);
      }
    };

    loadBirthday();
  }, [id]);

  // ---------------------------------------------------------------------------
  // Handle form submission to update the birthday
  // ---------------------------------------------------------------------------
  const handleFormSubmit = async (formData) => {
    setIsSubmitting(true);
    setServerError('');

    try {
      // Send PUT request to backend
      await updateBirthday(id, formData);

      // On success, redirect back to Dashboard
      navigate('/');
    } catch (err) {
      const message = err.response?.data?.message || 'Failed to update birthday. Please try again.';
      setServerError(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  // ---------------------------------------------------------------------------
  // Conditional UI states: Loading Spinner vs Not Found Card vs Edit Form
  // ---------------------------------------------------------------------------
  if (loading) {
    return (
      <div className="spinner-container">
        <div className="spinner"></div>
        <p>Loading birthday details...</p>
      </div>
    );
  }

  if (fetchError || !birthday) {
    return (
      <div className="card" style={{ textAlign: 'center', padding: '40px', maxWidth: '500px', margin: '40px auto' }}>
        <div style={{ fontSize: '2.5rem', marginBottom: '12px' }}>⚠️</div>
        <h3>{fetchError || 'Birthday not found'}</h3>
        <p style={{ color: 'var(--text-secondary)', margin: '12px 0 20px' }}>
          We could not find the birthday you are looking for.
        </p>
        <Link to="/" className="btn btn-primary">
          Back to Dashboard
        </Link>
      </div>
    );
  }

  return (
    <div className="form-page-container">
      {/* Page Title & Subtitle */}
      <div className="form-page-header">
        <h1>Edit Birthday</h1>
        <p>Updating details for {birthday.personName}</p>
      </div>

      {/* Shared Birthday Form with prefilled initial data */}
      <BirthdayForm
        key={birthday.id}
        initialData={birthday}
        onSubmit={handleFormSubmit}
        isSubmitting={isSubmitting}
        submitButtonText="Update Birthday"
        serverError={serverError}
      />
    </div>
  );
};

export default EditBirthdayPage;

