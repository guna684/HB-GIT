import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App.jsx';
import './styles/global.css';

/**
 * =============================================================================
 * REACT MOUNT POINT: main.jsx
 * =============================================================================
 * This file connects React with the index.html page.
 * It finds <div id="root"></div> in index.html and injects our <App /> inside it.
 */
ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

