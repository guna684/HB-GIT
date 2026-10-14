import React from 'react';
import { Link } from 'react-router-dom';

/**
 * Friendly empty state displayed when there are no birthdays saved yet
 * or when a search/filter produces 0 results.
 */
const EmptyState = ({
  emoji = '🎈',
  title = 'No birthdays yet. Add your first one!',
  subtitle = 'Never forget to celebrate someone special again.',
  showAddButton = true,
}) => {
  return (
    <div className="empty-state">
      <div className="empty-state-emoji">{emoji}</div>
      <h3 className="empty-state-text">{title}</h3>
      <p className="empty-state-subtext">{subtitle}</p>
      {showAddButton && (
        <Link to="/add" className="btn btn-primary">
          + Add Birthday
        </Link>
      )}
    </div>
  );
};

export default EmptyState;
