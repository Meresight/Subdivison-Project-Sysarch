// User Management JavaScript

document.addEventListener('DOMContentLoaded', function () {
    console.log('User Management JS loaded');

    // Filter functionality
    const roleFilter = document.getElementById('role-filter');
    const statusFilter = document.getElementById('status-filter');

    if (roleFilter && statusFilter) {
        roleFilter.addEventListener('change', applyFilters);
        statusFilter.addEventListener('change', applyFilters);
    }

    function applyFilters() {
        const roleValue = roleFilter.value;
        const statusValue = statusFilter.value;
        const userRows = document.querySelectorAll('.users-table tbody tr');

        userRows.forEach(row => {
            const userRole = row.querySelector('td:nth-child(2)').textContent.trim();
            const userStatus = row.querySelector('td:nth-child(3) .status').textContent.trim();

            const roleMatch = roleValue === 'All Roles' || userRole === roleValue;
            const statusMatch = statusValue === 'All Statuses' || userStatus === statusValue;

            if (roleMatch && statusMatch) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
    }

    // Search functionality
    const searchInput = document.getElementById('user-search');
    if (searchInput) {
        searchInput.addEventListener('keyup', performSearch);
    }

    function performSearch() {
        const searchTerm = searchInput.value.toLowerCase();
        const userRows = document.querySelectorAll('.users-table tbody tr');

        userRows.forEach(row => {
            const userName = row.querySelector('.user-name').textContent.toLowerCase();
            const userEmail = row.querySelector('.user-email').textContent.toLowerCase();

            if (userName.includes(searchTerm) || userEmail.includes(searchTerm)) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
    }

    // Reset Password Modal
    const resetPasswordBtns = document.querySelectorAll('.reset-password-btn');
    const resetPasswordModal = document.getElementById('reset-password-modal');
    const resetUserId = document.getElementById('reset-user-id');

    if (resetPasswordBtns && resetPasswordModal && resetUserId) {
        resetPasswordBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const userId = this.getAttribute('data-user-id');
                resetUserId.value = userId;
                resetPasswordModal.classList.add('show');
            });
        });
    }

    // Delete User Confirmation
    const deleteUserBtns = document.querySelectorAll('.delete-user-btn');
    const deleteConfirmationModal = document.getElementById('delete-confirmation-modal');
    const confirmDeleteBtn = document.getElementById('confirm-delete');

    if (deleteUserBtns && deleteConfirmationModal && confirmDeleteBtn) {
        deleteUserBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const userId = this.getAttribute('data-user-id');
                confirmDeleteBtn.setAttribute('data-user-id', userId);
                deleteConfirmationModal.classList.add('show');
            });
        });

        confirmDeleteBtn.addEventListener('click', function () {
            const userId = this.getAttribute('data-user-id');
            deleteUser(userId);
        });
    }

    function deleteUser(userId) {
        // Show loading indicator
        confirmDeleteBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Deleting...';
        confirmDeleteBtn.disabled = true;

        // Send AJAX request to delete user
        fetch('/UserManagement/DeleteUser', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ id: userId })
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    showAlert('success', data.message || 'User deleted successfully.');

                    // Close modal
                    deleteConfirmationModal.classList.remove('show');

                    // Remove the row from the table
                    const userRow = document.querySelector(`tr[data-user-id="${userId}"]`);
                    if (userRow) {
                        userRow.remove();
                    } else {
                        // Reload the page if the row can't be found
                        setTimeout(() => {
                            window.location.reload();
                        }, 1500);
                    }
                } else {
                    showAlert('danger', data.message || 'An error occurred while deleting the user.');
                    deleteConfirmationModal.classList.remove('show');
                }
            })
            .catch(error => {
                console.error('Error:', error);
                showAlert('danger', 'An error occurred while deleting the user.');
                deleteConfirmationModal.classList.remove('show');
            })
            .finally(() => {
                // Reset button state
                confirmDeleteBtn.innerHTML = 'Delete User';
                confirmDeleteBtn.disabled = false;
            });
    }

    // Change User Status
    const statusChangeBtns = document.querySelectorAll('.change-status-btn');

    if (statusChangeBtns) {
        statusChangeBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const userId = this.getAttribute('data-user-id');
                const newStatus = this.getAttribute('data-status');

                if (confirm(`Are you sure you want to change this user's status to ${newStatus}?`)) {
                    changeUserStatus(userId, newStatus);
                }
            });
        });
    }

    function changeUserStatus(userId, status) {
        fetch('/UserManagement/ChangeUserStatus', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ userId: userId, status: status })
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    showAlert('success', data.message || `User status changed to ${status} successfully.`);

                    // Update the status in the table
                    const statusCell = document.querySelector(`tr[data-user-id="${userId}"] td:nth-child(3) .status`);
                    if (statusCell) {
                        statusCell.textContent = status;
                        statusCell.className = `status ${status.toLowerCase()}`;
                    } else {
                        // Reload the page if the cell can't be found
                        setTimeout(() => {
                            window.location.reload();
                        }, 1500);
                    }
                } else {
                    showAlert('danger', data.message || 'An error occurred while changing the user status.');
                }
            })
            .catch(error => {
                console.error('Error:', error);
                showAlert('danger', 'An error occurred while changing the user status.');
            });
    }

    // Close Modals
    const closeModalBtns = document.querySelectorAll('.close-modal, .close-modal-btn');

    if (closeModalBtns) {
        closeModalBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const modal = this.closest('.modal');
                if (modal) {
                    modal.classList.remove('show');
                }
            });
        });
    }

    // Outside click to close modals
    window.addEventListener('click', function (event) {
        const modals = document.querySelectorAll('.modal.show');
        modals.forEach(modal => {
            if (event.target === modal) {
                modal.classList.remove('show');
            }
        });
    });

    // Toggle Password Visibility
    const togglePasswordBtns = document.querySelectorAll('.toggle-password');

    if (togglePasswordBtns) {
        togglePasswordBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const passwordInput = this.previousElementSibling;
                const icon = this.querySelector('i');

                if (passwordInput.type === 'password') {
                    passwordInput.type = 'text';
                    icon.classList.remove('fa-eye');
                    icon.classList.add('fa-eye-slash');
                } else {
                    passwordInput.type = 'password';
                    icon.classList.remove('fa-eye-slash');
                    icon.classList.add('fa-eye');
                }
            });
        });
    }

    // Close Alert Messages
    const closeAlertBtns = document.querySelectorAll('.close-alert');

    if (closeAlertBtns) {
        closeAlertBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const alert = this.closest('.alert');
                if (alert) {
                    alert.style.display = 'none';
                }
            });
        });
    }

    // Auto-hide alerts after 5 seconds
    setTimeout(function () {
        const alerts = document.querySelectorAll('.alert');
        alerts.forEach(alert => {
            alert.style.display = 'none';
        });
    }, 5000);

    // Helper Functions
    function getAntiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]').value;
    }

    function showAlert(type, message) {
        const alertClass = type === 'success' ? 'alert-success' : 'alert-danger';
        const icon = type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle';

        const alertHtml = `
            <div class="alert ${alertClass}">
                <i class="fas ${icon}"></i> ${message}
                <button class="close-alert"><i class="fas fa-times"></i></button>
            </div>
        `;

        // Find the page header to insert the alert after it
        const pageHeader = document.querySelector('.page-header');
        if (pageHeader) {
            pageHeader.insertAdjacentHTML('afterend', alertHtml);

            // Add event listener to the newly created close button
            const newAlert = pageHeader.nextElementSibling;
            const newCloseBtn = newAlert.querySelector('.close-alert');
            if (newCloseBtn) {
                newCloseBtn.addEventListener('click', function () {
                    newAlert.style.display = 'none';
                });
            }

            // Auto-hide this alert after 5 seconds
            setTimeout(function () {
                newAlert.style.display = 'none';
            }, 5000);
        }
    }
});