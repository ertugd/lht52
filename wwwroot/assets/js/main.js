document.addEventListener('DOMContentLoaded', () => {
    // Main navigation menu toggle
    const menuToggle = document.getElementById('menu-toggle-btn');
    const navLinks = document.getElementById('navbar-links');
    if (menuToggle && navLinks) {
        menuToggle.addEventListener('click', (e) => {
            e.stopPropagation();
            navLinks.classList.toggle('active');
        });
        
        // Close menu when clicking outside
        document.addEventListener('click', (e) => {
            if (!navLinks.contains(e.target) && !menuToggle.contains(e.target)) {
                navLinks.classList.remove('active');
            }
        });
    }

    // GPS page sidebar toggle
    const sidebarToggle = document.getElementById('sidebar-toggle-btn');
    const sidebar = document.querySelector('.sidebar');
    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener('click', (e) => {
            e.stopPropagation();
            sidebar.classList.toggle('active');
            // Close main menu if open
            if (navLinks) navLinks.classList.remove('active');
        });

        // Close sidebar when clicking on the map
        const mapEl = document.getElementById('map');
        if (mapEl) {
            mapEl.addEventListener('click', (e) => {
                // If the click is inside a Leaflet popup (like clicking View History), do not close the sidebar
                if (e.target.closest('.leaflet-popup')) {
                    return;
                }
                sidebar.classList.remove('active');
            });
        }
        
        // Also close sidebar when any device card or history session is clicked inside the sidebar
        sidebar.addEventListener('click', (e) => {
            const card = e.target.closest('.device-card');
            if (card) {
                sidebar.classList.remove('active');
            }
        });
    }
});
