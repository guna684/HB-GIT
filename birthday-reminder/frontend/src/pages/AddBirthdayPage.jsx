import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { createBirthday } from '../api/birthdayApi';
import BirthdayForm from '../components/BirthdayForm';

/**
 * Page allowing the user to create a new birthday reminder with an optional photo.
 */
const AddBirthdayPage = () => {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [serverError, setServerError] = useState('');
  const navigate = useNavigate();

  const handleFormSubmit = async (formData) => {
    setIsSubmitting(true);
    setServerError('');

    try {
      await createBirthday(formData);
      navigate('/');
    } catch (err) {
      const msg = err.response?.data?.message || 'Failed to save birthday. Please try again.';
      setServerError(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="form-page-container">
      <div className="form-page-header">
        <h1>Add a Birthday</h1>
        <p>Save details for someone you care about</p>
      </div>

      <BirthdayForm
        onSubmit={handleFormSubmit}
        isSubmitting={isSubmitting}
        submitButtonText="Save Birthday"
        serverError={serverError}
      />
    </div>
  );
};

export default AddBirthdayPage;
