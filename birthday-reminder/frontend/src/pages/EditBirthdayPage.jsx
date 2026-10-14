import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { getBirthdayById, updateBirthday } from '../api/birthdayApi';
import BirthdayForm from '../components/BirthdayForm';

/**
 * Page allowing the user to edit an existing birthday reminder and optionally update their photo.
 */
const EditBirthdayPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();

  const [birthday, setBirthday] = useState(null);
  const [loading, setLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [fetchError, setFetchError] = useState('');
  const [serverError, setServerError] = useState('');

  // Fetch the current birthday data by ID
  useEffect(() => {
    const loadBirthday = async () => {
      setLoading(true);
      setFetchError('');
      try {
        const data = await getBirthdayById(id);
        setBirthday(data);
      } catch (err) {
        setFetchError('Could not load this birthday reminder. It may have been deleted.');
      } finally {
        setLoading(false);
      }
    };

    loadBirthday();
  }, [id]);

  const handleFormSubmit = async (formData) => {
    setIsSubmitting(true);
    setServerError('');

    try {
      await updateBirthday(id, formData);
      navigate('/');
    } catch (err) {
      const msg = err.response?.data?.message || 'Failed to update birthday. Please try again.';
      setServerError(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

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
      <div className="form-page-header">
        <h1>Edit Birthday</h1>
        <p>Updating details for {birthday.personName}</p>
      </div>

      <BirthdayForm
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
