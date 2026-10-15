import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getAllBirthdays, getUpcomingBirthdays, deleteBirthday } from '../api/birthdayApi';
import { useAuth } from '../context/AuthContext';
import BirthdayCard from '../components/BirthdayCard';
import EmptyState from '../components/EmptyState';

/**
 * =============================================================================
 * PAGE: DashboardPage
 * =============================================================================
 * The main home page of the app after logging in.
 * Features:
 * 1. Warm greeting with the logged-in user's first name.
 * 2. Big celebration banner if anyone's birthday is TODAY!
 * 3. "Upcoming Birthdays" section (next 30 days).
 * 4. "All Birthdays" section with live search and relationship filter.
 * 5. Safe Delete confirmation popup modal.
 */
const DashboardPage = () => {
  // Grab the currently logged-in user from our global AuthContext
  const { user } = useAuth();

  // ---------------------------------------------------------------------------
  // STEP 1: COMPONENT STATE
  // React state holds the data that changes over time and needs to update the UI.
  // ---------------------------------------------------------------------------
  const [allBirthdays, setAllBirthdays] = useState([]);         // List of all birthdays
  const [upcomingBirthdays, setUpcomingBirthdays] = useState([]); // List of birthdays in next 30 days
  const [loading, setLoading] = useState(true);                  // True while fetching data from backend
  const [error, setError] = useState('');                        // Error message if fetch fails

  // Search & category filter states
  const [searchTerm, setSearchTerm] = useState('');             // Text typed into the search bar
  const [selectedRelationship, setSelectedRelationship] = useState('All'); // "All", "Family", "Friend", etc.

  // Delete modal state
  const [birthdayToDelete, setBirthdayToDelete] = useState(null); // The birthday object to be deleted (or null if modal closed)
  const [isDeleting, setIsDeleting] = useState(false);            // True while DELETE API call is in progress

  // ---------------------------------------------------------------------------
  // STEP 2: LOAD DATA FROM BACKEND ON PAGE LOAD
  // useEffect with an empty array [] runs exactly once when this page first appears on screen.
  // ---------------------------------------------------------------------------
  useEffect(() => {
    let isMounted = true;

    const loadDashboardData = async () => {
      try {
        // Promise.all runs both requests at the same time for faster page load!
        const [allResults, upcomingResults] = await Promise.all([
          getAllBirthdays(),
          getUpcomingBirthdays(),
        ]);

        if (isMounted) {
          setAllBirthdays(allResults);
          setUpcomingBirthdays(upcomingResults);
          setError('');
        }
      } catch {
        if (isMounted) {
          setError('Failed to load birthdays. Please check your connection.');
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    loadDashboardData();

    return () => {
      isMounted = false;
    };
  }, []);

  // ---------------------------------------------------------------------------
  // STEP 3: HANDLE DELETING A BIRTHDAY
  // When the user confirms deletion in the popup modal:
  // 1. Call backend API: DELETE /api/birthdays/{id}
  // 2. Remove that item from our local React state so UI updates immediately!
  // ---------------------------------------------------------------------------
  const handleConfirmDelete = async () => {
    if (!birthdayToDelete) return;

    setIsDeleting(true);
    try {
      await deleteBirthday(birthdayToDelete.id);

      // Filter out the deleted birthday from both lists in state
      setAllBirthdays((previousList) => 
        previousList.filter((b) => b.id !== birthdayToDelete.id)
      );
      setUpcomingBirthdays((previousList) => 
        previousList.filter((b) => b.id !== birthdayToDelete.id)
      );

      // Close the modal
      setBirthdayToDelete(null);
    } catch {
      alert('Failed to delete birthday. Please try again.');
    } finally {
      setIsDeleting(false);
    }
  };

  // ---------------------------------------------------------------------------
  // STEP 4: FILTERING & SEARCH LOGIC
  // Notice: We do filtering directly in React memory! We do NOT need to call the
  // backend on every keystroke, which makes the search instantaneous!
  // ---------------------------------------------------------------------------
  
  // Find anyone whose birthday is TODAY (daysUntilBirthday === 0)
  const todayBirthdays = allBirthdays.filter((b) => b.daysUntilBirthday === 0);

  // Filter "All Birthdays" by search term (name or notes) and relationship dropdown
  const filteredBirthdays = allBirthdays.filter((item) => {
    const searchLower = searchTerm.toLowerCase();
    
    // Check if search matches name or notes
    const matchesName = item.personName.toLowerCase().includes(searchLower);
    const matchesNotes = item.notes && item.notes.toLowerCase().includes(searchLower);
    const matchesSearch = matchesName || matchesNotes;

    // Check if category matches
    const matchesCategory =
      selectedRelationship === 'All' ||
      item.relationship.toLowerCase() === selectedRelationship.toLowerCase();

    return matchesSearch && matchesCategory;
  });

  // Extract first name for a friendly greeting (e.g. "Ravi Kumar" -> "Ravi")
  const userFirstName = user?.name ? user.name.split(' ')[0] : 'Friend';

  // ---------------------------------------------------------------------------
  // STEP 5: RENDER THE DASHBOARD UI
  // ---------------------------------------------------------------------------
  return (
    <div>
      {/* 1. Header: Greeting & "+ Add Birthday" Button */}
      <div className="dashboard-header">
        <div className="greeting-text">
          <h1>Hi {userFirstName} 👋</h1>
          <p>Here are the birthdays you are keeping track of.</p>
        </div>
        <Link to="/add" className="btn btn-primary">
          + Add Birthday
        </Link>
      </div>

      {/* 2. Today's Celebration Banner (Shows only if someone has a birthday today!) */}
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

      {/* 3. Error Banner (if API failed) */}
      {error && <div className="alert alert-danger">{error}</div>}

      {/* 4. Main Content: Loading Spinner vs Empty State vs Birthday Lists */}
      {loading ? (
        // State A: Loading spinner
        <div className="spinner-container">
          <div className="spinner"></div>
          <p>Loading your reminders...</p>
        </div>
      ) : allBirthdays.length === 0 ? (
        // State B: No birthdays added yet (shows friendly empty state)
        <EmptyState />
      ) : (
        // State C: Show birthday sections
        <>
          {/* Section A: Upcoming Birthdays (Next 30 Days) */}
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

          {/* Section B: All Birthdays with Live Search & Category Filter */}
          <section className="section">
            <div className="section-header">
              <h2 className="section-title">
                <span>👥 All Birthdays</span>
                <span className="section-badge">{allBirthdays.length}</span>
              </h2>
            </div>

            {/* Search Input and Category Dropdown */}
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

            {/* List or Search Empty State */}
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

      {/* 5. Delete Confirmation Popup Modal */}
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
                onClick={handleConfirmDelete}
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
