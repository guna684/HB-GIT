import React from 'react';
import { Link } from 'react-router-dom';

/**
 * Generates initials (e.g. "Ravi Kumar" -> "RK") for the avatar fallback.
 */
const getInitials = (name) => {
  if (!name) return '?';
  const parts = name.trim().split(/\s+/);
  if (parts.length >= 2) {
    return (parts[0][0] + parts[1][0]).toUpperCase();
  }
  return name.slice(0, 2).toUpperCase();
};

/**
 * Returns a cheerful background color for avatars based on the person's name.
 */
const getAvatarColor = (name) => {
  const colors = [
    '#6366F1', // Indigo
    '#EC4899', // Pink
    '#10B981', // Emerald
    '#F59E0B', // Amber
    '#8B5CF6', // Purple
    '#06B6D4', // Cyan
    '#3B82F6', // Blue
    '#F43F5E', // Rose
  ];
  let hash = 0;
  for (let i = 0; i < (name || '').length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  const index = Math.abs(hash) % colors.length;
  return colors[index];
};

/**
 * Formats a date string (ISO format) into readable format like "Oct 16, 2000".
 */
const formatDate = (dateString) => {
  if (!dateString) return '';
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
};

/**
 * Card displaying a single birthday with photo/initials, name, relationship,
 * turning age, and countdown badge.
 */
const BirthdayCard = ({ birthday, onDelete }) => {
  const isToday = birthday.daysUntilBirthday === 0;
  const isTomorrow = birthday.daysUntilBirthday === 1;

  // Compute countdown text and styling
  let countdownText = `In ${birthday.daysUntilBirthday} days`;
  let countdownClass = 'normal';

  if (isToday) {
    countdownText = 'Today! 🎉';
    countdownClass = 'today';
  } else if (isTomorrow) {
    countdownText = 'Tomorrow';
    countdownClass = 'soon';
  }

  // Relationship CSS badge class
  const relationshipClass = `badge-${(birthday.relationship || 'friend').toLowerCase()}`;

  return (
    <div className={`birthday-card ${isToday ? 'is-today' : ''}`}>
      {/* Left: Avatar with photo or colorful initials */}
      <div className="card-avatar">
        {birthday.photoUrl ? (
          <img
            src={birthday.photoUrl}
            alt={birthday.personName}
            className="avatar-img"
            onError={(e) => {
              // Fallback to initials if image fails to load
              e.currentTarget.style.display = 'none';
              e.currentTarget.nextSibling.style.display = 'flex';
            }}
          />
        ) : null}
        <div
          className="avatar-initials"
          style={{
            backgroundColor: getAvatarColor(birthday.personName),
            display: birthday.photoUrl ? 'none' : 'flex',
          }}
        >
          {getInitials(birthday.personName)}
        </div>
      </div>

      {/* Middle: Details */}
      <div className="card-info">
        <div className="card-name-row">
          <span className="card-name" title={birthday.personName}>
            {birthday.personName}
          </span>
        </div>
        <div className="card-turning">
          {isToday ? `Turning ${birthday.turningAge} today!` : `Turning ${birthday.turningAge}`}
        </div>
        <div className="card-meta">
          <span>📅 {formatDate(birthday.dateOfBirth)}</span>
          <span className={`badge-relationship ${relationshipClass}`}>
            {birthday.relationship}
          </span>
        </div>
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

      {/* Right: Countdown badge and quick actions */}
      <div className="card-right">
        <span className={`badge-countdown ${countdownClass}`}>{countdownText}</span>
        <div className="card-actions">
          <Link
            to={`/edit/${birthday.id}`}
            className="action-btn"
            title="Edit birthday"
            aria-label={`Edit ${birthday.personName}`}
          >
            ✏️
          </Link>
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
