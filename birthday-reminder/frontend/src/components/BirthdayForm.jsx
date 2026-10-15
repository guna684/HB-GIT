import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import axiosClient from '../api/axiosClient';

// ---------------------------------------------------------------------------
// HELPER FUNCTIONS (Pure functions outside component)
// ---------------------------------------------------------------------------
const formatDateForInput = (rawDate) => {
  if (!rawDate) return '';
  const dateObj = new Date(rawDate);
  const year = dateObj.getFullYear();
  const month = String(dateObj.getMonth() + 1).padStart(2, '0');
  const day = String(dateObj.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const getTodayDateString = () => {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

/**
 * =============================================================================
 * COMPONENT: BirthdayForm
 * =============================================================================
 * Reusable form used for BOTH creating a new birthday (AddBirthdayPage)
 * and updating an existing birthday (EditBirthdayPage).
 */
const BirthdayForm = ({
  initialData = {},
  onSubmit,
  isSubmitting = false,
  submitButtonText = 'Save Birthday',
  serverError = '',
}) => {
  // ---------------------------------------------------------------------------
  // STEP 1: FORM STATE VARIABLES
  // Initialized directly with initialData on mount
  // ---------------------------------------------------------------------------
  const [personName, setPersonName] = useState(initialData.personName || '');
  const [dateOfBirth, setDateOfBirth] = useState(formatDateForInput(initialData.dateOfBirth));
  const [relationship, setRelationship] = useState(initialData.relationship || 'Friend');
  const [notes, setNotes] = useState(initialData.notes || '');
  const [remindDaysBefore, setRemindDaysBefore] = useState(initialData.remindDaysBefore ?? 1);
  const [photoFile, setPhotoFile] = useState(null); // The actual binary image File object
  const [previewUrl, setPreviewUrl] = useState(null); // Local blob URL to display preview
  const [removePhoto, setRemovePhoto] = useState(false); // Flag to remove photo on edit
  const [validationError, setValidationError] = useState(''); // Client-side error message

  // Today's date in local YYYY-MM-DD
  const [todayString] = useState(getTodayDateString);

  // If editing an existing birthday with a photo, fetch it as a blob so JWT token is sent
  useEffect(() => {
    let isMounted = true;
    let fetchedBlobUrl = null;

    if (initialData.hasPhoto && initialData.photoUrl) {
      axiosClient
        .get(initialData.photoUrl, { responseType: 'blob' })
        .then((response) => {
          if (isMounted) {
            fetchedBlobUrl = URL.createObjectURL(response.data);
            setPreviewUrl(fetchedBlobUrl);
          }
        })
        .catch(() => {
          // If loading fails, keep preview null
        });
    }

    return () => {
      isMounted = false;
      if (fetchedBlobUrl) {
        URL.revokeObjectURL(fetchedBlobUrl);
      }
    };
  }, [initialData.hasPhoto, initialData.photoUrl]);

  // Clean up user-selected file blob preview to prevent browser memory leaks
  useEffect(() => {
    return () => {
      if (previewUrl && previewUrl.startsWith('blob:')) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  // ---------------------------------------------------------------------------
  // STEP 3: HANDLE PHOTO SELECTION & LIVE PREVIEW
  // When a user selects a file from their computer, we check its size & type,
  // then create a local preview URL so they see their picture instantly!
  // ---------------------------------------------------------------------------
  const handlePhotoChange = (event) => {
    const selectedFile = event.target.files[0];
    if (!selectedFile) return;

    // Rule 1: File size limit (500 KB = 500 * 1024 bytes)
    const maxSizeBytes = 500 * 1024;
    if (selectedFile.size > maxSizeBytes) {
      setValidationError('Photo size must be less than 500 KB. Please choose a smaller photo.');
      event.target.value = '';
      return;
    }

    // Rule 2: Allowed image extensions (.jpg, .jpeg, .png, .webp)
    const extension = selectedFile.name.split('.').pop()?.toLowerCase();
    const allowedExtensions = ['jpg', 'jpeg', 'png', 'webp'];
    const allowedMimeTypes = ['image/jpeg', 'image/pjpeg', 'image/png', 'image/webp'];

    const isExtensionValid = allowedExtensions.includes(extension);
    const isMimeValid = !selectedFile.type || allowedMimeTypes.includes(selectedFile.type.toLowerCase());

    if (!isExtensionValid && !isMimeValid) {
      setValidationError('Only JPG, PNG, and WebP images are allowed.');
      event.target.value = '';
      return;
    }

    // If checks pass, clear any previous error and revoke previous blob preview
    setValidationError('');
    if (previewUrl && previewUrl.startsWith('blob:')) {
      URL.revokeObjectURL(previewUrl);
    }

    setRemovePhoto(false);
    setPhotoFile(selectedFile);
    setPreviewUrl(URL.createObjectURL(selectedFile));

    // Reset input value so selecting the same file again triggers onChange
    event.target.value = '';
  };

  const handleRemovePhoto = () => {
    if (previewUrl && previewUrl.startsWith('blob:')) {
      URL.revokeObjectURL(previewUrl);
    }
    setPhotoFile(null);
    setPreviewUrl(null);
    setRemovePhoto(true);
  };

  // ---------------------------------------------------------------------------
  // STEP 4: FORM SUBMISSION & VALIDATION
  // Validates user input before sending to backend.
  // Uses FormData because regular JSON cannot carry binary photo files.
  // ---------------------------------------------------------------------------
  const handleSubmit = (event) => {
    // Prevent standard browser page reload
    event.preventDefault();
    setValidationError('');

    // Check 1: Name must not be empty
    if (!personName.trim()) {
      setValidationError('Please enter a person name.');
      return;
    }

    // Check 2: Date of birth must be selected
    if (!dateOfBirth) {
      setValidationError('Please select a date of birth.');
      return;
    }

    // Check 3: Birth date cannot be in the future
    if (dateOfBirth > todayString) {
      setValidationError('Date of birth cannot be in the future.');
      return;
    }

    // Package the form into standard FormData for file upload
    const formData = new FormData();
    formData.append('PersonName', personName.trim());
    formData.append('DateOfBirth', dateOfBirth);
    formData.append('Relationship', relationship);
    formData.append('Notes', notes.trim());
    formData.append('RemindDaysBefore', remindDaysBefore);

    // If the user picked a new photo, attach it
    if (photoFile) {
      formData.append('Photo', photoFile);
    } else if (removePhoto) {
      formData.append('RemovePhoto', 'true');
    }

    // Call the parent component's onSubmit function
    onSubmit(formData);
  };

  // ---------------------------------------------------------------------------
  // STEP 5: RENDER THE FORM UI
  // ---------------------------------------------------------------------------
  return (
    <form onSubmit={handleSubmit} className="card" encType="multipart/form-data">
      
      {/* Show Error Banners if something is wrong */}
      {validationError && <div className="alert alert-danger">{validationError}</div>}
      {serverError && <div className="alert alert-danger">{serverError}</div>}

      {/* Field 1: Person Name */}
      <div className="form-group">
        <label className="form-label" htmlFor="personName">
          Person Name *
        </label>
        <input
          id="personName"
          type="text"
          className="form-input"
          placeholder="e.g. Priya Sharma"
          maxLength={100}
          value={personName}
          onChange={(e) => setPersonName(e.target.value)}
          required
        />
      </div>

      {/* Field 2: Date of Birth */}
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
        <span className="form-help">Enter their birth date. We calculate turning age and countdowns automatically!</span>
      </div>

      {/* Field 3: Relationship Category Dropdown */}
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

      {/* Field 4: Reminder Advance Setting */}
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

      {/* Field 5: Profile Photo Upload with Live Circular Preview */}
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
            <div style={{ display: 'flex', gap: '8px', alignItems: 'center', flexWrap: 'wrap' }}>
              <label htmlFor="photoFileInput" className="btn btn-secondary" style={{ cursor: 'pointer', margin: 0 }}>
                {previewUrl ? 'Change Photo' : 'Choose Photo'}
              </label>
              {previewUrl && (
                <button
                  type="button"
                  className="btn btn-outline"
                  onClick={handleRemovePhoto}
                  style={{ color: '#ef4444', borderColor: '#fca5a5' }}
                >
                  Remove Photo
                </button>
              )}
            </div>
            <input
              id="photoFileInput"
              type="file"
              accept=".jpg,.jpeg,.png,.webp"
              onChange={handlePhotoChange}
              className="file-input-hidden"
            />
            <span className="form-help">JPG, PNG, or WebP. Maximum 500 KB.</span>
          </div>
        </div>
      </div>

      {/* Field 6: Notes & Gift Ideas */}
      <div className="form-group">
        <label className="form-label" htmlFor="notes">
          Notes & Gift Ideas (Optional)
        </label>
        <textarea
          id="notes"
          className="form-textarea"
          placeholder="e.g. Loves reading sci-fi books, favorite cake is red velvet"
          maxLength={500}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows="3"
        />
      </div>

      {/* Form Action Buttons: Submit & Cancel */}
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
