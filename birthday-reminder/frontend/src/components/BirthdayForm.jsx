import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';

/**
 * Reusable form component used by both AddBirthdayPage and EditBirthdayPage.
 * Includes live photo upload preview, date constraints, and field validation.
 */
const BirthdayForm = ({
  initialData = {},
  onSubmit,
  isSubmitting = false,
  submitButtonText = 'Save Birthday',
  serverError = '',
}) => {
  // Format initial date to YYYY-MM-DD for <input type="date" />
  const getFormattedDate = (dateVal) => {
    if (!dateVal) return '';
    const d = new Date(dateVal);
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${d.getFullYear()}-${month}-${day}`;
  };

  const [personName, setPersonName] = useState(initialData.personName || '');
  const [dateOfBirth, setDateOfBirth] = useState(getFormattedDate(initialData.dateOfBirth));
  const [relationship, setRelationship] = useState(initialData.relationship || 'Friend');
  const [notes, setNotes] = useState(initialData.notes || '');
  const [remindDaysBefore, setRemindDaysBefore] = useState(initialData.remindDaysBefore ?? 1);
  const [photoFile, setPhotoFile] = useState(null);
  const [previewUrl, setPreviewUrl] = useState(initialData.photoUrl || null);
  const [validationError, setValidationError] = useState('');

  // Update form if initialData arrives after fetch
  useEffect(() => {
    if (initialData.personName) setPersonName(initialData.personName);
    if (initialData.dateOfBirth) setDateOfBirth(getFormattedDate(initialData.dateOfBirth));
    if (initialData.relationship) setRelationship(initialData.relationship);
    if (initialData.notes !== undefined) setNotes(initialData.notes || '');
    if (initialData.remindDaysBefore !== undefined) setRemindDaysBefore(initialData.remindDaysBefore);
    if (initialData.photoUrl) setPreviewUrl(initialData.photoUrl);
  }, [initialData]);

  // Today's date in YYYY-MM-DD format to prevent future birth dates
  const todayString = new Date().toISOString().split('T')[0];

  // Handle image file selection and generate a local preview URL
  const handlePhotoChange = (e) => {
    const file = e.target.files[0];
    if (!file) return;

    // Check file size (max 2 MB = 2 * 1024 * 1024 bytes)
    const maxSizeBytes = 2 * 1024 * 1024;
    if (file.size > maxSizeBytes) {
      setValidationError('Photo size must be less than 2 MB.');
      return;
    }

    // Check file extension
    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      setValidationError('Only JPG, PNG, and WebP images are allowed.');
      return;
    }

    setValidationError('');
    setPhotoFile(file);
    // Create live preview object URL
    setPreviewUrl(URL.createObjectURL(file));
  };

  // Form submission handler
  const handleSubmit = (e) => {
    e.preventDefault();
    setValidationError('');

    // Client-side validations
    if (!personName.trim()) {
      setValidationError('Please enter a person name.');
      return;
    }

    if (!dateOfBirth) {
      setValidationError('Please select a date of birth.');
      return;
    }

    if (dateOfBirth > todayString) {
      setValidationError('Date of birth cannot be in the future.');
      return;
    }

    // Package the form into a multipart/form-data object
    const formData = new FormData();
    formData.append('PersonName', personName.trim());
    formData.append('DateOfBirth', dateOfBirth);
    formData.append('Relationship', relationship);
    formData.append('Notes', notes.trim());
    formData.append('RemindDaysBefore', remindDaysBefore);

    if (photoFile) {
      formData.append('Photo', photoFile);
    }

    onSubmit(formData);
  };

  return (
    <form onSubmit={handleSubmit} className="card" encType="multipart/form-data">
      {/* Error alerts */}
      {validationError && <div className="alert alert-danger">{validationError}</div>}
      {serverError && <div className="alert alert-danger">{serverError}</div>}

      {/* Person Name */}
      <div className="form-group">
        <label className="form-label" htmlFor="personName">
          Person Name *
        </label>
        <input
          id="personName"
          type="text"
          className="form-input"
          placeholder="e.g. Priya Sharma"
          value={personName}
          onChange={(e) => setPersonName(e.target.value)}
          required
        />
      </div>

      {/* Date of Birth */}
      <div className="form-group">
        <label className="form-label" htmlFor="dateOfBirth">
          Date of Birth *
        </label>
        <input
          id="dateOfBirth"
          type="date"
          className="form-input"
          max={todayString}
          value={dateOfBirth}
          onChange={(e) => setDateOfBirth(e.target.value)}
          required
        />
        <span className="form-help">Enter their actual birth date. We calculate their age automatically!</span>
      </div>

      {/* Relationship Dropdown */}
      <div className="form-group">
        <label className="form-label" htmlFor="relationship">
          Relationship *
        </label>
        <select
          id="relationship"
          className="form-select"
          value={relationship}
          onChange={(e) => setRelationship(e.target.value)}
        >
          <option value="Friend">Friend</option>
          <option value="Family">Family</option>
          <option value="Work">Work</option>
          <option value="Other">Other</option>
        </select>
      </div>

      {/* Remind Days Before */}
      <div className="form-group">
        <label className="form-label" htmlFor="remindDaysBefore">
          Remind Me In Advance
        </label>
        <select
          id="remindDaysBefore"
          className="form-select"
          value={remindDaysBefore}
          onChange={(e) => setRemindDaysBefore(Number(e.target.value))}
        >
          <option value={0}>On the day of birthday</option>
          <option value={1}>1 day before (recommended)</option>
          <option value={2}>2 days before</option>
          <option value={3}>3 days before</option>
          <option value={7}>1 week before</option>
        </select>
      </div>

      {/* Photo Upload with Live Preview */}
      <div className="form-group">
        <label className="form-label">Profile Photo (Optional)</label>
        <div className="photo-upload-area">
          <div className="photo-preview-circle">
            {previewUrl ? (
              <img src={previewUrl} alt="Preview" className="photo-preview-img" />
            ) : (
              <span className="photo-placeholder-icon">📷</span>
            )}
          </div>
          <div className="photo-upload-controls">
            <label htmlFor="photoFileInput" className="btn btn-secondary" style={{ cursor: 'pointer' }}>
              {previewUrl ? 'Change Photo' : 'Choose Photo'}
            </label>
            <input
              id="photoFileInput"
              type="file"
              accept=".jpg,.jpeg,.png,.webp"
              onChange={handlePhotoChange}
              className="file-input-hidden"
            />
            <span className="form-help">JPG, PNG, or WebP. Maximum 2 MB.</span>
          </div>
        </div>
      </div>

      {/* Notes */}
      <div className="form-group">
        <label className="form-label" htmlFor="notes">
          Notes & Gift Ideas (Optional)
        </label>
        <textarea
          id="notes"
          className="form-textarea"
          placeholder="e.g. Loves reading sci-fi books, favorite cake is red velvet"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows="3"
        />
      </div>

      {/* Buttons */}
      <div style={{ display: 'flex', gap: '12px', marginTop: '24px' }}>
        <button
          type="submit"
          className="btn btn-primary"
          style={{ flex: 1 }}
          disabled={isSubmitting}
        >
          {isSubmitting ? 'Saving...' : submitButtonText}
        </button>
        <Link to="/" className="btn btn-outline">
          Cancel
        </Link>
      </div>
    </form>
  );
};

export default BirthdayForm;
