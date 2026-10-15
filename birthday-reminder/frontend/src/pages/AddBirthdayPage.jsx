import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { createBirthday } from '../api/birthdayApi';
import BirthdayForm from '../components/BirthdayForm';

/**
 * =============================================================================
 * PAGE: AddBirthdayPage
 * =============================================================================
 * This page lets users add a new birthday reminder.
 * 
 * How it works:
 * 1. It renders the shared <BirthdayForm /> component.
 * 2. When the user clicks "Save Birthday", handleFormSubmit receives the FormData.
 * 3. It calls createBirthday(formData) which sends a POST request to ASP.NET Core.
 * 4. Once saved successfully, React Router's `navigate('/')` sends the user back
 *    to the Dashboard where they see their new birthday!
 */
const AddBirthdayPage = () => {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [serverError, setServerError] = useState('');
  const navigate = useNavigate();

  const handleFormSubmit = async (formData) => {
    setIsSubmitting(true);
    setServerError('');

    try {
      // Send POST request to backend with form data and optional photo
      await createBirthday(formData);

      // On success, redirect back to Dashboard
      navigate('/');
    } catch (err) {
      // If the backend returns an error (e.g. file too big), display it to the user
      const message = err.response?.data?.message || 'Failed to save birthday. Please try again.';
      setServerError(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="form-page-container">
      {/* Page Title & Subtitle */}
      <div className="form-page-header">
        <h1>Add a Birthday</h1>
        <p>Save details for someone you care about</p>
      </div>

      {/* Reusable Form Component */}
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

