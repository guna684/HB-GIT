import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getAllBirthdays, getUpcomingBirthdays, deleteBirthday } from '../api/birthdayApi';
import { useAuth } from '../context/AuthContext';
import BirthdayCard from '../components/BirthdayCard';
import EmptyState from '../components/EmptyState';

/**
 * Main dashboard page showing greetings, today's celebration banner,
 * upcoming birthdays (next 30 days), and all birthdays with search/filter.
 */
const DashboardPage = () => {
  const { user } = useAuth();

  const [allBirthdays, setAllBirthdays] = useState([]);
  const [upcomingBirthdays, setUpcomingBirthdays] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Search & filter state
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedRelationship, setSelectedRelationship] = useState('All');

  // Deletion modal state
  const [birthdayToDelete, setBirthdayToDelete] = useState(null);
  const [isDeleting, setIsDeleting] = useState(false);

  // Load birthdays on initial mount
  const loadData = async () => {
    setLoading(true);
    setError('');

    try {
      const [all, upcoming] = await Promise.all([
        getAllBirthdays(),
        getUpcomingBirthdays(),
      ]);
      setAllBirthdays(all);
      setUpcomingBirthdays(upcoming);
    } catch (err) {
      setError('Failed to load birthdays. Please check your connection.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  // Handle confirmation of deletion
  const confirmDelete = async () => {
    if (!birthdayToDelete) return;

    setIsDeleting(true);
    try {
      await deleteBirthday(birthdayToDelete.id);
      // Remove deleted item from local state
      setAllBirthdays((prev) => prev.filter((b) => b.id !== birthdayToDelete.id));
      setUpcomingBirthdays((prev) => prev.filter((b) => b.id !== birthdayToDelete.id));
      setBirthdayToDelete(null);
    } catch (err) {
      alert('Failed to delete birthday. Please try again.');
    } finally {
      setIsDeleting(false);
    }
  };

  // Find people whose birthday is TODAY
  const todayBirthdays = allBirthdays.filter((b) => b.daysUntilBirthday === 0);

  // Filter "All Birthdays" based on search text and selected relationship
  const filteredBirthdays = allBirthdays.filter((item) => {
    const matchesSearch =
      item.personName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      (item.notes && item.notes.toLowerCase().includes(searchTerm.toLowerCase()));

    const matchesRelationship =
      selectedRelationship === 'All' ||
      item.relationship.toLowerCase() === selectedRelationship.toLowerCase();

    return matchesSearch && matchesRelationship;
  });

  const firstName = user?.name ? user.name.split(' ')[0] : 'Friend';

  return (
    <div>
      {/* Top Header: Greeting + Add Birthday button */}
      <div className="dashboard-header">
        <div className="greeting-text">
          <h1>Hi {firstName} 👋</h1>
          <p>Here are the birthdays you are keeping track of.</p>
        </div>
        <Link to="/add" className="btn btn-primary">
          + Add Birthday
        </Link>
      </div>

      {/* Highlighted Celebration Banner if someone has a birthday today */}
      {todayBirthdays.length > 0 && (
        <div className="today-banner">
          <div className="today-banner-content">
            <span className="today-banner-icon">🎂</span>
            <div>
              <h2>Today's Birthday!</h2>
              <p>
                {todayBirthdays.map((b) => `${b.personName} (turning ${b.turningAge})`).join(', ')}
                {todayBirthdays.length === 1 ? ' is celebrating today!' : ' are celebrating today!'}
              </p>
            </div>
          </div>
          <span style={{ fontSize: '1.4rem' }}>🎉🥳</span>
        </div>
      )}

      {/* Error alert */}
      {error && <div className="alert alert-danger">{error}</div>}

      {/* Loading state */}
      {loading ? (
        <div className="spinner-container">
          <div className="spinner"></div>
          <p>Loading your reminders...</p>
        </div>
      ) : allBirthdays.length === 0 ? (
        // Global empty state if user hasn't added any birthdays
        <EmptyState />
      ) : (
        <>
          {/* Section 1: Upcoming Birthdays (Next 30 Days) */}
          <section className="section">
            <div className="section-header">
              <h2 className="section-title">
                <span>🌟 Upcoming Birthdays</span>
                <span className="section-badge">{upcomingBirthdays.length}</span>
              </h2>
            </div>

            {upcomingBirthdays.length === 0 ? (
              <div className="card" style={{ textAlign: 'center', padding: '32px', color: 'var(--text-secondary)' }}>
                <p>No birthdays coming up in the next 30 days.</p>
              </div>
            ) : (
              <div className="cards-grid">
                {upcomingBirthdays.map((birthday) => (
                  <BirthdayCard
                    key={`upcoming-${birthday.id}`}
                    birthday={birthday}
                    onDelete={(b) => setBirthdayToDelete(b)}
                  />
                ))}
              </div>
            )}
          </section>

          {/* Section 2: All Birthdays with Search & Filter */}
          <section className="section">
            <div className="section-header">
              <h2 className="section-title">
                <span>👥 All Birthdays</span>
                <span className="section-badge">{allBirthdays.length}</span>
              </h2>
            </div>

            {/* Filter Bar */}
            <div className="filter-bar">
              <div className="search-input-wrapper">
                <span className="search-icon">🔍</span>
                <input
                  type="text"
                  className="search-input"
                  placeholder="Search by name or notes..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                />
              </div>

              <select
                className="filter-select"
                value={selectedRelationship}
                onChange={(e) => setSelectedRelationship(e.target.value)}
              >
                <option value="All">All Categories</option>
                <option value="Family">Family</option>
                <option value="Friend">Friend</option>
                <option value="Work">Work</option>
                <option value="Other">Other</option>
              </select>
            </div>

            {filteredBirthdays.length === 0 ? (
              <EmptyState
                emoji="🔍"
                title="No birthdays match your search"
                subtitle="Try clearing the search text or switching the category filter."
                showAddButton={false}
              />
            ) : (
              <div className="cards-grid">
                {filteredBirthdays.map((birthday) => (
                  <BirthdayCard
                    key={`all-${birthday.id}`}
                    birthday={birthday}
                    onDelete={(b) => setBirthdayToDelete(b)}
                  />
                ))}
              </div>
            )}
          </section>
        </>
      )}

      {/* Delete Confirmation Modal */}
      {birthdayToDelete && (
        <div className="modal-overlay" onClick={() => !isDeleting && setBirthdayToDelete(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <span className="modal-icon">⚠️</span>
              <h3 className="modal-title">Delete Birthday</h3>
            </div>
            <p className="modal-body">
              Are you sure you want to delete the reminder for{' '}
              <strong>{birthdayToDelete.personName}</strong>? This action cannot be undone.
            </p>
            <div className="modal-actions">
              <button
                type="button"
                className="btn btn-outline"
                onClick={() => setBirthdayToDelete(null)}
                disabled={isDeleting}
              >
                Cancel
              </button>
              <button
                type="button"
                className="btn btn-danger"
                onClick={confirmDelete}
                disabled={isDeleting}
              >
                {isDeleting ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default DashboardPage;
