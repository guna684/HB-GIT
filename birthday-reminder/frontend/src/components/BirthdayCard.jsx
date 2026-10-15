import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';

// =============================================================================
// HELPER FUNCTIONS
// These are simple helper functions that do specific little jobs for our card.
// Putting them outside the component keeps our main component clean and fast!
// =============================================================================

/**
 * Helper 1: getInitials
 * Takes a full name (e.g. "Ravi Kumar") and returns 2 uppercase letters (e.g. "RK").
 * If the user hasn't uploaded a photo, we show these initials inside a colorful circle!
 */
const getInitials = (fullName) => {
  if (!fullName) return '?';

  // Split name by spaces into words: "Ravi Kumar" -> ["Ravi", "Kumar"]
  const words = fullName.trim().split(/\s+/);

  // If there are 2 or more words, take first letter of word 1 and first letter of word 2
  if (words.length >= 2) {
    const firstInitial = words[0][0];
    const secondInitial = words[1][0];
    return (firstInitial + secondInitial).toUpperCase();
  }

  // If only 1 word (e.g. "Priya"), take the first 2 letters
  return fullName.slice(0, 2).toUpperCase();
};

/**
 * Helper 2: getAvatarColor
 * Generates a consistent, cheerful background color for each person's avatar.
 * The same name will always get the same color!
 */
const getAvatarColor = (name) => {
  const colorPalette = [
    '#6366F1', // Indigo
    '#EC4899', // Pink
    '#10B981', // Emerald Green
    '#F59E0B', // Amber
    '#8B5CF6', // Purple
    '#06B6D4', // Cyan
    '#3B82F6', // Blue
    '#F43F5E', // Rose
  ];

  // We convert the letters of the name into a number (hash)
  let hashNumber = 0;
  for (let i = 0; i < (name || '').length; i++) {
    hashNumber = name.charCodeAt(i) + ((hashNumber << 5) - hashNumber);
  }

  // Use the remainder (%) to pick one color from the array
  const colorIndex = Math.abs(hashNumber) % colorPalette.length;
  return colorPalette[colorIndex];
};

/**
 * Helper 3: formatDate
 * Converts a raw date string (like "2000-10-15T00:00:00") into a friendly readable
 * format like "Oct 15, 2000" using the browser's built-in Intl date formatter.
 */
const formatDate = (dateString) => {
  if (!dateString) return '';
  const dateObject = new Date(dateString);
  return dateObject.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
};

// =============================================================================
// MAIN COMPONENT: BirthdayCard
// Props received:
// - birthday: An object containing { id, personName, dateOfBirth, relationship,
//             notes, photoUrl, daysUntilBirthday, turningAge }
// - onDelete: A function passed from DashboardPage that opens the delete confirmation
// =============================================================================
const BirthdayCard = ({ birthday, onDelete }) => {
  // We use React state to track if the photo failed to load.
  // If it fails (broken link or file not found), we smoothly show the initials instead!
  const [imageFailedToLoad, setImageFailedToLoad] = useState(false);

  // Reset fallback state if the photo URL updates (e.g. after editing)
  useEffect(() => {
    setImageFailedToLoad(false);
  }, [birthday.photoUrl]);

  // ---------------------------------------------------------------------------
  // STEP 1: Calculate countdown text and badge color
  // ---------------------------------------------------------------------------
  const isToday = birthday.daysUntilBirthday === 0;
  const isTomorrow = birthday.daysUntilBirthday === 1;

  let countdownText = `In ${birthday.daysUntilBirthday} days`;
  let countdownStyleClass = 'normal';

  if (isToday) {
    countdownText = 'Today! 🎉';
    countdownStyleClass = 'today';
  } else if (isTomorrow) {
    countdownText = 'Tomorrow';
    countdownStyleClass = 'soon';
  }

  // Pick the badge color based on relationship ("Family", "Friend", "Work", "Other")
  const relationshipBadgeClass = `badge-${(birthday.relationship || 'friend').toLowerCase()}`;

  // ---------------------------------------------------------------------------
  // STEP 2: Render the Card UI
  // ---------------------------------------------------------------------------
  return (
    <div className={`birthday-card ${isToday ? 'is-today' : ''}`}>
      
      {/* 1. LEFT COLUMN: Avatar (Photo or Colored Initials) */}
      <div className="card-avatar">
        {birthday.photoUrl && !imageFailedToLoad ? (
          <img
            src={birthday.photoUrl}
            alt={birthday.personName}
            className="avatar-img"
            onError={() => setImageFailedToLoad(true)} // If photo fails, switch to initials
          />
        ) : (
          <div
            className="avatar-initials"
            style={{ backgroundColor: getAvatarColor(birthday.personName) }}
          >
            {getInitials(birthday.personName)}
          </div>
        )}
      </div>

      {/* 2. MIDDLE COLUMN: Person Details */}
      <div className="card-info">
        {/* Person Name */}
        <div className="card-name-row">
          <span className="card-name" title={birthday.personName}>
            {birthday.personName}
          </span>
        </div>

        {/* Turning Age (e.g. "Turning 25 today!" or "Turning 25") */}
        <div className="card-turning">
          {isToday ? `Turning ${birthday.turningAge} today!` : `Turning ${birthday.turningAge}`}
        </div>

        {/* Birth Date and Relationship Category Badge */}
        <div className="card-meta">
          <span>📅 {formatDate(birthday.dateOfBirth)}</span>
          <span className={`badge-relationship ${relationshipBadgeClass}`}>
            {birthday.relationship}
          </span>
        </div>

        {/* Optional Notes (gift ideas, favorite cake, memories) */}
        {birthday.notes && (
          <div
            style={{
              fontSize: '0.8rem',
              color: 'var(--text-secondary)',
              marginTop: '4px',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
            title={birthday.notes}
          >
            💭 {birthday.notes}
          </div>
        )}
      </div>

      {/* 3. RIGHT COLUMN: Countdown Badge & Edit/Delete Action Buttons */}
      <div className="card-right">
        {/* Countdown Badge ("Today! 🎉", "Tomorrow", "In 12 days") */}
        <span className={`badge-countdown ${countdownStyleClass}`}>
          {countdownText}
        </span>

        {/* Action Buttons: Edit and Delete */}
        <div className="card-actions">
          {/* Edit Button: Links to /edit/5 */}
          <Link
            to={`/edit/${birthday.id}`}
            className="action-btn"
            title="Edit birthday details"
            aria-label={`Edit ${birthday.personName}`}
          >
            ✏️
          </Link>

          {/* Delete Button: Calls the onDelete function passed from parent */}
          <button
            type="button"
            className="action-btn delete-btn"
            onClick={() => onDelete(birthday)}
            title="Delete birthday"
            aria-label={`Delete ${birthday.personName}`}
          >
            🗑️
          </button>
        </div>
      </div>

    </div>
  );
};

export default BirthdayCard;
