import React from 'react';
import { Link } from 'react-router-dom';

/**
 * =============================================================================
 * COMPONENT: EmptyState
 * =============================================================================
 * A friendly, reusable placeholder displayed when there is no data to show.
 * 
 * Used in two scenarios:
 * 1. When a new user has 0 birthdays saved yet -> Encourages them to add one!
 * 2. When a search query returns 0 matching results -> Suggests clearing search.
 * 
 * Props:
 * - emoji: Visual icon (defaults to 🎈)
 * - title: Main headline
 * - subtitle: Explanation text
 * - showAddButton: Whether to show the "+ Add Birthday" button
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

